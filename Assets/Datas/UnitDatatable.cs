using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitDatatable : ScriptableObject
{
    [SerializeField] List<UnitDataset> datasets;
    
    private Dictionary<string, UnitDataset> datasetsDictionary;

    public void Initialize()
    {
        datasetsDictionary = datasets.ToDictionary(data => data.referenceImageKey);
    }

    public UnitDataset GetItem(string key)
    {
        UnitDataset item = null;
        if (! datasetsDictionary.TryGetValue(key, out item))
        {
            Debug.LogWarning($"[UnitDatatable]키값\"{key}\"을 찾지 못함");
        }
        return item;
    }
}
