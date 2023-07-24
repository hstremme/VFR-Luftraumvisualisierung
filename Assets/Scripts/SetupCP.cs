using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using CesiumForUnity;

public class SetupCP : MonoBehaviour
{
    private Transform parent;
    public Material cpPositonMarker;
    public Material activeCpMaterial;
    public Material baseCpMaterial;
    private CesiumGlobeAnchor anchor;
    private CesiumGlobeAnchor cylinderAnchor;

    private Renderer renderer;
    private GameObject cylinder;
    
    void Awake() {
        anchor = GetComponent<CesiumGlobeAnchor>();
    }

    // Start is called before the first frame update
    void Start()
    {
        parent = this.transform.parent.transform;
        
        // create cylinder as CP Position marker 
        cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(parent, false);
        cylinder.name = this.name + "_location_marker";
        cylinder.layer = gameObject.layer;
        cylinder.transform.localScale = transform.localScale;
        cylinder.GetComponent<Renderer>().material = cpPositonMarker;
        cylinderAnchor = cylinder.AddComponent<CesiumGlobeAnchor>();
        StartCoroutine(DelayUpdate());
    }

    public void UpdateLocationMarker() {
        float height = (float) anchor.longitudeLatitudeHeight.z;
        cylinderAnchor.longitudeLatitudeHeight = new double3(anchor.longitudeLatitudeHeight.x, anchor.longitudeLatitudeHeight.y, height / 2);
        cylinder.transform.localScale = new Vector3(transform.localScale.x, height / 2, transform.localScale.z);
    }

    IEnumerator DelayUpdate() {
        yield return new WaitForSeconds(0.1f);
        UpdateLocationMarker();
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
