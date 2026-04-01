using UnityEngine;

/// <summary>
/// Representa un elemento a debuggear.
/// Permite asignar un alias personalizado y un script objetivo.
/// </summary>

[System.Serializable]
public class DebugTarget
{
    #region VARIABLES

    public string alias;
    public MonoBehaviour target;

    #endregion
}