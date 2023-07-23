using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OperatingHours
{
    
    public int dayOfWeek;
    public string startTime;
    public string endTime;
    public bool byNotam;
    public bool sunrise;
    public bool sunset;
    public bool publicHolidaysExcluded;

}
