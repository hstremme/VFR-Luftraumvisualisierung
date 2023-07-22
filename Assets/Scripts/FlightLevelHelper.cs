using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightLevelHelper
{
    public static float MetersToFlightLevel(double meters) {
        return (float) meters * 3.28084F / 100;
    }

    public static float FlightLevelToMeters(double flightLevel) {
        return (float) flightLevel * 100 / 3.28084F;
    }
}
