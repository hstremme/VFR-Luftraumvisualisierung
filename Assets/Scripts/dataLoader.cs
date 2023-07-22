using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using System.Xml;
using System;
using System.Globalization;
using UnityEngine.UIElements;
using System.Collections;
using System.IO;
using System.Linq;

public class dataLoader : MonoBehaviour
{
    public GameObject dynamicCamera;

    public Material airportMaterial;

    public Material helipadMaterial;

    public Material obstaclePrefab;

    public Material windmillMat;
    
    private Geoid geoid;

    private List<GameObject> airportsList = new List<GameObject>();

    private GameObject container;

    private double lastCamHeight = 0;

    // Start is called before the first frame update
    void Start()
    {
        geoid = this.GetComponentInParent<Geoid>();

        container = new GameObject("Container");
        container.transform.SetParent(this.transform.parent.transform);
        container.AddComponent<CesiumSubScene>();
        container.GetComponent<CesiumSubScene>().activationRadius = 10000000;
        container.GetComponent<CesiumSubScene>().latitude = 51.162408881414336;
        container.GetComponent<CesiumSubScene>().longitude = 10.445476086596582;

        AddAirports();
        AddObstacles();
    }

    // Update is called once per frame
    void Update()
    {
        double camHeight = dynamicCamera.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight.z;
        if (lastCamHeight > 150000 && camHeight < 150000)
        {
                foreach(var airport in airportsList)
                {
                    airport.GetComponent<CesiumGlobeAnchor>().scaleEastUpNorth = 200;
                }
        } 
        else if (lastCamHeight < 150000 && camHeight > 150000)
        {
                foreach(var airport in airportsList)
                {
                    airport.GetComponent<CesiumGlobeAnchor>().scaleEastUpNorth = 500;
                }
        }
        else if (lastCamHeight > 20000 && camHeight < 20000)
        {
                foreach(var airport in airportsList)
                {
                    airport.GetComponent<CesiumGlobeAnchor>().scaleEastUpNorth = 80;
                }
        }
        else if (lastCamHeight < 20000 && camHeight > 20000)
        {
                foreach(var airport in airportsList)
                {
                    airport.GetComponent<CesiumGlobeAnchor>().scaleEastUpNorth = 200;
                }
        }
        lastCamHeight = camHeight;
    }

    /*
     * Adds all Airports and Heliport from the XML to the Scene
     */
    void AddAirports()
    {
        XmlDocument airportXml = new XmlDocument();
        airportXml.Load("Assets/Data/ED_AirportHeliport_2023-05-18_2023-05-18_snapshot.xml");
        XmlDocument runwayXml = new XmlDocument();
        runwayXml.Load("Assets/Data/ED_Runway_2023-07-13_2023-07-13_snapshot.xml");
        XmlNamespaceManager nsmgr = CreateXmlNsmng(runwayXml);

        XmlNodeList airportXmlList = airportXml.GetElementsByTagName("aixm:AirportHeliport");
        for (int i = 0; i < airportXmlList.Count; i++)
        {
            var xmlPart = airportXmlList.Item(i)["aixm:timeSlice"];
            string name = xmlPart.GetElementsByTagName("aixm:name").Item(0).InnerText;
            string[] pos = xmlPart.GetElementsByTagName("gml:pos").Item(0).InnerText.Split(' ');
            string type = xmlPart.GetElementsByTagName("aixm:type").Item(0).InnerText;
            string icao = xmlPart.GetElementsByTagName("aixm:locationIndicatorICAO").Item(0).InnerText;
            double lon = double.Parse(pos[0], CultureInfo.InvariantCulture);
            double lat = double.Parse(pos[1], CultureInfo.InvariantCulture);
            // Set default height to 50 meters
            double height = 50;
            string strHeight = airportXmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("aixm:elevation").Item(0).InnerText;
            if (!strHeight.Equals(""))
            {
                height = double.Parse(strHeight, CultureInfo.InvariantCulture);
                // feet to meters
                height *= 0.3048;
            }
            double geoidHeight = geoid.GetGeoid(lon, lat);
            // add height(msl) to geoid and additonal margin
            height = height + geoidHeight + 5;
            double3 position = new double3(lat, lon, height);
            Material objectMaterial = airportMaterial;
            int rotation = 0;
            // Use helipad material
            if (type.Equals("HP"))
            {
                objectMaterial = helipadMaterial;
            }
            // get object rotation from the runway designator
            else if (type.Equals("AD") || type.Equals("AH"))
            {
                // searches for the associated airport by ICAO in runway data
                XmlNode linkedAirportNode = runwayXml.DocumentElement.SelectSingleNode($"//aixm:associatedAirportHeliport[@xlink:title='{icao}']", nsmgr);
                if (linkedAirportNode != null)
                {
                    string designator = linkedAirportNode.ParentNode.SelectSingleNode("aixm:designator", nsmgr).InnerText;
                    // pareses designator to degrees (07/25 -> 70°)
                    rotation = int.Parse(designator.Substring(0, 2)) * 10;
                }
            }
            GameObject airportObject = AnchorNewObject(position, name, PrimitiveType.Plane, objectMaterial, container.transform, rotation);
            AirportInfo info = airportObject.AddComponent<AirportInfo>();
            info.name = name;
            info.type = type;
            this.airportsList.Add(airportObject);
        }
    }

