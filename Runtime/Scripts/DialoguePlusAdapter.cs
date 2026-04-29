using UnityEngine;
using DialoguePlus.Core;
using DialoguePlus.Unity;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

public class DialoguePlusAdapter : MonoBehaviour
{
    private static DialoguePlusAdapter? _instance;
    /// <summary>
    /// The current adapter instance in the scene.
    /// Users are expected to add <see cref="DialoguePlusAdapter"/> to a GameObject explicitly.
    /// </summary>
    public static DialoguePlusAdapter Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new InvalidOperationException(
                    "DialoguePlusAdapter.Instance is null. Please add a DialoguePlusAdapter component to a GameObject in the scene (and keep exactly one active instance)."
                );
            }
            return _instance;
        }
    }

    private readonly ConcurrentDictionary<string, SourceContent> _cache = new(StringComparer.Ordinal);

    private Executor _executor = new();
    private Compiler _compiler = null!;

    public Executor Executor => _executor;
    public Runtime Runtime => _executor.Runtime;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogError("Multiple DialoguePlusAdapter instances detected. Please keep exactly one active instance in the scene.");
            enabled = false;
            return;
        }

        _instance = this;

    // Build compiler with Unity resolver (cache + addressables + filesystem).
    var resolver = DialoguePlusUnityResolverFactory.CreateRuntimeResolver(_cache);
    _compiler = new Compiler(resolver);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public Task<CompileResult> CompileAsync(string entrySourceId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entrySourceId))
            throw new ArgumentException("entrySourceId cannot be null or empty.", nameof(entrySourceId));

        var req = new CompileRequest
        {
            EntrySourceId = entrySourceId,
            CancellationToken = ct,
        };

        return _compiler.CompileAsync(req);
    }

    public Task<CompileResult> CompileAsync(DialoguePlusScript entry, CancellationToken ct = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        var entrySourceId = !string.IsNullOrWhiteSpace(entry.SourceId)
            ? entry.SourceId
            : (!string.IsNullOrWhiteSpace(entry.Key)
                ? DialoguePlusSourceId.SourceIdFromKey(entry.Key)
                : throw new InvalidOperationException(
                    "DialoguePlusScript is missing both SourceId and Key. Ensure it was imported correctly, or call CompileAsync(entrySourceId) instead."
                ));

        var req = new CompileRequest
        {
            EntrySourceId = entrySourceId,
            EntryTextOverride = entry.Text,
            CancellationToken = ct,
        };

        return _compiler.CompileAsync(req);
    }

    public async Task ExecuteToEnd(DialoguePlusScript entry, CancellationToken ct = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        var result = await CompileAsync(entry, ct);
        Debug.Log($"[D+] Script compiled: {result.SourceID} (success={result.Success})");
        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == Diagnostic.SeverityLevel.Error)
            {
                Debug.LogError($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
            else if (diag.Severity == Diagnostic.SeverityLevel.Warning)
            {
                Debug.LogWarning($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
            else
            {
                Debug.Log($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
        }
        if (result.Success)
        {
            this.Runtime.Variables.Clear();
            _executor.Prepare(result.Labels);
            Debug.Log($"[D+] Script execution start, include labels: {string.Join(", ", result.Labels.Labels.Keys)}");
            await _executor.AutoStepAsync(0, ct);
        }
    }

    public async Task ExecuteToEnd(string entrySourceId, CancellationToken ct = default)
    {
        Debug.Log($"[D+] Compiling and executing script: {entrySourceId}");
        var result = await CompileAsync(entrySourceId, ct);
        Debug.Log($"[D+] Script compiled: {entrySourceId} (success={result.Success})");
        foreach (var diag in result.Diagnostics)
        {
            if (diag.Severity == Diagnostic.SeverityLevel.Error)
            {
                Debug.LogError($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
            else if (diag.Severity == Diagnostic.SeverityLevel.Warning)
            {
                Debug.LogWarning($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
            else
            {
                Debug.Log($"[D+] {diag.Message} (Line {diag.Line}, Column {diag.Column})");
            }
        }
        if (result.Success)
        {
            this.Runtime.Variables.Clear();
            _executor.Prepare(result.Labels);
            Debug.Log($"[D+] Script execution start, include labels: {string.Join(", ", result.Labels.Labels.Keys)}");
            await _executor.AutoStepAsync(0, ct);
        }
    }
}
