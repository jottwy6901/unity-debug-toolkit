using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;

/// <summary>
/// Monitorea automáticamente cambios en campos de un script en runtime.
/// 100% plug & play: sin necesidad de modificar los scripts originales.
/// </summary>
public class DebugRuntimeMonitor : MonoBehaviour
{
    #region CLASSES

    private class ScriptMonitor
    {
        public MonoBehaviour script;
        public string alias;
        public Dictionary<string, object> lastFieldValues = new Dictionary<string, object>();
        public List<string> watchedFields;
        public List<string> enabledActions;
    }

    #endregion

    #region VARIABLES

    private Dictionary<int, ScriptMonitor> monitors = new Dictionary<int, ScriptMonitor>();
    private static DebugRuntimeMonitor instance;

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Update()
    {
        // Monitorear cambios cada frame
        foreach (var monitor in monitors.Values)
        {
            CheckFieldChanges(monitor);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Registra un script para monitoreo automático de cambios.
    /// </summary>
    public static void RegisterMonitor(MonoBehaviour script, string alias, List<string> fieldNames)
    {
        if (!Application.isPlaying) return;

        if (instance == null)
        {
            GameObject go = new GameObject("[DebugRuntimeMonitor]");
            instance = go.AddComponent<DebugRuntimeMonitor>();
        }

        int scriptId = script.GetInstanceID();

        var monitor = new ScriptMonitor
        {
            script = script,
            alias = alias,
            watchedFields = fieldNames,
            enabledActions = new List<string>()
        };

        // Inicializar valores de campos
        foreach (var fieldName in fieldNames)
        {
            var field = script.GetType().GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null)
            {
                monitor.lastFieldValues[fieldName] = field.GetValue(script);
            }
        }

        instance.monitors[scriptId] = monitor;
        Debug.Log($"<color=yellow>✓ Monitoreando {alias}</color>");
    }

    /// <summary>
    /// Habilita el monitoreo de una acción específica.
    /// </summary>
    public static void EnableAction(MonoBehaviour script, string actionName)
    {
        if (instance == null) return;

        int scriptId = script.GetInstanceID();
        if (instance.monitors.ContainsKey(scriptId))
        {
            if (!instance.monitors[scriptId].enabledActions.Contains(actionName))
                instance.monitors[scriptId].enabledActions.Add(actionName);
        }
    }

    /// <summary>
    /// Deshabilita el monitoreo de una acción específica.
    /// </summary>
    public static void DisableAction(MonoBehaviour script, string actionName)
    {
        if (instance == null) return;

        int scriptId = script.GetInstanceID();
        if (instance.monitors.ContainsKey(scriptId))
        {
            instance.monitors[scriptId].enabledActions.Remove(actionName);
        }
    }

    #endregion

    #region PRIVATE METHODS

    private void CheckFieldChanges(ScriptMonitor monitor)
    {
        if (monitor.script == null || monitor.enabledActions.Count == 0)
            return;

        var type = monitor.script.GetType();

        foreach (var fieldName in monitor.watchedFields)
        {
            var field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field == null) continue;

            object currentValue = field.GetValue(monitor.script);

            // Si el campo cambió
            if (!monitor.lastFieldValues.ContainsKey(fieldName) || !Equals(monitor.lastFieldValues[fieldName], currentValue))
            {
                // Determinar qué acción se ejecutó basándose en el campo que cambió
                string actionName = DetermineAction(monitor, fieldName);

                if (!string.IsNullOrEmpty(actionName) && monitor.enabledActions.Contains(actionName))
                {
                    Debug.Log($"<color=cyan>[{monitor.alias}] → {actionName} | {fieldName}={currentValue}</color>");
                }

                monitor.lastFieldValues[fieldName] = currentValue;
            }
        }
    }

    private string DetermineAction(ScriptMonitor monitor, string fieldName)
    {
        // Lógica heurística: mapear campos a acciones
        if (fieldName.Contains("isGrounded"))
            return "Jump";
        if (fieldName.Contains("linearVelocity") || fieldName.Contains("h"))
            return "Movement";

        return null;
    }

    #endregion
}