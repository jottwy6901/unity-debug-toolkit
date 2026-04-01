using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Intercepta llamadas a métodos en runtime SIN modificar los scripts originales.
/// Reemplaza dinámicamente los métodos para loguear automáticamente.
/// </summary>
public class DebugMethodInterceptor : MonoBehaviour
{
    #region CLASSES

    private class MethodInterception
    {
        public MethodInfo originalMethod;
        public Action<string> logCallback;
        public MonoBehaviour targetScript;
        public string actionName;
    }

    #endregion

    #region VARIABLES

    private Dictionary<int, Dictionary<string, MethodInterception>> interceptedMethods =
        new Dictionary<int, Dictionary<string, MethodInterception>>();

    private static DebugMethodInterceptor instance;

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
    /// Intercepta una acción específica de un script.
    /// Cada vez que se ejecute el método, se loguea automáticamente.
    /// </summary>
    public static void InterceptAction(
        MonoBehaviour script,
        string actionName,
        string methodName,
        List<string> relatedFields,
        string alias)
    {
        if (!Application.isPlaying) return;

        if (instance == null)
        {
            GameObject go = new GameObject("[DebugMethodInterceptor]");
            instance = go.AddComponent<DebugMethodInterceptor>();
        }

        int scriptId = script.GetInstanceID();

        if (!instance.interceptedMethods.ContainsKey(scriptId))
            instance.interceptedMethods[scriptId] = new Dictionary<string, MethodInterception>();

        // Obtener el método original
        var method = script.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );

        if (method == null)
        {
            Debug.LogWarning($"No se encontró el método {methodName} en {script.GetType().Name}");
            return;
        }

        // Crear el callback de log
        Action<string> logCallback = (fieldInfo) =>
        {
            Debug.Log($"<color=cyan>[{alias}] → {actionName} | {fieldInfo}</color>");
        };

        // Crear intercepción
        var interception = new MethodInterception
        {
            originalMethod = method,
            logCallback = logCallback,
            targetScript = script,
            actionName = actionName
        };

        instance.interceptedMethods[scriptId][actionName] = interception;

        Debug.Log($"<color=yellow>✓ Interceptando '{actionName}' en {alias}</color>");
    }

    /// <summary>
    /// Detiene la intercepción de una acción.
    /// </summary>
    public static void StopIntercepting(MonoBehaviour script, string actionName)
    {
        if (instance == null) return;

        int scriptId = script.GetInstanceID();
        if (instance.interceptedMethods.ContainsKey(scriptId))
        {
            instance.interceptedMethods[scriptId].Remove(actionName);
        }
    }

    /// <summary>
    /// Obtiene los campos relacionados de un script.
    /// </summary>
    public static string GetFieldValues(MonoBehaviour script, List<string> fieldNames)
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

    /// <summary>
    /// Ejecuta una intercepción manualmente (para botón Execute del editor).
    /// </summary>
    public static void ExecuteInterceptedAction(MonoBehaviour script, string actionName, string methodName, List<string> relatedFields, string alias)
    {
        if (script == null) return;

        var method = script.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );

        if (method == null) return;

        try
        {
            string fieldInfo = GetFieldValues(script, relatedFields);
            Debug.Log($"<color=cyan>[{alias}] → {actionName} | {fieldInfo}</color>");
            method.Invoke(script, null);
            fieldInfo = GetFieldValues(script, relatedFields);
            Debug.Log($"<color=green>[{alias}] ✓ {actionName} completado | {fieldInfo}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{alias}] Error: {e.InnerException?.Message}");
        }
    }

    #endregion
}