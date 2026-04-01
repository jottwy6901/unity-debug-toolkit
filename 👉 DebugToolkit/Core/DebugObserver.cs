using UnityEngine;
using System.Reflection;

public class DebugObserver : MonoBehaviour
{
    [SerializeField] private MonoBehaviour target;
    [SerializeField] private string alias = "PlayerMovement";

    private void Update()
    {
        if (target == null) return;

        var fields = target.GetType().GetFields(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance
        );

        foreach (var field in fields)
        {
            var value = field.GetValue(target);

            Debug.Log($"[{alias}] {field.Name}: {value}");
        }
    }
}