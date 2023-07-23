using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DataLoaderJson : MonoBehaviour
{
    public TextAsset airspacesData;
    private Airspace[] airspaces;

    public void LoadAirspaces() {
        // Read airspaces data from the given asset
        airspaces = JsonHelper.FromJson<Airspace>(airspacesData.text);
    }

    public Airspace[] GetAirspaces() {
        return airspaces;
    }

    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }
}
