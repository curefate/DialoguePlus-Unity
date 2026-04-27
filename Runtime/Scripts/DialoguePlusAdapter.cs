using UnityEngine;
using DialoguePlus.Core;
using System;
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

    private Executor _executor = new();
    private Compiler _compiler = new();

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
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    public async Task ExecuteToEnd(string path)
    {
        Debug.Log($"[D+] Compiling and executing script: {path}");
        var result = _compiler.Compile(path);
        Debug.Log($"[D+] Script compiled successfully: {path}");
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
            await _executor.AutoStepAsync(0);
        }
    }
}
