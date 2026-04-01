using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

/// <summary>
/// Ventana principal del Debug Toolkit.
/// Arrastra scripts → lee acciones automáticamente → genera toggles para logging en runtime.
/// </summary>
public class DebugToolkitWindow : EditorWindow
{
    #region CLASSES

    [System.Serializable]
    public class DebugItem
    {
        public DebugTarget target = new DebugTarget();
        public bool foldout;

        [System.NonSerialized]
        public List<DebugActionAnalyzer.DebugAction> actions = new List<DebugActionAnalyzer.DebugAction>();

        [System.NonSerialized]
        public Dictionary<string, bool> actionToggles = new Dictionary<string, bool>();
    }

    #endregion

    #region VARIABLES

    [SerializeField] private List<DebugItem> items = new List<DebugItem>();
    private List<DebugItem> filteredItems = new List<DebugItem>();

    private ReorderableList list;
    private string search = "";
    
    // Control de cambios para ReorderableList
    private bool needsRepaint = false;

    #endregion

    #region UNITY METHODS

    [MenuItem("Tools/Debug Toolkit")]
    public static void ShowWindow()
    {
        GetWindow<DebugToolkitWindow>("Debug Toolkit");
    }

    private void OnEnable()
    {
        if (items == null)
            items = new List<DebugItem>();

        // Reconstruir análisis de scripts
        foreach (var item in items)
        {
            if (item.target.target != null)
                AnalyzeScript(item);
        }

        BuildFilteredList();

        list = new ReorderableList(filteredItems, typeof(DebugItem), true, true, true, true);
        ConfigureList();
    }

    private void OnGUI()
    {
        DrawSearchBar();
        GUILayout.Space(5);

        if (list != null)
        {
            list.DoLayoutList();
            
            // Si hubo cambios, repinta
            if (needsRepaint)
            {
                needsRepaint = false;
                Repaint();
            }
        }
    }

    #endregion

    #region UI METHODS

    private void ConfigureList()
    {
        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Debug Toolkit - Acciones en Tiempo Real", EditorStyles.boldLabel);
        };

        list.onAddCallback = l =>
        {
            items.Add(new DebugItem());
            BuildFilteredList();
        };

        list.onRemoveCallback = l =>
        {
            if (l.index >= 0 && l.index < filteredItems.Count)
            {
                var item = filteredItems[l.index];
                if (item.target.target != null)
                    DebugActionRunner.UnregisterScript(item.target.target);

                items.Remove(item);
                BuildFilteredList();
            }
        };

        list.drawElementCallback = DrawElement;
        list.elementHeightCallback = GetElementHeight;
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Buscar", GUILayout.Width(50));

        string newSearch = EditorGUILayout.TextField(search);
        if (newSearch != search)
        {
            search = newSearch;
            BuildFilteredList();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (index < 0 || index >= filteredItems.Count) return;
        var item = filteredItems[index];

        rect.y += 2;

        // FOLDOUT
        item.foldout = EditorGUI.Foldout(
            new Rect(rect.x, rect.y, 15, EditorGUIUtility.singleLineHeight),
            item.foldout,
            ""
        );

        // NOMBRE (alias)
        Rect nameRect = new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight);
        item.target.alias = EditorGUI.TextField(nameRect, "Nombre", item.target.alias);

        if (!item.foldout) return;

        float y = rect.y + EditorGUIUtility.singleLineHeight + 4;

        float boxHeight = 50 + (item.actions.Count * 25);
        GUI.Box(new Rect(rect.x, y, rect.width, boxHeight), "");
        y += 5;

        // SCRIPT
        var newScript = (MonoBehaviour)EditorGUI.ObjectField(
            new Rect(rect.x + 10, y, rect.width - 20, EditorGUIUtility.singleLineHeight),
            "Script",
            item.target.target,
            typeof(MonoBehaviour),
            true
        );

        if (newScript != item.target.target)
        {
            item.target.target = newScript;
            if (string.IsNullOrEmpty(item.target.alias) && newScript != null)
                item.target.alias = newScript.GetType().Name;

            AnalyzeScript(item);
            Repaint();
        }

