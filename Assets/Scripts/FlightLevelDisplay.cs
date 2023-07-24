using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using CesiumForUnity;
using Unity.Mathematics;

public class FlightLevelDisplay : MonoBehaviour
{
    private RectTransform flightLevelBackground;

    private double displayHeight = 0.3;
    public int flightLevelRange = 500;
    private float flightLevelRangeInMeters;
    private float flightLevelRadiusInMeters;

    private CesiumGlobeAnchor dynamicCameraAnchor;
    private CesiumGeoreference cesiumGeoreference;
    private GameObject flightLevelTextObject;
    private TMP_Text flightLevelText;

    void Awake() {
        this.flightLevelBackground = GameObject.Find("Background").GetComponent<RectTransform>();
        this.dynamicCameraAnchor = GameObject.Find("DynamicCamera").GetComponent<CesiumGlobeAnchor>();
        this.cesiumGeoreference = GameObject.Find("CesiumGeoreference").GetComponent<CesiumGeoreference>();
        this.flightLevelTextObject = GameObject.Find("CP FL Text");
        this.flightLevelText = flightLevelTextObject.GetComponent<TMP_Text>();
    }

    // Start is called before the first frame update
    void Start()
    {
        displayHeight = flightLevelBackground.rect.height;
        flightLevelRangeInMeters = FlightLevelHelper.FlightLevelToMeters(flightLevelRange);
        flightLevelRadiusInMeters = flightLevelRangeInMeters / 2;
    }

