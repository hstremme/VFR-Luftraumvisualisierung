using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;

public class miniMapScript : MonoBehaviour
{
    CesiumGlobeAnchor anchor;
    CesiumGlobeAnchor anchorMiniMap;
    // Start is called before the first frame update
    void Start()
    {
        anchor = GameObject.Find("DynamicCamera").GetComponent<CesiumGlobeAnchor>();
        anchorMiniMap = this.gameObject.GetComponent<CesiumGlobeAnchor>();
    }

    // Update is called once per frame
    void Update()
    {
        anchorMiniMap.longitudeLatitudeHeight = anchor.longitudeLatitudeHeight;
        //anchorMiniMap.rotationEastUpNorth = anchor.rotationEastUpNorth;


    }
}
