using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupCP : MonoBehaviour
{
    private Transform parent;
    public float radius = 50f;
    public float height = 100f;

    // Start is called before the first frame update
    void Start()
    {
        parent = this.transform;

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(parent);
        cylinder.name = "cp_location_marker";
        cylinder.transform.position = new Vector3(0f,0f,0f);
        cylinder.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
