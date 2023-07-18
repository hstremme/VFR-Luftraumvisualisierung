using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class SetupCP : MonoBehaviour
{
    private Transform parent;
    public float cpHeight = 1200;
    public Material cpPositonMarker;
    public Material activeCpMaterial;
    public Material baseCpMaterial;

    private Renderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        parent = this.transform;
        
        // create cylinder as CP Position marker 
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(parent, false);
        cylinder.name = this.name + "_location_marker";
        cylinder.transform.localScale = new Vector3(0.3f, 0.002f*cpHeight, 0.3f);
        cylinder.transform.Translate(new Vector3(0, -cpHeight, 0));
        cylinder.GetComponent<Renderer>().material = cpPositonMarker;       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setCPMaterialStatus(bool isActive)
    {
        renderer = this.GetComponent<MeshRenderer>();

        if (isActive)
        {
            renderer.material = activeCpMaterial;
        } else { 
            renderer.material = baseCpMaterial;
        }
    }
 
}
