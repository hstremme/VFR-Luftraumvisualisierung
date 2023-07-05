using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Airspace
{
    public string _id;
    public string name;
    public int type;
    public int icaoClass;
    public bool onDemand;
    public bool onRequest;
    public bool byNotam;
    public bool specialAgreement;
    public Geometry geometry;
    public string country;
    public DateTime createdAt;
    public DateTime updatedAt;
    public string createdBy;
    public string updatedBy;
    public int activity;
    public HoursOfOperation hoursOfOperation;
}
