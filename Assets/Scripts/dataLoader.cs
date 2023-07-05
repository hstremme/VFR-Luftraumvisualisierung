using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Xml;
using System;
using System.Globalization;
using UnityEngine.UIElements;

public class dataLoader : MonoBehaviour
{

    [SerializeField]
    public Material areodromMaterial;
    
    [SerializeField]
    public XmlDocument areodromSources;

    private Transform parent;

    // Start is called before the first frame update
    void Start()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load("Assets/Data/ED_AirportHeliport_2023-05-18_2023-05-18_snapshot.xml");
        XmlNodeList xmlList = xmlDoc.GetElementsByTagName("aixm:AirportHeliport");
        for (int i = 0;  i < xmlList.Count; i++)
        {
            string[] pos = xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("gml:pos").Item(0).InnerText.Split(' ');
            string height = xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("aixm:elevation").Item(0).InnerText;
            if (height.Length == 0)
            {
                height = "250";
            }
            double3 position = new double3(
                double.Parse(pos[1], CultureInfo.InvariantCulture), 
                double.Parse(pos[0], CultureInfo.InvariantCulture), 
                double.Parse(height, CultureInfo.InvariantCulture));
            string name = xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("aixm:name").Item(0).InnerText;
            AddAreodromeSprite(position, name);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void AddAreodromeSprite(double3 position, string name)
    {
        parent = this.transform.parent.transform;

        GameObject areodromSprite = GameObject.CreatePrimitive(PrimitiveType.Plane);
        areodromSprite.name = name;
        areodromSprite.transform.SetParent(parent);

        areodromSprite.GetComponent<MeshRenderer>().material = areodromMaterial;

        areodromSprite.AddComponent<CesiumGlobeAnchor>();
        CesiumGlobeAnchor anchor = areodromSprite.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;
        anchor.transform.localScale = new Vector3(100, 1000, 100);
    }
}
