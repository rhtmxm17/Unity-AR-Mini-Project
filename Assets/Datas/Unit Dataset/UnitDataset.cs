using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Unit Dataset")]
public class UnitDataset : ScriptableObject
{
    public GameObject unitPrefab;
    public string referenceImageKey;
}
