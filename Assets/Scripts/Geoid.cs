using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class Geoid : MonoBehaviour
{
    private Dictionary<string, List<double[]>> geoidDict = new Dictionary<string, List<double[]>>();

    void Awake()
    {
        InitGeoidDict();        
    }

    private void InitGeoidDict()
    {
        List<double[]> part = new List<double[]>();
        string identifier = null;
        var lines = File.ReadLines(Application.streamingAssetsPath + "/geoid.csv").Skip(1).ToList();
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

    public double GetGeoid(double lat, double lon)
    {
        string searchKey = Math.Floor(lat * 100) / 100 +
                "-" +
                Math.Floor(lon * 10) / 10;
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
