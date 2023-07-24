using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Globalization;

public class AirspacesRenderer : MonoBehaviour
{

    private DataLoaderJson dataLoaderJson;
    private CesiumGeoreference cesiumGeoreference;
    private CesiumGeoreference cesiumGeoreference2D;
    private Dictionary<ICAOClass, Material> airspaceMaterials = new Dictionary<ICAOClass, Material>();
    public Material airspaceMaterialIcaoClassA;
    public Material airspaceMaterialIcaoClassB;
    public Material airspaceMaterialIcaoClassC;
    public Material airspaceMaterialIcaoClassD;
    public Material airspaceMaterialIcaoClassE;
    public Material airspaceMaterialIcaoClassF;
    public Material airspaceMaterialIcaoClassG;
    public Material airspaceMaterialIcaoClassSUA;
    private Geoid geoid;

    void Awake() {
        dataLoaderJson = GameObject.Find("DataLoaderJson").GetComponent<DataLoaderJson>();
        cesiumGeoreference = GameObject.Find("CesiumGeoreference").GetComponent<CesiumGeoreference>();
        cesiumGeoreference2D = GameObject.Find("CesiumGeoreferenceMiniMap").GetComponent<CesiumGeoreference>();
    }

    // Start is called before the first frame update
    void Start()
    {
        geoid = GameObject.Find("DataLoader").GetComponent<Geoid>();
        airspaceMaterials[ICAOClass.A] = airspaceMaterialIcaoClassA;
        airspaceMaterials[ICAOClass.B] = airspaceMaterialIcaoClassB;
        airspaceMaterials[ICAOClass.C] = airspaceMaterialIcaoClassC;
        airspaceMaterials[ICAOClass.D] = airspaceMaterialIcaoClassD;
        airspaceMaterials[ICAOClass.E] = airspaceMaterialIcaoClassE;
        airspaceMaterials[ICAOClass.F] = airspaceMaterialIcaoClassF;
        airspaceMaterials[ICAOClass.G] = airspaceMaterialIcaoClassG;
        airspaceMaterials[ICAOClass.UNCLASSIFIED_OR_SUA] = airspaceMaterialIcaoClassSUA;
        dataLoaderJson.LoadAirspaces();
        RenderAirspaces();
    }

    void RenderAirspaces() {
        Airspace[] airspaces = dataLoaderJson.GetAirspaces();
        for(int i = 0; i < airspaces.Length; i++) {
            Airspace airspace = airspaces[i];

            AddAirspaceObject(airspace);
        }
    }

    void AddAirspaceObject(Airspace airspace) {
        double3[] positions = new double3[airspace.geometry.coordinates.Length];
        double3[] unityPositions = new double3[positions.Length];

        string[] firstCoords = airspace.geometry.coordinates[0].Split(" ");
        string firstLongitude = firstCoords[0];
        string firstLatitude = firstCoords[1];

        float lowerLimit = airspace.lowerLimit.value;
        if(airspace.lowerLimit.unit == HeightUnit.FL) {
            lowerLimit = FlightLevelHelper.FlightLevelToMeters(lowerLimit); // FL in meters
        }
        else if(airspace.lowerLimit.unit == HeightUnit.MSL) {
            double lon = double.Parse(firstLongitude, CultureInfo.InvariantCulture);
            double lat = double.Parse(firstLatitude, CultureInfo.InvariantCulture);
            lowerLimit = (float) (geoid.GetGeoid(lat, lon) + lowerLimit);
        }

        float upperLimit = airspace.upperLimit.value;
        if(airspace.upperLimit.unit == HeightUnit.FL) {
            upperLimit = FlightLevelHelper.FlightLevelToMeters(upperLimit); // FL in meters
        }
        else if(airspace.upperLimit.unit == HeightUnit.MSL) {
            double lon = double.Parse(firstLongitude, CultureInfo.InvariantCulture);
            double lat = double.Parse(firstLatitude, CultureInfo.InvariantCulture);
            upperLimit = (float) (geoid.GetGeoid(lat, lon) + upperLimit);
        }

        var temp = lowerLimit;
        if(lowerLimit > upperLimit) {
            lowerLimit = upperLimit;
            upperLimit = temp;
        }

        airspace.lowerLimit.inMeters = lowerLimit;
        airspace.upperLimit.inMeters = upperLimit;

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
            vertices[topIndex] = new Vector3(vertices[i].x, upperLimit - lowerLimit, vertices[i].z);

            normals[i] = Vector3.down;
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

        // 2D GameObject
        GameObject miniMapObject = new GameObject(airspace.name + " Polygon");
        miniMapObject.transform.SetParent(cesiumGeoreference2D.transform);
        LineRenderer polygonRenderer = miniMapObject.AddComponent<LineRenderer>();
        polygonRenderer.positionCount = vertices2D.Length;

        for(int currentPoint = 0; currentPoint < vertices2D.Length; currentPoint++) {
            polygonRenderer.SetPosition(currentPoint, vertices[currentPoint]);
        }

        polygonRenderer.loop = true;
        polygonRenderer.useWorldSpace = false;
        polygonRenderer.material = airspaceMaterials[airspace.icaoClass];
        polygonRenderer.widthMultiplier = 2000;

        CesiumGlobeAnchor anchor2D = miniMapObject.AddComponent<CesiumGlobeAnchor>();
        anchor2D.longitudeLatitudeHeight = new double3(positions[0].x, positions[0].y, 0);
        anchor2D.transform.localScale = new Vector3(1, 1, 1);
        miniMapObject.layer = LayerMask.NameToLayer("miniMap");

        // 3D GameObject
        GameObject gObject = new GameObject(airspace.name);
        gObject.transform.SetParent(cesiumGeoreference.transform);

        AirspaceComponent airspaceComponent = gObject.AddComponent<AirspaceComponent>();
        airspaceComponent.airspaceData = airspace;

        Mesh mesh = new Mesh();

        MeshFilter meshFilter = gObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        mesh.vertices = vertices;
        mesh.triangles = tris.ToArray();
        mesh.normals = normals;

        MeshRenderer meshRenderer = gObject.AddComponent<MeshRenderer>();
        meshRenderer.material = airspaceMaterials[airspace.icaoClass];

        MeshCollider meshCollider = gObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
        gObject.layer = LayerMask.NameToLayer("Zone");

        CesiumGlobeAnchor anchor = gObject.AddComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = positions[0];
        anchor.transform.localScale = new Vector3(1, 1, 1);
    }
}
