using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Detecta cambios en las variables de un objeto mediante reflection.
/// Solo devuelve diferencias entre frames.
/// </summary>
public class DebugTracker
{
    #region VARIABLES

    private object target;
    private Dictionary<string, object> lastValues = new Dictionary<string, object>();

    #endregion

    #region CONSTRUCTOR

    public DebugTracker(object target)
    {
        this.target = target;
    }

    #endregion

    #region PUBLIC METHODS

    public List<string> GetChanges()
    {
        List<string> changes = new List<string>();

        if (target == null) return changes;

        var fields = target.GetType().GetFields(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance
        );

        foreach (var field in fields)
        {
            object currentValue = field.GetValue(target);

            if (!lastValues.ContainsKey(field.Name))
            {
                lastValues[field.Name] = currentValue;
                continue;
            }

            if (!Equals(lastValues[field.Name], currentValue))
            {
                changes.Add($"{field.Name} → {currentValue}");
                lastValues[field.Name] = currentValue;
            }
        }

        return changes;
    }

    #endregion
}