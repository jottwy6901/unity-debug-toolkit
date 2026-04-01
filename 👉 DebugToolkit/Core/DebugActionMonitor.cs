using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Monitorea acciones en runtime y genera logs automáticos.
/// Se inyecta en scripts sin modificarlos.
/// </summary>
public class DebugActionMonitor : MonoBehaviour
{
    #region VARIABLES

    private MonoBehaviour targetScript;
    private string alias;
    private Dictionary<string, MethodInfo> monitoredMethods = new Dictionary<string, MethodInfo>();
    private Dictionary<string, bool> enabledActions = new Dictionary<string, bool>();

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Registra un script para monitoreo.
    /// </summary>
    public void RegisterTarget(MonoBehaviour script, string scriptAlias)
    {
        targetScript = script;
        alias = scriptAlias;
    }

    /// <summary>
    /// Añade una acción para monitorear.
    /// </summary>
    public void AddMonitoredAction(string actionName, string methodName)
    {
        if (targetScript == null) return;

        var method = targetScript.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );

        if (method != null)
        {
            monitoredMethods[actionName] = method;
            enabledActions[actionName] = false;
        }
    }

    /// <summary>
    /// Habilita/deshabilita el monitoreo de una acción.
    /// </summary>
    public void SetActionMonitored(string actionName, bool monitored)
    {
        if (enabledActions.ContainsKey(actionName))
            enabledActions[actionName] = monitored;
    }

    /// <summary>
    /// Ejecuta una acción monitorrada con logs.
    /// </summary>
    public void ExecuteMonitoredAction(string actionName)
    {
        if (!enabledActions.ContainsKey(actionName) || !monitoredMethods.ContainsKey(actionName))
            return;

        try
        {
            var method = monitoredMethods[actionName];
            Debug.Log($"<color=cyan>[{alias}] → {actionName} ejecutado</color>");
            method.Invoke(targetScript, null);
            Debug.Log($"<color=green>[{alias}] ✓ {actionName} completado</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{alias}] Error en {actionName}: {e.InnerException?.Message}");
        }
    }

    /// <summary>
    /// Verifica si una acción está siendo monitorrada.
    /// </summary>
    public bool IsActionMonitored(string actionName)
    {
        return enabledActions.ContainsKey(actionName) && enabledActions[actionName];
    }

    #endregion
}