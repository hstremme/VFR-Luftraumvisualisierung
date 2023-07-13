using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

public class routeManager : MonoBehaviour
{
    private Transform parent;

    public double3[] routeCPs;
    public GameObject checkpointPrefab;
    public GameObject splineRoutePrefab;
    

    //todo auf Checkpoint ändern
    private GameObject[] checkpoints;
    private GameObject routeSpline;
  
    // Start is called before the first frame update
    void Start()
    {
        
        // create CPs
        checkpoints = new GameObject[routeCPs.Length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i] = AddCheckpoint(routeCPs[i], "cp_" + i);
        }

        // create SplineRoute
        createSplineGameObject();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    GameObject AddCheckpoint(double3 position, string name)
    {
        parent = this.transform.parent.transform;

        GameObject checkpoint = Instantiate(checkpointPrefab);
        checkpoint.name = name;
        checkpoint.transform.SetParent(parent);

        CesiumGlobeAnchor anchor = checkpoint.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;
        //anchor.transform.localScale = new Vector3(400, 400, 400);

        return checkpoint;
    }



    void createSplineGameObject()
    {
        parent = this.transform.parent.transform;

        // create spline from prefab
        routeSpline = Instantiate(splineRoutePrefab);
        routeSpline.name = "routeSpline";
        routeSpline.transform.SetParent(parent);

        // position spline object
        CesiumGlobeAnchor anchor = routeSpline.GetComponent<CesiumGlobeAnchor>();
        // using the position of the first cp as origin
        anchor.longitudeLatitudeHeight = routeCPs[0];
        anchor.transform.localScale = new Vector3(1, 1, 1);

        // get spline component
        var spline = routeSpline.GetComponent<SplineContainer>().Spline;

        // get realativ positions
        //float3[] relativPositions = getRelativPositionsIncludingTheEarthCurbing(routeCPs[0], routeCPs);
        float3[] relativPositions = getRelativPositionsBetweenCheckpoints(checkpoints[0], checkpoints);

        // create knots
        BezierKnot[] cpKnots = new BezierKnot[relativPositions.Length];
        for (int i = 1; i < routeCPs.Length; i++)
        {
            cpKnots[i] = new BezierKnot(relativPositions[i]);
        }

        // set knots to spline
        spline.Knots = cpKnots;
    }

    /*
     * This methods returns the realativ positions including the earth curbin, between one origin and an array of coordinates.
     */
    float3[] getRelativPositionsIncludingTheEarthCurbing(double3 origin, double3[] coords)
    {

        CesiumGeoreference cesiumGeoreference = GetComponentInParent<CesiumGeoreference>();
        float3[] relativPositions = new float3[coords.Length];

        for (int i = 1; i < coords.Length; i++)
        {
            // get realtiv position
            double3 relativCoords = coords[i] - origin;

            // convert 
            double3 earthCenteredPosition = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(relativCoords);
            double3 unityPosition = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(earthCenteredPosition);

            relativPositions[i] = convertDouble3tofloat3(unityPosition);
        }

        return relativPositions;
    }

    float3[] getRelativPositionsBetweenCheckpoints(GameObject origin, GameObject[] coords) 
    {
        float3[] relativPositions = new float3[coords.Length];
        for (int i = 1; i < coords.Length; i++)
        {
            // get realtiv position
            relativPositions[i] = coords[i].transform.position - origin.transform.position;
            relativPositions[i].y *= -0.4f;

        }

        return relativPositions;
    }

    float3 convertDouble3tofloat3(double3 vector)
    {
        return new float3((float)vector.x, (float)vector.y, (float)vector.z);
    }
}


