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

            // normals[i] = Vector3.up;
            // normals[topIndex] = Vector3.up;

            // if(i >= 2) {
            //     if(tris.Count >= 3) {
            //         // int oldTriangleIndexV3 = tris[tris.Count - 1];
            //         // int oldTriangleIndexV2 = tris[tris.Count - 2];
            //         // int oldTriangleIndexV1 = tris[tris.Count - 3];
            //         // Triangle before = new Triangle(vertices[oldTriangleIndexV1], vertices[oldTriangleIndexV2], vertices[oldTriangleIndexV3], oldTriangleIndexV1, oldTriangleIndexV2, oldTriangleIndexV3);
            //         // Triangle newTri = new Triangle(vertices[i - 1], vertices[i - 2], vertices[0], i, i - 1, 0);

            //         Vector3 pos1 = vertices[i];
            //         Vector3 pos2 = vertices[i - 1];
            //         Vector3 pos3 = vertices[0];

            //         if((pos1.x < pos2.x && pos3.x > pos2.x && ((pos1.z > pos2.z && pos3.z < pos2.z) || (pos1.z < pos2.z && pos3.z > pos2.z)))
            //         || (pos1.z < pos2.z && pos3.z > pos2.z && ((pos1.x > pos2.x && pos3.x < pos2.x) || (pos1.x < pos2.x && pos3.x > pos2.x)))
            //         || (pos1.z == pos2.z && pos2.z == pos3.z && ((pos1.x > pos2.x && pos3.x < pos2.x) || (pos3.x > pos2.x && pos1.x < pos2.x)))
            //         || (pos1.x == pos2.x && pos2.x == pos3.x && ((pos1.z > pos2.z && pos3.z < pos2.z) || (pos3.z > pos2.z && pos1.z < pos2.z)))) {
            //             continue;
            //         }

            //         // Intersection intersectionOfNewTriWithTriBefore = newTri.IntersectionWithTriangle(before);
            //         // if(intersectionOfNewTriWithTriBefore != null) {
                        
            //         // }
            //     }

            //     tris.Add(i-1);
            //     tris.Add(i);
            //     tris.Add(0);
            // }
        }
        Triangulator triangulator = new Triangulator(vertices2D);
        tris = triangulator.Triangulate();

        // Triangles for the top surface
        int trisPerArea = tris.Count;
        for(int i = 0; i < trisPerArea; i++) {
            tris.Add(tris[i] + positions.Length);
        }

        // for(int i = positions.Length; i < positions.Length * 2; i++) {

        //     if(i >= positions.Length + 2) {
        //         tris.Add(i-1);
        //         tris.Add(i);
        //         tris.Add(positions.Length + 1);
        //     }
        // }

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

        // for(int i = 0; i < tris.Count - 3; i += 3) {

        //     int tri1VertexIndex1 = tris[i];
        //     int tri1VertexIndex2 = tris[i + 1];
        //     int tri1VertexIndex3 = tris[i + 2];

        //     if(tri1VertexIndex1 == -1) {
        //         continue;
        //     }

        //     Vector3 tri1pos1 = vertices[tri1VertexIndex1];
        //     Vector3 tri1pos2 = vertices[tri1VertexIndex2];
        //     Vector3 tri1pos3 = vertices[tri1VertexIndex3];
        //     Triangle tri1 = new Triangle(vertices[tri1VertexIndex1], vertices[tri1VertexIndex2], vertices[tri1VertexIndex3], tri1VertexIndex1, tri1VertexIndex2, tri1VertexIndex3); 
        //     for(int j = i + 3; j < tris.Count - 3; j += 3) {

        //         if(tri1VertexIndex1 < positions.Length && tris[j] >= positions.Length) {
        //             break;
        //         }

        //         int tri2VertexIndex1 = tris[j];
        //         int tri2VertexIndex2 = tris[j + 1];
        //         int tri2VertexIndex3 = tris[j + 2];

        //         if(tri2VertexIndex1 == -1) {
        //             continue;
        //         }

        //         Triangle tri2 = new Triangle(vertices[tri2VertexIndex1], vertices[tri2VertexIndex2], vertices[tri2VertexIndex3], tri2VertexIndex1, tri2VertexIndex2, tri2VertexIndex3); 

        //         Intersection intersection = tri1.IntersectionWithTriangle(tri2);
        //         if(intersection != null) {
        //             if(intersection.type == IntersectionType.POINT_INTERSECTS_WITH_AREA) {
        //                 Debug.Log("Passiert das?");
        //                 // tris[j] = -1;
        //                 // tris[j + 1] = -1;
        //                 // tris[j + 2] = -1;
        //             }
                    
        //         }
        //     }
        // }

        // tris.RemoveAll(x => x == -1);


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