    public void UpdateCheckpoint(GameObject checkpoint) {
        // TODO: replace with current checkpoint's position
        CesiumGlobeAnchor checkpointAnchor = checkpoint.GetComponent<CesiumGlobeAnchor>();
        double3 positionLLH = checkpointAnchor.longitudeLatitudeHeight;
        double3 positionExtendedByRadiusLLH = checkpointAnchor.longitudeLatitudeHeight;
        positionExtendedByRadiusLLH.z += flightLevelRangeInMeters * 2; // why 2? idek, just moooore
        double3 positionEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(positionExtendedByRadiusLLH);
        double3 position = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(positionEcef);

        double3 belowPositionLLH = new double3(positionLLH.x, positionLLH.y, positionLLH.z - flightLevelRadiusInMeters);
        double3 belowPositionEcef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(belowPositionLLH);
        double3 belowPosition = cesiumGeoreference.TransformEarthCenteredEarthFixedPositionToUnity(belowPositionEcef);

        Vector3 positionVect = FlightLevelDisplay.Double3ToVector3(position);
        Vector3 belowPositionVect = FlightLevelDisplay.Double3ToVector3(belowPosition);
        RaycastHit[] hits = Physics.RaycastAll(new Ray(positionVect, belowPositionVect - positionVect), flightLevelRangeInMeters * 4, LayerMask.GetMask("Zone")); // max range in meters?

        float currentFL = (int) FlightLevelHelper.MetersToFlightLevel(positionLLH.z);
        flightLevelText.text = "FL " + (currentFL >= 9000 ? "9k+" : currentFL);

        GameObject[] zoneRepresentations = GameObject.FindGameObjectsWithTag("ZoneRepresentation");

        foreach (GameObject zone in zoneRepresentations) {
            Destroy(zone);
        }

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Renderer rend = hit.transform.GetComponent<Renderer>();
            GameObject zone = hit.transform.gameObject;
            if (zone)
            {
                AirspaceComponent airspaceComponent = zone.GetComponent<AirspaceComponent>();

                if(airspaceComponent) {
                    Airspace airspace = airspaceComponent.airspaceData;

                    // Calculate zone height relative to the display's height
                    var zoneHeightInMeters = airspace.upperLimit.inMeters - airspace.lowerLimit.inMeters;
                    float zoneHeightInFlightLevelDisplayFactor = (float) flightLevelRangeInMeters / (float) zoneHeightInMeters;
                    float zoneHeightInFlightLevelDisplay = (float) displayHeight / zoneHeightInFlightLevelDisplayFactor;
                    
                    CesiumGlobeAnchor zoneAnchor = zone.GetComponent<CesiumGlobeAnchor>();

                    // This is set to either the top or the bottom border's height of the zone (the closer one to the current position)
                    var zoneRelevantPositionalHeight = zoneAnchor.longitudeLatitudeHeight.z;
                    var topBorder = zoneRelevantPositionalHeight + airspace.upperLimit.inMeters - airspace.lowerLimit.inMeters;
                    bool zoneIsLowerThanPosition = topBorder < positionLLH.z;
                    if(zoneIsLowerThanPosition) {
                        zoneRelevantPositionalHeight = topBorder;
                    }
                    
                    if(Mathf.Abs((float) zoneRelevantPositionalHeight - (float) positionLLH.z) > flightLevelRadiusInMeters && !(zoneAnchor.longitudeLatitudeHeight.z <= positionLLH.z && zoneAnchor.longitudeLatitudeHeight.z + airspace.upperLimit.inMeters - airspace.lowerLimit.inMeters >= positionLLH.z)) {
                        continue;
                    }

                    GameObject zoneRepresentation = new GameObject();
                    zoneRepresentation.name = airspace.name;
                    zoneRepresentation.transform.parent = transform;
                    // Tag the zone representation for later removal (see for-loop above)
                    zoneRepresentation.tag = "ZoneRepresentation";
                    zoneRepresentation.AddComponent<CanvasRenderer>();

                    RectTransform rectTransform = zoneRepresentation.AddComponent<RectTransform>();
                    rectTransform.localScale = new Vector3(1, 1, 1);
                    // Center the anchor vertically and set it to the right horizontally 
                    rectTransform.anchorMin = new Vector2(1, .5F);
                    rectTransform.anchorMax = new Vector2(1, .5F);
                    // Set rect size
                    rectTransform.sizeDelta = new Vector2(0.05F, zoneHeightInFlightLevelDisplay);   

                    // Calculate zone position relative to the display's height
                    float distanceFromCheckpointFactor = (float) flightLevelRangeInMeters / (float) (zoneRelevantPositionalHeight - positionLLH.z);
                    float distanceFromCheckpoint = (float) displayHeight / distanceFromCheckpointFactor;
                    float zoneAnchoredPositionY = distanceFromCheckpoint + rectTransform.sizeDelta.y / (zoneIsLowerThanPosition ? -2 : 2);
                    rectTransform.anchoredPosition = new Vector2(-0.05F, zoneAnchoredPositionY);

                    // Clear bottom overflow
                    float lowestZoneRepresentationPoint = zoneAnchoredPositionY - rectTransform.sizeDelta.y / 2;
                    bool bottomOverflowing = lowestZoneRepresentationPoint < -displayHeight / 2;
                    if(bottomOverflowing) {
                        var overflow = (float) -displayHeight / 2 - lowestZoneRepresentationPoint;
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y - overflow);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y + overflow / 2);
                    }

                    // Clear top overflow
                    float highestZoneRepresentationPoint = rectTransform.anchoredPosition.y + rectTransform.sizeDelta.y / 2;
                    bool topOverflowing = highestZoneRepresentationPoint > displayHeight / 2;
                    if(topOverflowing) {
                        var overflow = (float) highestZoneRepresentationPoint - (float) displayHeight / 2;
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y - overflow);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y - overflow / 2);
                    }

                    // Copy the zone's color
                    Color zoneColor = zone.GetComponent<Renderer>().material.color;
                    zoneColor.a = .8F;

                    // Set airspace color in FL display
                    RawImage image = zoneRepresentation.AddComponent<RawImage>();
                    image.color = zoneColor;

                    // Place FL annotations
                    GameObject flBoxBorder = new GameObject();
                    flBoxBorder.transform.parent = transform;
                    flBoxBorder.tag = "ZoneRepresentation";
                    flBoxBorder.name = airspace.name + " Upper FL Box Border";
                    RectTransform flBoxBorderRectTransform = flBoxBorder.AddComponent<RectTransform>();
                    flBoxBorderRectTransform.localScale = new Vector3(1, 1, 1);
                    flBoxBorderRectTransform.anchorMin = new Vector2(1, .5F);
                    flBoxBorderRectTransform.anchorMax = new Vector2(1, .5F);
                    flBoxBorderRectTransform.sizeDelta = new Vector2(0.038F, 0.018F);
                    flBoxBorderRectTransform.anchoredPosition = new Vector2(-0.1F, rectTransform.anchoredPosition.y + rectTransform.sizeDelta.y / 2);
                    flBoxBorder.AddComponent<CanvasRenderer>();
                    RawImage flBoxBorderImage = flBoxBorder.AddComponent<RawImage>();
                    flBoxBorderImage.color = zoneColor;

                    GameObject flBoxBackground = new GameObject();
                    flBoxBackground.transform.parent = transform;
                    flBoxBackground.tag = "ZoneRepresentation";
                    flBoxBackground.name = airspace.name + " Upper FL Box Background";
                    RectTransform flBoxBackgroundRectTransform = flBoxBackground.AddComponent<RectTransform>();
                    flBoxBackgroundRectTransform.localScale = new Vector3(1, 1, 1);
                    flBoxBackgroundRectTransform.anchorMin = new Vector2(1, .5F);
                    flBoxBackgroundRectTransform.anchorMax = new Vector2(1, .5F);
                    flBoxBackgroundRectTransform.sizeDelta = new Vector2(0.035F, 0.015F);
                    flBoxBackgroundRectTransform.anchoredPosition = new Vector2(-0.1F, rectTransform.anchoredPosition.y + rectTransform.sizeDelta.y / 2);
                    flBoxBackground.AddComponent<CanvasRenderer>();
                    RawImage flBoxBackgroundImage = flBoxBackground.AddComponent<RawImage>();
                    flBoxBackgroundImage.color = Color.white;

                    GameObject flBoxText = new GameObject();
                    flBoxText.transform.parent = transform;
                    flBoxText.tag = "ZoneRepresentation";
                    flBoxText.name = airspace.name + " Upper FL Box Text";
                    RectTransform flBoxTextRectTransform = flBoxText.AddComponent<RectTransform>();
                    flBoxTextRectTransform.localScale = new Vector3(1, 1, 1);
                    flBoxTextRectTransform.anchorMin = new Vector2(1, .5F);
                    flBoxTextRectTransform.anchorMax = new Vector2(1, .5F);
                    flBoxTextRectTransform.sizeDelta = new Vector2(0.3F, 0.3F);
                    flBoxTextRectTransform.anchoredPosition = new Vector2(0.034F, rectTransform.anchoredPosition.y + rectTransform.sizeDelta.y / 2 - flBoxTextRectTransform.sizeDelta.y / 2 + 0.005F);
                    TMP_Text flxBoxTextComponent = flBoxText.AddComponent<TextMeshProUGUI>();
                    flxBoxTextComponent.fontSize = 0.009F;
                    flxBoxTextComponent.color = Color.black;
                    float upperFL = (int) FlightLevelHelper.MetersToFlightLevel(airspace.upperLimit.inMeters);
                    flxBoxTextComponent.text = "FL " + (upperFL >= 9000 ? "9k+" : upperFL);

                    if(!bottomOverflowing) {
                        GameObject flBoxBorderLower = Instantiate(flBoxBorder);
                        flBoxBorderLower.name = airspace.name + " Lower FL Box Border";
                        flBoxBorderLower.transform.SetParent(transform);
                        RectTransform flBoxBorderLowerRectTransform = flBoxBorderLower.GetComponent<RectTransform>();
                        flBoxBorderLowerRectTransform.localScale = new Vector3(1, 1, 1);
                        flBoxBorderLowerRectTransform.anchoredPosition = new Vector2(flBoxBorderRectTransform.anchoredPosition.x, flBoxBorderRectTransform.anchoredPosition.y - zoneHeightInFlightLevelDisplay);

                        GameObject flBoxBackgroundLower = Instantiate(flBoxBackground);
                        flBoxBackgroundLower.name = airspace.name + " Lower FL Box Background";
                        flBoxBackgroundLower.transform.SetParent(transform);
                        RectTransform flBoxBackgroundLowerRectTransform = flBoxBackgroundLower.GetComponent<RectTransform>();
                        flBoxBackgroundLowerRectTransform.localScale = new Vector3(1, 1, 1);
                        flBoxBackgroundLowerRectTransform.anchoredPosition = new Vector2(flBoxBackgroundRectTransform.anchoredPosition.x, flBoxBackgroundRectTransform.anchoredPosition.y - zoneHeightInFlightLevelDisplay);

                        GameObject flBoxTextLower = Instantiate(flBoxText);
                        flBoxTextLower.name = airspace.name + " Lower FL Box Text";
                        flBoxTextLower.transform.SetParent(transform);
                        RectTransform flBoxTextLowerRectTransform = flBoxTextLower.GetComponent<RectTransform>();
                        flBoxTextLowerRectTransform.localScale = new Vector3(1, 1, 1);
                        flBoxTextLowerRectTransform.anchoredPosition = new Vector2(flBoxTextRectTransform.anchoredPosition.x, flBoxTextRectTransform.anchoredPosition.y - zoneHeightInFlightLevelDisplay);
                        TMP_Text flxBoxTextLowerComponent = flBoxTextLower.GetComponent<TextMeshProUGUI>();
                        float lowerFL = (int) FlightLevelHelper.MetersToFlightLevel(airspace.lowerLimit.inMeters);
                        flxBoxTextLowerComponent.text = "FL " + (lowerFL >= 9000 ? "9k+" : lowerFL);
                    }

                    if(topOverflowing) {
                        Destroy(flBoxBorder);
                        Destroy(flBoxBackground);
                        Destroy(flBoxText);
                    }
                }
            }
        }
    }

    private static Vector3 Double3ToVector3(double3 d3) {
        return new Vector3((float)d3.x, (float)d3.y, (float)d3.z);
    }
}
