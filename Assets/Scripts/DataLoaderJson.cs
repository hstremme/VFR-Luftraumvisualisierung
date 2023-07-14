using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using System;
using Unity.Mathematics;
using System.Globalization;

public class DataLoaderJson : MonoBehaviour
{
    public TextAsset airspacesData;
    public Material airspaceMaterial;
    public CesiumGeoreference cesiumGeoreference;

    void Awake() {
        cesiumGeoreference = GameObject.Find("CesiumGeoreference").GetComponent<CesiumGeoreference>();
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

        double lowerLimit = airspace.lowerLimit.value;
        if(airspace.lowerLimit.unit == HeightUnit.FL) {
            lowerLimit *= 100 / 3.28084; // FL in meters
        }

        float upperLimit = airspace.upperLimit.value;
        if(airspace.upperLimit.unit == HeightUnit.FL) {
            upperLimit *= 100 / (float)3.28084; // FL in meters
        }

        Vector3[] vertices = new Vector3[2 * positions.Length];
        Vector2[] vertices2D = new Vector2[positions.Length];
        vertices2D[0] = new Vector2(0, 0);
        vertices[0] = new Vector3(0, 0, 0);

        Vector3[] normals = new Vector3[vertices.Length];

        List<int> tris;

        

        // General vertices and triangles for the bottom surface
        for(int i = 0; i < airspace.geometry.coordinates.Length; i++) {
            string[] coords = airspace.geometry.coordinates[i].Split(" ");
            string longitude = coords[0];
            string latitude = coords[1];

            vertices2D[i] = new Vector2(float.Parse(longitude, CultureInfo.InvariantCulture), float.Parse(latitude, CultureInfo.InvariantCulture));

            double3 position = new double3(
                double.Parse(longitude, CultureInfo.InvariantCulture), 
                double.Parse(latitude, CultureInfo.InvariantCulture), 
                lowerLimit); // TODO: calculate height based on feet

            positions[i] = position;

            double3 earthCenteredPosition = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(position);
            double3 unityPosition = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(earthCenteredPosition);
            unityPositions[i] = unityPosition;


            int topIndex = positions.Length + i;

            if(i != 0) {
                vertices2D[i] = new Vector2((float)(unityPosition.x - unityPositions[0].x), (float)(unityPosition.z - unityPositions[0].z));
                vertices[i] = new Vector3((float)(unityPosition.x - unityPositions[0].x), 0, (float)(unityPosition.z - unityPositions[0].z));
            }
            vertices[topIndex] = new Vector3(vertices[i].x, upperLimit, vertices[i].z);

            normals[i] = Vector3.up;
            normals[topIndex] = Vector3.up;
        }
        Triangulator triangulator = new Triangulator(vertices2D);
        tris = triangulator.Triangulate();

        // Triangles for the top surface
        int trisPerArea = tris.Count;
        for(int i = 0; i < trisPerArea; i++) {
            tris.Add(tris[i] + positions.Length);
        }

        // Triangles for the geometry's sides
        for(int i = positions.Length; i < positions.Length * 2; i++) {
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
                topRightCorner = positions.Length;
            }
            tris.Add(topRightCorner);
            tris.Add(i);
            tris.Add(i - positions.Length);
        }

        GameObject gObject = new GameObject(airspace.name);
        if(this.transform.parent != null) {
            gObject.transform.SetParent(this.transform.parent.transform);
        }

        Mesh mesh = new Mesh();

        MeshFilter meshFilter = gObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.normals = normals;

        MeshRenderer meshRenderer = gObject.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(airspaceMaterial);

        // Outline outline = gObject.AddComponent<Outline>();
        // outline.OutlineMode = OutlineMode.OutlineAll;
        // outline.OutlineColor = Color.red;
        // outline.OutlineWidth = 8f;

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
