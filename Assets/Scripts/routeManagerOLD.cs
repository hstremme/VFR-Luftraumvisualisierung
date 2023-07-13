using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

public class routeManagerOLD : MonoBehaviour
{
    private Transform parent;

    public double3[] routeCPs;
    public GameObject checkpointPrefab;
    public GameObject routeSplinePrefab;
    public SplineContainer splineContainer;
    

    //todo auf Checkpoint ändern
    private GameObject[] checkpoints;
    private Spline spline;
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


        createSplineGameObject();

        // Create a new Spline on the SplineContainer.
        //spline = splineContainer.AddSpline();

        // set knots on spline
        //setKnotsOnSpline(checkpoints);

    }

    // Update is called once per frame
    void Update()
    {
        // update spline
        //setKnotsOnSpline(checkpoints);
    }

    GameObject AddCheckpoint(double3 position, string name)
    {
        parent = this.transform.parent.transform;

        GameObject checkpoint = Instantiate(checkpointPrefab);
        checkpoint.name = name;
        checkpoint.transform.SetParent(parent);

        CesiumGlobeAnchor anchor = checkpoint.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;
        anchor.transform.localScale = new Vector3(400, 400, 400);

        return checkpoint;
    }

    void setKnotsOnSpline(GameObject[] checkpoints)
    {
        BezierKnot[] cpKnots = new BezierKnot[checkpoints.Length];

        for (int i = 0; i < checkpoints.Length; i++)
        {
            cpKnots[i] = new BezierKnot(checkpoints[i].transform.TransformPoint(parent.transform.position));
        }

        // add knots to spline
        spline.Knots = cpKnots;
    }

    void createSplineGameObject()
    {
        parent = this.transform.parent.transform;

        routeSpline = Instantiate(checkpointPrefab);
        routeSpline.name = "routeSpline";
        routeSpline.transform.SetParent(parent);

        CesiumGlobeAnchor anchor = routeSpline.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = routeCPs[0];
    }
}


