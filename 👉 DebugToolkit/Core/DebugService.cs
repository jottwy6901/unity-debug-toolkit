using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class DebugService
{
    private static Dictionary<object, DebugTracker> trackers = new Dictionary<object, DebugTracker>();

    public static void LogTarget(DebugTarget debugTarget)
    {
        if (debugTarget == null || debugTarget.target == null) return;

        if (!trackers.ContainsKey(debugTarget.target))
        {
            trackers[debugTarget.target] = new DebugTracker(debugTarget.target);
        }

        var changes = trackers[debugTarget.target].GetChanges();

        if (changes.Count == 0) return;

        string alias = GetAlias(debugTarget);

        foreach (var change in changes)
        {
            Debug.Log($"[{alias}] {change}");
        }
    }

    public static void RegisterActions(DebugTarget debugTarget, List<DebugActionAnalyzer.DebugAction> actions)
    {
        if (debugTarget == null || debugTarget.target == null) return;

        string alias = GetAlias(debugTarget);

        // Recolectar todos los campos relacionados
        var allFields = new List<string>();
        foreach (var action in actions)
        {
            if (action.relatedFields != null)
                allFields.AddRange(action.relatedFields);
        }
        allFields = new List<string>(allFields.Distinct());

        // Registrar monitor
        DebugRuntimeMonitor.RegisterMonitor(debugTarget.target, alias, allFields);
    }

    public static void EnableAction(DebugTarget debugTarget, string actionName)
    {
        if (debugTarget?.target == null) return;
        DebugRuntimeMonitor.EnableAction(debugTarget.target, actionName);
    }

    public static void DisableAction(DebugTarget debugTarget, string actionName)
    {
        if (debugTarget?.target == null) return;
        DebugRuntimeMonitor.DisableAction(debugTarget.target, actionName);
    }

    public static void ExecuteLoggedAction(DebugTarget debugTarget, string actionName)
    {
        if (debugTarget?.target == null) return;

        string alias = GetAlias(debugTarget);
        DebugMethodInterceptor.ExecuteInterceptedAction(
            debugTarget.target,
            actionName,
            $"Handle{actionName}",
            new List<string>(),
            alias
        );
    }

    private static string GetAlias(DebugTarget target)
    {
        return string.IsNullOrEmpty(target.alias)
            ? target.target.GetType().Name
            : target.alias;
    }
}