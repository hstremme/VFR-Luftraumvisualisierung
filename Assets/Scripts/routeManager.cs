using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;

public class routeManager: MonoBehaviour
{
    public double3[] routeCPs;

    public GameObject checkpointPrefab;

    //todo auf Checkpoint ändern
    private GameObject[] checkpoints;

    private Transform parent;


    // Start is called before the first frame update
    void Start()
    {
        //create CPs
        checkpoints[0] = AddCheckpoint(routeCPs[0], "cp_1");
        checkpoints[1] = AddCheckpoint(routeCPs[1], "cp_2");

        //draw splines
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
        anchor.transform.localScale = new Vector3(400, 400, 400);

        return checkpoint;
    }

    void CreateSplines()
    {
        // Add a SplineContainer component to this GameObject.
        var container = gameObject.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();
    }
}

