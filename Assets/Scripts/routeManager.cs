using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using Unity.VisualScripting;
using System;

public class routeManager : MonoBehaviour
{
    private Transform parent;

    public double3[] inputRouteCPs;
    public GameObject checkpointPrefab;
    public GameObject splineRoutePrefab;

    private GameObject[] checkpoints;
    private GameObject routeSpline;
    public GameObject activeCP;

    private int flag;
    // Start is called before the first frame update
    void Start()
    {

        flag = 0;

        // create CPs
        checkpoints = new GameObject[inputRouteCPs.Length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            checkpoints[i] = AddCheckpoint(inputRouteCPs[i], "cp_" + i);
        }

        // set first active cp
        activeCP = checkpoints[0];
        activeCP.GetComponent<SetupCP>().setCPMaterialStatus(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (flag == 1)
        {
            afterCesiumAnchorSetup();
            flag += 1;

        }
        else if (flag == 0)
        {
            flag += 1;
        }

        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            changeToNextCP(true);
        }

        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            changeToNextCP(false);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            adjustCPHeight( 100);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            adjustCPHeight( -100);
        }
    }

    void afterCesiumAnchorSetup()
    {
        // create SplineRoute
        createSplineGameObject();

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
        anchor.longitudeLatitudeHeight = inputRouteCPs[0];
        anchor.transform.localScale = new Vector3(1, 1, 1);

        updateSpline();
    }

    void updateSpline()
    {
        // get all Knots of the spline
        var spline = routeSpline.GetComponent<SplineContainer>().Spline;
        BezierKnot[] cpKnots = getRelativKnotPositionsOfCheckpoints();

        // set knots to spline
        spline.Knots = cpKnots;
        routeSpline.transform.rotation = Quaternion.identity;
        routeSpline.GetComponent<SplineExtrude>().Rebuild();
    }

    /*
     * This methods returns the realativ positions between the first and all other cps
     */
    BezierKnot[] getRelativKnotPositionsOfCheckpoints()
    {
        BezierKnot[] relativPositions = new BezierKnot[checkpoints.Length];


        for (int i = 0; i < checkpoints.Length; i++)
        {
            // get realtiv position
            relativPositions[i] = new BezierKnot(checkpoints[i].transform.position - checkpoints[0].transform.position);
        }

        return relativPositions;
    }

    void setAsActiveCP(GameObject cp)
    {
        // reset material of old cp
        activeCP.GetComponent<SetupCP>().setCPMaterialStatus(false);

        // set new active cp
        activeCP = cp;
        activeCP.GetComponent<SetupCP>().setCPMaterialStatus(true);
    }

    void changeToNextCP(bool increment)
    {
        // get Index of next ActiveCP
        int i = Array.IndexOf(checkpoints, activeCP);
        if (increment)
        {
            i += 1;
        }
        else
        {
            i -= 1;
        }

        i = Math.Clamp(i, 0, checkpoints.Length - 1);
        setAsActiveCP(checkpoints[i]);
    }

    void adjustCPHeight(int interval)
    {
        // get new Position
        double3 position = activeCP.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight;
        position[2] += interval;

        // set new Position
        activeCP.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = position;

        // update spline
        updateSpline();
    }

}


