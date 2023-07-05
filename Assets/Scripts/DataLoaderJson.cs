using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Globalization;

public class DataLoaderJson : MonoBehaviour
{
    public TextAsset airspacesData;
    public Material airspaceMaterial;

    // Start is called before the first frame update
    void Start()
    {
        LoadAirspaces()
    }

    [ContextMenu("Load Airspaces")]
    void LoadAirspaces() {
        // Read airspaces data from the given asset
        Airspace[] airspaces = JsonHelper.FromJson<Airspace>(airspacesData.text);

              
        for(int i = 0; i < airspaces.Length; i++) {
            Airspace airspace = airspaces[i];

            string height = "250"; // TODO: height

            string[] exampleCoords = airspace.geometry.coordinates[0].Split(" ");
            string exampleLatitude = exampleCoords[0];
            string exampleLongitude = exampleCoords[1];

            double3 position = new double3(
                double.Parse(exampleLongitude, CultureInfo.InvariantCulture), 
                double.Parse(exampleLatitude, CultureInfo.InvariantCulture), 
                double.Parse(height, CultureInfo.InvariantCulture));

            AddAirspaceObject(position, airspace);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void AddAirspaceObject(double3 position, Airspace airspace) {
        GameObject planePrimitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
        planePrimitive.name = name;
        planePrimitive.transform.SetParent(this.transform.parent);

        planePrimitive.GetComponent<MeshRenderer>().material = airspaceMaterial;

        planePrimitive.AddComponent<CesiumGlobeAnchor>();

        CesiumGlobeAnchor anchor = planePrimitive.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;
        anchor.transform.localScale = new Vector3(100, 1000, 100);
    }

    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }
}
