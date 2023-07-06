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
    public CesiumGeoreference cesiumGeoreference;
    public Transform parent;

    void Awake() {
        cesiumGeoreference = GameObject.Find("CesiumGeoreference").GetComponent<CesiumGeoreference>();
        parent = this.transform.parent;
    }

    // Start is called before the first frame update
    void Start()
    {
        LoadAirspaces();
    }

    [ContextMenu("Load Airspaces")]
    void LoadAirspaces() {
        // Read airspaces data from the given asset
        Airspace[] airspaces = JsonHelper.FromJson<Airspace>(airspacesData.text);

              
        for(int i = 0; i < airspaces.Length; i++) {
            Airspace airspace = airspaces[i];

            AddAirspaceObject(airspace);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void AddAirspaceObject(Airspace airspace) {
        

        double3[] positions = new double3[airspace.geometry.coordinates.Length];
        double3[] unityPositions = new double3[positions.Length];

        Vector3[] vertices = new Vector3[2 * positions.Length];
        vertices[0] = new Vector3(0, 0, 0);

        Vector3[] normals = new Vector3[vertices.Length];

        List<int> tris = new List<int>();
        
        tris.Add(1);
        tris.Add(0);
        tris.Add(2);

        for(int i = 0; i < airspace.geometry.coordinates.Length; i++) {
            string[] coords = airspace.geometry.coordinates[i].Split(" ");
            string longitude = coords[0];
            string latitude = coords[1];

            double lowerLimit = airspace.lowerLimit.value;
            if(airspace.lowerLimit.unit == HeightUnit.FL) {
                lowerLimit *= 100;
            }

            float upperLimit = airspace.upperLimit.value;
            if(airspace.upperLimit.unit == HeightUnit.FL) {
                upperLimit *= 100;
            }

            double3 position = new double3(
                double.Parse(longitude, CultureInfo.InvariantCulture), 
                double.Parse(latitude, CultureInfo.InvariantCulture), 
                lowerLimit + 650); // TODO: calculate height based on feet

            positions[i] = position;

            double3 earthCenteredPosition = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(position);
            double3 unityPosition = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(earthCenteredPosition);
            unityPositions[i] = unityPosition;


            int topIndex = positions.Length + i;

            if(i != 0) {
                vertices[i] = new Vector3((float)(unityPosition.x - unityPositions[0].x), 0, (float)(unityPosition.z - unityPositions[0].z));
                vertices[topIndex] = new Vector3(vertices[i].x, upperLimit + 650, vertices[i].z); // TODO: calculate y based on feet
            }

            normals[i] = Vector3.up;
            normals[topIndex] = Vector3.up;

            if(i > 2) {
                tris.Add(i-1);
                tris.Add(i);
                tris.Add(0);
            }
        }

        // tris.Add(positions.Length + 2);
        // tris.Add(positions.Length + 1);
        // tris.Add(positions.Length + 3);

        // Debug.Log("-----");
        for(int i = positions.Length; i < positions.Length * 2; i++) {

            if(i > positions.Length + 2) {
                tris.Add(i-1);
                tris.Add(i);
                tris.Add(positions.Length + 1);
            }

            //  /|
            // /_|
            tris.Add(i);
            int bottomLeftCorner = i - 1 - positions.Length;
            if(bottomLeftCorner < 0) {
                bottomLeftCorner = positions.Length - 1;
            }
            tris.Add(bottomLeftCorner);
            tris.Add(i - positions.Length);
            // ___
            // | /
            // |/
            int topRightCorner = i + 1;
            if(topRightCorner == positions.Length * 2) {
                topRightCorner = positions.Length + 1;
            }
            tris.Add(topRightCorner);
            tris.Add(i);
            tris.Add(i - positions.Length);
        }
        // for(int i = 0; i < tris.Count; i++) {
        //     Debug.Log("i: " + tris[i]);
        // }
        // Debug.Log("-----");


        GameObject gObject = new GameObject(airspace.name);
        gObject.transform.SetParent(parent);

        MeshRenderer meshRenderer = gObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(airspaceMaterial);

        MeshFilter meshFilter = gObject.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.normals = normals;
        meshFilter.mesh = mesh;

        CesiumGlobeAnchor anchor = gObject.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = positions[0];
        anchor.transform.localScale = new Vector3(1, 1, 1);
    }

    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }
}
