using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

/// <summary>
/// Analiza un MonoBehaviour sin modificarlo.
/// Extrae métodos que representan acciones (Handle*, On*, etc.).
/// </summary>
public class DebugActionAnalyzer
{
    #region CLASSES

    [System.Serializable]
    public class DebugAction
    {
        public string actionName;          // "Jump", "Movement"
        public string methodName;          // "HandleJump"
        public MethodInfo method;
        public List<string> relatedFields; // ["isGrounded", "jumpForce"]
    }

    #endregion

    /// <summary>
    /// Analiza un script y extrae todas sus acciones.
    /// </summary>
    public static List<DebugAction> AnalyzeScript(MonoBehaviour script)
    {
        if (script == null) return new List<DebugAction>();

        var actions = new List<DebugAction>();
        var type = script.GetType();

        var methods = type.GetMethods(
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly
        );

        foreach (var method in methods)
        {
            if (IsActionMethod(method))
            {
                var action = new DebugAction
                {
                    methodName = method.Name,
                    actionName = ExtractActionName(method.Name),
                    method = method,
                    relatedFields = ExtractRelatedFields(script, method)
                };

                actions.Add(action);
            }
        }

        return actions.OrderBy(a => a.actionName).ToList();
    }

    private static bool IsActionMethod(MethodInfo method)
    {
        string name = method.Name;

        // NO son acciones: métodos de ciclo de vida
        if (name.StartsWith("Awake") || name.StartsWith("Start") ||
            name.StartsWith("Update") || name.StartsWith("FixedUpdate") ||
            name.StartsWith("LateUpdate") || name.StartsWith("OnGUI"))
            return false;

        // NO son acciones: eventos de colisión estándar
        if (name.StartsWith("OnCollision") || name.StartsWith("OnTrigger"))
            return false;

        // SÍ son acciones: métodos privados con patrones
        if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
        {
            if (name.StartsWith("Handle") || name.StartsWith("Execute") ||
                name.StartsWith("Perform") || name.StartsWith("Check") ||
                name.StartsWith("Do") || name.StartsWith("Can") ||
                name.StartsWith("Try") || name.StartsWith("Attempt"))
                return true;
        }

        return false;
    }

    private static string ExtractActionName(string methodName)
    {
        var prefixes = new[] { "Handle", "Execute", "Perform", "Check", "Do", "Can", "Try", "Attempt" };

        foreach (var prefix in prefixes)
        {
            if (methodName.StartsWith(prefix) && methodName.Length > prefix.Length)
            {
                return methodName.Substring(prefix.Length);
            }
        }

        return methodName;
    }

    private static List<string> ExtractRelatedFields(MonoBehaviour script, MethodInfo method)
    {
        var fields = new List<string>();
        var type = script.GetType();
        var allFields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        );

        // Heurística simple: si el campo coincide con el nombre del método
        foreach (var field in allFields)
        {
            string fieldName = field.Name.ToLower().Replace("_", "");
            string methodName = method.Name.ToLower();

            if (methodName.Contains(fieldName))
                fields.Add(field.Name);
        }

        // Fallback: asociación por patrón de acción
        if (method.Name.Contains("Jump"))
            AddIfExists(allFields, fields, new[] { "isGrounded", "jumpForce", "rb" });

        if (method.Name.Contains("Movement"))
            AddIfExists(allFields, fields, new[] { "speed", "rb", "h" });

        return fields.Distinct().ToList();
    }

    private static void AddIfExists(FieldInfo[] allFields, List<string> fieldList, string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (allFields.Any(f => f.Name == name))
                fieldList.Add(name);
        }
    }
}