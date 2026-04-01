using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Inyecta logs dinámicos en runtime sin modificar scripts originales.
/// Gestiona el estado de monitoreo por script y por acción.
/// </summary>
public class DebugActionRunner : MonoBehaviour
{
    #region VARIABLES

    // scriptId → (actionName → isMonitored)
    private Dictionary<int, Dictionary<string, bool>> monitoredActions = 
        new Dictionary<int, Dictionary<string, bool>>();

    // scriptId → (actionName → DebugAction)
    private Dictionary<int, Dictionary<string, DebugActionAnalyzer.DebugAction>>
        registeredActions = new Dictionary<int, Dictionary<string, DebugActionAnalyzer.DebugAction>>();

    private static DebugActionRunner instance;

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Registra un script y sus acciones para logging dinámico.
    /// </summary>
    public static void RegisterScript(MonoBehaviour script, List<DebugActionAnalyzer.DebugAction> actions, string alias)
    {
        if (!Application.isPlaying) return;

        if (instance == null)
        {
            GameObject go = new GameObject("[DebugActionRunner]");
            instance = go.AddComponent<DebugActionRunner>();
        }

        int scriptId = script.GetInstanceID();

        // Crear diccionarios si no existen
        if (!instance.registeredActions.ContainsKey(scriptId))
        {
            instance.registeredActions[scriptId] = new Dictionary<string, DebugActionAnalyzer.DebugAction>();
            instance.monitoredActions[scriptId] = new Dictionary<string, bool>();
        }

        // Registrar acciones
        foreach (var action in actions)
        {
            instance.registeredActions[scriptId][action.actionName] = action;
            
            // IMPORTANTE: inicializar como NO monitoreado
            if (!instance.monitoredActions[scriptId].ContainsKey(action.actionName))
            {
                instance.monitoredActions[scriptId][action.actionName] = false;
            }
        }
    }

    /// <summary>
    /// Habilita/deshabilita el monitoreo de una acción específica.
    /// ESTO ES LO QUE DEBE LLAMAR EL EDITOR.
    /// </summary>
    public static void SetActionMonitored(MonoBehaviour script, string actionName, bool monitored)
    {
        if (!Application.isPlaying || instance == null) return;

        int scriptId = script.GetInstanceID();

        if (!instance.monitoredActions.ContainsKey(scriptId))
            return;

        if (!instance.monitoredActions[scriptId].ContainsKey(actionName))
            return;

        instance.monitoredActions[scriptId][actionName] = monitored;

        Debug.Log($"<color=yellow>Action '{actionName}' monitoring: {(monitored ? "ON" : "OFF")}</color>");
    }

    /// <summary>
    /// Verifica si una acción está siendo monitoreada.
    /// ESTO ES LO QUE CONSULTA PlayerMovement.
    /// </summary>
    public static bool IsMonitored(MonoBehaviour script, string actionName)
    {
        if (!Application.isPlaying || instance == null) return false;

        int scriptId = script.GetInstanceID();

        if (!instance.monitoredActions.ContainsKey(scriptId))
            return false;

        if (!instance.monitoredActions[scriptId].ContainsKey(actionName))
            return false;

        return instance.monitoredActions[scriptId][actionName];
    }

    /// <summary>
    /// Ejecuta una acción con logs automáticos (desde botón Execute).
    /// </summary>
    public static void ExecuteAction(MonoBehaviour script, string actionName, string alias)
    {
        if (instance == null) return;

        int scriptId = script.GetInstanceID();

        if (!instance.registeredActions.ContainsKey(scriptId) ||
            !instance.registeredActions[scriptId].ContainsKey(actionName))
            return;

        var action = instance.registeredActions[scriptId][actionName];

        try
        {
            string fieldInfo = GetFieldValues(script, action.relatedFields);
            Debug.Log($"<color=cyan>[{alias}] → {action.actionName} | {fieldInfo}</color>");

            action.method.Invoke(script, null);

            fieldInfo = GetFieldValues(script, action.relatedFields);
            Debug.Log($"<color=green>[{alias}] ✓ {action.actionName} completado | {fieldInfo}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{alias}] Error en {action.actionName}: {e.InnerException?.Message}");
        }
    }

    /// <summary>
    /// Limpia referencias al destruir un script.
    /// </summary>
    public static void UnregisterScript(MonoBehaviour script)
    {
        if (instance == null) return;

        int scriptId = script.GetInstanceID();
        instance.registeredActions.Remove(scriptId);
        instance.monitoredActions.Remove(scriptId);
    }

    #endregion

    #region PRIVATE METHODS

    private static string GetFieldValues(MonoBehaviour script, List<string> fieldNames)
    {
        if (fieldNames == null || fieldNames.Count == 0)
            return "";

        var type = script.GetType();
        var parts = new List<string>();

        foreach (var fieldName in fieldNames)
        {
            var field = type.GetField(
                fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null)
            {
                object value = field.GetValue(script);
                parts.Add($"{fieldName}={value}");
            }
        }

        return string.Join(" | ", parts);
    }

    #endregion
}