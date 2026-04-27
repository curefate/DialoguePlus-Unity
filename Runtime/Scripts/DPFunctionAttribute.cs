using System;

/// <summary>
/// Mark a C# method as callable from DialoguePlus scripts.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DPFunctionAttribute : Attribute
{
    public string Name { get; } = string.Empty;

    public DPFunctionAttribute() { }

    public DPFunctionAttribute(string name)
    {
        Name = name;
    }
}
