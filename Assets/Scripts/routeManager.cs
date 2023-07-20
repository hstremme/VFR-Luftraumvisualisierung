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
    public GameObject miniMapSplineRoutePrefab;

    // Geoid
    public GameObject dataLoader;
    private Geoid Geoid;

    //miniMap
    public GameObject miniMapGeoRef;

    private GameObject[] checkpoints;
    private GameObject[] miniMapCps;
    private GameObject routeSpline;
    private GameObject miniMapRouteSpline;
    public GameObject activeCP;

    private int flag;
    // Start is called before the first frame update
    void Start()
    {

        // get geoid script refrenz
        Geoid = dataLoader.GetComponent<Geoid>();

        flag = 0;

        // create CPs
        checkpoints = new GameObject[inputRouteCPs.Length];
        miniMapCps = new GameObject[inputRouteCPs.Length];

        for (int i = 0; i < checkpoints.Length; i++)
        {
            (checkpoints[i], miniMapCps[i]) = AddCheckpoint(inputRouteCPs[i], "cp_" + i);
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
        // create SplineRoutes
        routeSpline = createSplineGameObject( this.transform.parent.transform, checkpoints, "routeSpline", splineRoutePrefab, 20);
        miniMapRouteSpline = createSplineGameObject( miniMapGeoRef.transform, miniMapCps, "miniMapRouteSpline", miniMapSplineRoutePrefab, 200);
        miniMapRouteSpline.layer = LayerMask.NameToLayer("miniMap");

    }

    (GameObject world, GameObject minimap) AddCheckpoint(double3 position, string name)
    {
        parent = this.transform.parent.transform;

        // add Cp to 3D map
        GameObject checkpoint = Instantiate(checkpointPrefab);
        checkpoint.name = name;
        checkpoint.transform.SetParent(parent);

        // add geoid to height
        double geoid = Geoid.GetGeoid(position[1], position[0]);
        position[2] = position[2] + geoid ;

        CesiumGlobeAnchor anchor = checkpoint.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;

        // add cp to 3D map
        GameObject miniMapCheckpoint = Instantiate(checkpointPrefab);
        miniMapCheckpoint.name = "mM_"+name;
        miniMapCheckpoint.transform.SetParent(miniMapGeoRef.transform);
        CesiumGlobeAnchor miniMapAnchor = miniMapCheckpoint.GetComponent<CesiumGlobeAnchor>();
        miniMapAnchor.longitudeLatitudeHeight = position;
        miniMapAnchor.transform.localScale = new Vector3(10000,100,10000);
        miniMapCheckpoint.layer = LayerMask.NameToLayer("miniMap");


        return (checkpoint, miniMapCheckpoint);
    }



    GameObject createSplineGameObject(Transform parent, GameObject[] cps, String name, GameObject Prefab, int radius)
    {
        //parent = this.transform.parent.transform;

        // create spline from prefab
        GameObject splineObject = Instantiate(Prefab);
        splineObject.name = name;
        splineObject.transform.SetParent(parent);

        // position spline object
        CesiumGlobeAnchor anchor = splineObject.GetComponent<CesiumGlobeAnchor>();
        // using the position of the first cp as origin
        anchor.longitudeLatitudeHeight = inputRouteCPs[0];
        anchor.transform.localScale = new Vector3(1, 1, 1);

        updateSpline(splineObject, cps, radius);
        return splineObject;
    }

    void updateSpline(GameObject splineObject, GameObject[] cps, int radius)
    {
        // get all Knots of the spline
        var spline = splineObject.GetComponent<SplineContainer>().Spline;
        BezierKnot[] cpKnots = getRelativKnotPositionsOfCheckpoints(cps);

        // set knots to spline
        spline.Knots = cpKnots;
        splineObject.transform.rotation = Quaternion.identity;
        splineObject.GetComponent<SplineExtrude>().Radius = radius;
        splineObject.GetComponent<SplineExtrude>().Rebuild();
    }

    /*
     * This methods returns the realativ positions between the first and all other cps
     */
    BezierKnot[] getRelativKnotPositionsOfCheckpoints(GameObject[] cps)
    {
        BezierKnot[] relativPositions = new BezierKnot[cps.Length];


        for (int i = 0; i < cps.Length; i++)
        {
            // get realtiv position
            relativPositions[i] = new BezierKnot(cps[i].transform.position - cps[0].transform.position);
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
        updateSpline(routeSpline, checkpoints, 80);
    }

}


