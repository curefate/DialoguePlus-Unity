using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DialoguePlus.Core;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public static class FunctionRegistrar
{
    private static readonly HashSet<Type> AllowedTypes = new()
    {
        typeof(string), typeof(bool), typeof(int), typeof(float)
    };

    private static readonly string[] SkipNamespacePrefixes =
    {
        "UnityEngine",
        "UnityEditor",
        "TMPro",
        "System",
        "Microsoft",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterAll()
    {
        // Users must add DialoguePlusAdapter explicitly.
        // If there's no adapter in the scene, skip auto-registration.
        if (!TryGetRegistry(out var registry))
        {
            Debug.LogWarning("[D+] DialoguePlusAdapter not found in scene. DPFunction auto-registration skipped.");
            return;
        }

        // Scan only components that exist in currently loaded scenes (active GameObjects only).
        // This keeps auto-registration costs proportional to scene complexity rather than total AppDomain size.
        var typesInScenes = CollectSceneComponentTypes();
        var attrType = typeof(DPFunctionAttribute);
        foreach (var type in typesInScenes)
        {
            foreach (var m in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(m => m.IsDefined(attrType, false)))
            {
                TryRegister(registry, m, CreateInstanceDelegate);
            }

            foreach (var m in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(m => m.IsDefined(attrType, false)))
            {
                TryRegister(registry, m, CreateStaticDelegate);
            }
        }
    }

    private static void TryRegister(FunctionRegistry registry, MethodInfo method, Func<MethodInfo, Delegate> delegateFactory)
    {
        if (method.IsGenericMethodDefinition) { Warn(method, "Generic method is not supported."); return; }

        if (HasOptionalParameters(method))
        {
            Warn(method,
                "Optional parameters detected. DialoguePlus does not support omitting optional args at runtime; scripts must pass all parameters explicitly."
            );
        }

        if (!CheckSignatureSupported(method, out string reason))
        {
            Warn(method, $"Unsupported signature: {reason}");
            return;
        }

        var name = ResolveFuncName(method);
        try
        {
            var del = delegateFactory(method);
            registry.AddFunction(del, name);
#if UNITY_EDITOR
            Debug.Log($"[D+] register：{name} => {method.DeclaringType.FullName}.{method.Name}");
#endif
        }
        catch (Exception ex)
        {
            Warn(method, $"Failed to register: {ex.Message}");
        }
    }

    private static Delegate CreateInstanceDelegate(MethodInfo method) => DelegateWrapper(method);

    private static Delegate CreateStaticDelegate(MethodInfo method)
    {
        var delType = BuildDelegateType(method);
        return Delegate.CreateDelegate(delType, method);
    }

    // Wrap instance method as Action<string, ...> / Func<string, ..., TResult>, the first parameter is the GameObject name
    private static Delegate DelegateWrapper(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        var srcParams = method.GetParameters().Select(p => p.ParameterType).ToArray();
        var retType = method.ReturnType;

        // 目标委托形参：string(对象名) + 原形参
        var wrapperParams = (new[] { typeof(string) }).Concat(srcParams).ToArray();

        // 构造委托类型
        Type delegateType = retType == typeof(void)
            ? Expression.GetActionType(wrapperParams)
            : Expression.GetFuncType(wrapperParams.Concat(new[] { retType }).ToArray());

        // 表达式参数： (string goName, A a, B b, ...)
        var paramExprs = wrapperParams.Select(Expression.Parameter).ToArray();
        var goNameExpr = paramExprs[0];
        var argExprs = paramExprs.Skip(1).ToArray(); // A,B,...

        // GameObject.Find(goName)
        var findGo = typeof(GameObject).GetMethod(nameof(GameObject.Find), new[] { typeof(string) });
        var goVar = Expression.Variable(typeof(GameObject), "go");

        // if (go == null) throw ...
        var exCtor1 = typeof(ArgumentException).GetConstructor(new[] { typeof(string) });

        // go.GetComponent(declaringType)
        var getCompGeneric = typeof(GameObject).GetMethods()
                                .First(mi => mi.Name == nameof(GameObject.GetComponent)
                                          && mi.IsGenericMethodDefinition && mi.GetParameters().Length == 0)
                                .MakeGenericMethod(declaringType);
        var compVar = Expression.Variable(declaringType, "comp");
        var exCtor2 = typeof(ArgumentException).GetConstructor(new[] { typeof(string) });

        // comp.M(a,b,...)
        var call = Expression.Call(compVar, method, argExprs);

        // 构建 Block：查找 → 取组件 → 调用
        var body = Expression.Block(
            new ParameterExpression[] { goVar, compVar },
            Expression.Assign(goVar, Expression.Call(findGo, goNameExpr)),
            Expression.IfThen(
                Expression.Equal(goVar, Expression.Constant(null, typeof(GameObject))),
                Expression.Throw(
                    Expression.New(
                        exCtor1,
                        Expression.Call(
                            typeof(string),
                            nameof(string.Format),
                            Type.EmptyTypes,
                            Expression.Constant("GameObject '{0}' not found."),
                            goNameExpr
                        )
                    )
                )
            ),
            Expression.Assign(compVar, Expression.Call(goVar, getCompGeneric)),
            Expression.IfThen(
                Expression.Equal(compVar, Expression.Constant(null, declaringType)),
                Expression.Throw(
                    Expression.New(
                        exCtor2,
                        Expression.Constant($"Component {declaringType.Name} not found.")
                    )
                )
            ),
            call // 返回值：void 直接 call；非 void 直接返回 call 的结果
        );

        return Expression.Lambda(delegateType, body, paramExprs).Compile();
    }

    private static bool CheckSignatureSupported(MethodInfo m, out string reason)
    {
        if (m.ReturnType != typeof(void) && !AllowedTypes.Contains(m.ReturnType))
        {
            reason = $"Return type not supported: {m.ReturnType.Name}（expected void/string/bool/int/float）";
            return false;
        }

        foreach (var p in m.GetParameters())
        {
            var t = p.ParameterType;
            if (!AllowedTypes.Contains(t))
            {
                reason = $"Parameter '{p.Name}' type {t.Name} not supported（expected string/bool/int/float）";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static string ResolveFuncName(MethodInfo m)
    {
        var attr = m.GetCustomAttribute<DPFunctionAttribute>(false);
        return string.IsNullOrWhiteSpace(attr?.Name) ? m.Name : attr!.Name;
    }

    private static Type BuildDelegateType(MethodInfo method)
    {
        var ps = method.GetParameters().Select(p => p.ParameterType).ToArray();
        return method.ReturnType == typeof(void)
            ? Expression.GetActionType(ps)
            : Expression.GetFuncType(ps.Concat(new[] { method.ReturnType }).ToArray());
    }

    private static void Warn(MethodInfo m, string msg)
        => Debug.LogWarning($"[D+] {m.DeclaringType.FullName}.{m.Name} - {msg}");

    private static bool TryGetRegistry(out FunctionRegistry registry)
    {
        registry = null!;
        try
        {
            registry = DialoguePlusAdapter.Instance.Runtime.Functions;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasOptionalParameters(MethodInfo method)
        => method.GetParameters().Any(p => p.HasDefaultValue || p.IsOptional);

    private static bool ShouldSkipType(Type t)
    {
        // Skip Unity/Editor/framework types - we're only interested in user/game scripts.
        // This also avoids scanning huge UnityEngine component trees.
        var ns = t.Namespace ?? string.Empty;
        foreach (var prefix in SkipNamespacePrefixes)
        {
            if (ns.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static HashSet<Type> CollectSceneComponentTypes()
    {
        var set = new HashSet<Type>();

        // FindObjectsOfType will include components from all loaded scenes.
        // We limit ourselves to active objects to match expected gameplay surface.
        var allBehaviours = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allBehaviours)
        {
            if (mb == null) continue;
            if (!mb.isActiveAndEnabled) continue;

            var type = mb.GetType();
            if (ShouldSkipType(type)) continue;
            set.Add(type);
        }

        return set;
    }
}
