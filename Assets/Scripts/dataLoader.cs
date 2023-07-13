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

    [SerializeField]
    public Material areodromMaterial;
    
    [SerializeField]
    public XmlDocument areodromSources;

    private Transform parent;

    private Dictionary<string, List<double[]>> geoidDict = new Dictionary<string, List<double[]>>();

    // Start is called before the first frame update
    // TODO global variable für zusätzliche höhe
    void Start()
    {
        InitGeoidDict();
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load("Assets/Data/ED_AirportHeliport_2023-05-18_2023-05-18_snapshot.xml");
        XmlNodeList xmlList = xmlDoc.GetElementsByTagName("aixm:AirportHeliport");
        for (int i = 0;  i < xmlList.Count; i++)
        {
            string name = xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("aixm:name").Item(0).InnerText;
            string[] pos = xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("gml:pos").Item(0).InnerText.Split(' ');
            double lon = double.Parse(pos[0], CultureInfo.InvariantCulture);
            double lat = double.Parse(pos[1], CultureInfo.InvariantCulture);
            double height = 0;
            try
            {
                height = double.Parse(xmlList.Item(i)["aixm:timeSlice"].GetElementsByTagName("aixm:elevation").Item(0).InnerText, CultureInfo.InvariantCulture);
                height = height * 0.3048;
            }
            catch
            {
                height = 50;
            }
            double geoid = GetGeoid(lon, lat);
            height = height + geoid; 
            double3 position = new double3(lat, lon, height);
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

    private void InitGeoidDict()
    {
        List<double[]> part = new List<double[]>();
        string identifier = null;
        var lines = File.ReadLines("Assets/Data/geoid.csv").Skip(1).ToList();
        string last = lines.Last();
        foreach (var line in lines)
        {
            string[] rows = line.Split(',');
            string key = Math.Floor(double.Parse(rows[1], CultureInfo.InvariantCulture) * 100) / 100 +
                "-" +
                Math.Floor(double.Parse(rows[2], CultureInfo.InvariantCulture) * 10) / 10;
            double[] value = new double[]
            {
                double.Parse(rows[1], CultureInfo.InvariantCulture),
                double.Parse(rows[2], CultureInfo.InvariantCulture),
                double.Parse(rows[3], CultureInfo.InvariantCulture)
            };
            if (!key.Equals(identifier) && identifier != null)
            {
                try
                {
                    geoidDict.Add(identifier, new List<double[]>(part));
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
                part.Clear();
            }
            part.Add(value);
            identifier = key;
        }

    }

    public double GetGeoid(double lon, double lat)
    {
        string searchKey = Math.Floor(lon * 100) / 100 +
                "-" +
                Math.Floor(lat * 10) / 10;
        List<double[]> region = null;
        try
        {
            region = this.geoidDict[searchKey];
        }
        catch
        {
            return 0;
        }
        double lastDiff = 99999;
        double lastHeight = 0;
        foreach (double[] row in region)
        {
            double diff = Math.Abs(lat - row[1]);
            if (diff > lastDiff)
            {
                return lastHeight;
            }
            lastDiff = diff;
            lastHeight = row[2];
        }
        return lastHeight;
    }

}