        y += EditorGUIUtility.singleLineHeight + 5;

        // BOTÓN: Log Changes
        if (GUI.Button(new Rect(rect.x + 10, y, rect.width - 20, 20), "Log Changes (Variables)"))
        {
            DebugService.LogTarget(item.target);
        }

        y += 25;

        // ACCIONES DETECTADAS
        EditorGUI.LabelField(new Rect(rect.x + 10, y, rect.width - 20, EditorGUIUtility.singleLineHeight),
            "Acciones Disponibles", EditorStyles.boldLabel);
        y += EditorGUIUtility.singleLineHeight + 3;

        foreach (var action in item.actions)
        {
            if (!item.actionToggles.ContainsKey(action.actionName))
            {
                item.actionToggles[action.actionName] = false;
            }

            bool toggleState = item.actionToggles[action.actionName];

            // TOGGLE
            Rect toggleRect = new Rect(rect.x + 20, y, 20, EditorGUIUtility.singleLineHeight);
            bool newToggleState = EditorGUI.Toggle(toggleRect, toggleState);

            // ETIQUETA
            Rect labelRect = new Rect(rect.x + 45, y, 100, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, action.actionName, EditorStyles.boldLabel);

            // BOTÓN
            Rect buttonRect = new Rect(rect.x + 150, y, rect.width - 160, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginDisabledGroup(!newToggleState);
            {
                if (GUI.Button(buttonRect, "Execute"))
                {
                    if (item.target.target != null && newToggleState)
                    {
                        DebugService.ExecuteLoggedAction(item.target, action.actionName);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            // Si cambió el toggle
            if (newToggleState != toggleState)
            {
                item.actionToggles[action.actionName] = newToggleState;

                if (item.target.target != null)
                {
                    if (newToggleState)
                    {
                        DebugService.RegisterActions(item.target, new List<DebugActionAnalyzer.DebugAction> { action });
                        // IMPORTANTE: Comunicar al runtime QUE ESTÁ ACTIVO
                        DebugActionRunner.SetActionMonitored(item.target.target, action.actionName, true);
                        Debug.Log($"<color=green>✓ '{action.actionName}' activado</color>");
                    }
                    else
                    {
                        // IMPORTANTE: Comunicar al runtime QUE ESTÁ INACTIVO
                        DebugActionRunner.SetActionMonitored(item.target.target, action.actionName, false);
                        Debug.Log($"<color=red>✗ '{action.actionName}' desactivado</color>");
                    }
                }

                GUI.changed = true;
                needsRepaint = true;
            }

            y += EditorGUIUtility.singleLineHeight + 5;
        }
    }

    private float GetElementHeight(int index)
    {
        if (index < 0 || index >= filteredItems.Count) return EditorGUIUtility.singleLineHeight + 6;

        var item = filteredItems[index];

        if (!item.foldout)
            return EditorGUIUtility.singleLineHeight + 6;

        int actionCount = item.actions?.Count ?? 0;
        return EditorGUIUtility.singleLineHeight + 6 + 5 + 20 + 5 + 20 + 20 +
               (actionCount * (EditorGUIUtility.singleLineHeight + 5)) + 40;
    }

    #endregion

    #region LOGIC METHODS 

    private void AnalyzeScript(DebugItem item)
    {
        if (item.target.target == null)
        {
            item.actions = new List<DebugActionAnalyzer.DebugAction>();
            return;
        }

        item.actions = DebugActionAnalyzer.AnalyzeScript(item.target.target);
        item.actionToggles = new Dictionary<string, bool>();
    }

    private void BuildFilteredList()
    {
        if (string.IsNullOrEmpty(search))
        {
            filteredItems = new List<DebugItem>(items);
        }
        else
        {
            filteredItems = items
                .Where(i => !string.IsNullOrEmpty(i.target.alias) &&
                            i.target.alias.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        if (list != null)
            list.list = filteredItems;
    }

    #endregion
}