    /*
     * Adds all obstacles to scene 
     */
    void AddObstacles()
    {
        XmlDocument obstacleXml = new XmlDocument();
        obstacleXml.Load("Assets/Data/ED_Obstacles_Area_1_2023-06-15_2023-06-15_snapshot.xml");
        XmlNodeList obstacleXmlList = obstacleXml.GetElementsByTagName("aixm:VerticalStructureTimeSlice");
        XmlNamespaceManager nsmgr = CreateXmlNsmng(obstacleXml);

        foreach(XmlNode obst in obstacleXmlList)
        {
            string name = obst.SelectSingleNode("aixm:name", nsmgr).InnerText;
            XmlNodeList partList = obst.SelectNodes("aixm:part", nsmgr);
            foreach(XmlNode part in partList)
            {
                Material mat = obstaclePrefab;
                string type = part.SelectSingleNode("aixm:VerticalStructurePart//aixm:note", nsmgr).InnerText.Split(":")[1].Trim();
                switch (type)
                {
                    case "WINDMILL":
                        mat = windmillMat; 
                        break;
                    case "TOWER":
                        mat = Resources.Load("Materials/Tower") as Material;
                        break;
                    case "SPIRE":
                        mat = Resources.Load("Materials/Spire") as Material;
                        break;
                    case "BUILDING":
                        mat = Resources.Load("Materials/Building") as Material;
                        break;
                    case "ANTENNA":
                        mat = Resources.Load("Materials/Antenna") as Material;
                        break;
                    case "STACK":
                        mat = Resources.Load("Materials/Stack") as Material;
                        break;
                }
                Debug.Log(type);
                string[] pos = part.SelectSingleNode("aixm:VerticalStructurePart//gml:pos", nsmgr).InnerText.Split(' ');
                double lon = double.Parse(pos[0], CultureInfo.InvariantCulture);
                double lat = double.Parse(pos[1], CultureInfo.InvariantCulture);
                double height = 50;
                string strHeight = part.SelectSingleNode("aixm:VerticalStructurePart//aixm:elevation", nsmgr).InnerText;
                if (!strHeight.Equals(""))
                {
                    height = double.Parse(strHeight, CultureInfo.InvariantCulture);
                    // feet to meters
                    height *= 0.3048;
                }
                double geoidHeight = geoid.GetGeoid(lon, lat);
                height = height + geoidHeight;
                double3 position = new double3(lat, lon, height);
                AnchorNewObject(position, name, PrimitiveType.Cylinder, mat, container.transform);
            }
        }
    }

    private GameObject AnchorNewObject(double3 position, string name, PrimitiveType primitiveType, Material material, Transform parent, int rotation = 0)
    {
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent);

        go.GetComponent<MeshRenderer>().material = material;

        go.AddComponent<CesiumGlobeAnchor>();
        CesiumGlobeAnchor anchor = go.GetComponent<CesiumGlobeAnchor>();
        anchor.longitudeLatitudeHeight = position;
        anchor.transform.localScale = new Vector3(100, 1000, 100);
        anchor.rotationEastUpNorth *= Quaternion.Euler(Vector3.up * rotation);
        anchor.scaleEastUpNorth = new double3(500, 500, 500);
        
        return go;
    }
    
    private XmlNamespaceManager CreateXmlNsmng(XmlDocument xmlDocument)
    {
        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);
        nsmgr.AddNamespace("gss", "http://www.isotc211.org/2005/gss");
        nsmgr.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        nsmgr.AddNamespace("message", "http://www.aixm.aero/schema/5.1.1/message");
        nsmgr.AddNamespace("gsr", "http://www.isotc211.org/2005/gsr");
        nsmgr.AddNamespace("gco", "http://www.isotc211.org/2005/gco");
        nsmgr.AddNamespace("gml", "http://www.opengis.net/gml/3.2");
        nsmgr.AddNamespace("gmd", "http://www.isotc211.org/2005/gmd");
        nsmgr.AddNamespace("aixm", "http://www.aixm.aero/schema/5.1.1");
        nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");
        nsmgr.AddNamespace("gts", "http://www.isotc211.org/2005/gts");
        return nsmgr;
    }


}

