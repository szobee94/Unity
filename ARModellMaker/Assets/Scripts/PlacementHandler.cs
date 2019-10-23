using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PlacementHandler : MonoBehaviour
{
    public GameObject targetIndicator;
    public GameObject objectToPlace;

    private Vector3[] planeVector = new Vector3[3];
    private Plane plane = new Plane();

    public Vector3 Origin
    {
        get { return planeVector[0]; }
        private set { }
    }

    public Vector3 WidthScale
    {
        get { return planeVector[1] - planeVector[0]; }
        private set { }
    }

    public Vector3 HeightScale
    {
        get { return planeVector[2] - planeVector[0]; }
        private set { }
    }

    private Vector3 cameraPosition;
    private Vector3 cameraForward;

    private ARRaycastManager rayManager;
    private Pose placementPose;
    private bool placementPoseIsValid = false;
    private int placementCounter = 0;

    private bool placeObjectSet = false;
    private bool placementFinished = false;

    // Start is called before the first frame update
    void Start()
    {
        rayManager = FindObjectOfType<ARRaycastManager>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateCamera();
        if (!placementFinished)
        {
            UpdatePlacementPose();
            UpdateIndicator();
        }
    }

    private void HandleAsyncCalls()
    {
        if (placeObjectSet)
        {
            placeObjectSet = false;
            PlaceObject();
        }
    }

    public void PlaceObject_async()
    {
        placeObjectSet = true;
    }

    private void PlaceObject()
    {
        Vector3 placed = new Vector3(placementPose.position.x, placementPose.position.y, placementPose.position.z);
        if (placementCounter > 0)
            placed.y = Origin.y;
        if (placementCounter == 2)
        {
            Vector3 placedVector = placed - Origin;
            Vector3 parallel = Vector3.Dot(placedVector, WidthScale.normalized) * WidthScale.normalized;
            Vector3 perpendicular = placedVector - parallel;
            planeVector[placementCounter] = Origin + perpendicular;
        }
        else
            planeVector[placementCounter] = placed;
        Instantiate(objectToPlace, planeVector[placementCounter], Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 0.0f)));
        if (placementCounter >= 2)
        {
            Instantiate(objectToPlace, Origin + WidthScale + HeightScale, Quaternion.LookRotation(new Vector3(0.0f, 0.0f, 0.0f)));
            placementFinished = true;
            targetIndicator.SetActive(false);
        }
        else
        {
            placementCounter++;
        }
    }

    public Vector2 GetPlaneCoordinates()
    {
        Vector3 cameraVector = cameraPosition - Origin;
        float w = Vector3.Dot(cameraVector, WidthScale) / WidthScale.magnitude;
        float h = Vector3.Dot(cameraVector, HeightScale) / HeightScale.magnitude;
        return new Vector2(w, h);
    }


    public Pose GetCameraPose()
    {
        Pose result = new Pose();
        result.position = cameraPosition;
        result.rotation = Quaternion.LookRotation(cameraForward);
        return result;
    }

    public float GetCurrentAngle()
    {
        Vector2 forwardVector = new Vector2(cameraForward.x, cameraForward.z);
        Vector2 widthVector = new Vector2(WidthScale.x, WidthScale.z);
        Vector2 heightVector = new Vector2(HeightScale.x, HeightScale.z);
        float angleWidth = Vector2.Angle(widthVector, forwardVector);
        float angleHeight = Vector2.Angle(heightVector, forwardVector);
        if (angleHeight < 90.0f)
            return angleWidth;
        return 360.0f - angleWidth;
    }

    public Vector2 ConvertToPlaneCoordinates(Vector3 input)
    {
        Vector3 fromOrigin = input - Origin;
        float w = Vector3.Dot(fromOrigin, WidthScale) / WidthScale.magnitude;
        float h = Vector3.Dot(fromOrigin, HeightScale) / HeightScale.magnitude;
        return new Vector2(w, h);
    }

    public Vector2[] ConvertToPlaneCoordinates(Vector3[] input)
    {
        Vector2[] result = new Vector2[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            Vector3 fromOrigin = input[i] - Origin;
            float w = Vector3.Dot(fromOrigin, WidthScale) / WidthScale.magnitude;
            float h = Vector3.Dot(fromOrigin, HeightScale) / HeightScale.magnitude;
            result[i] = new Vector2(w, h);
        }
        return result;
    }

    public List<Vector3> Get3DCoordinates(List<Vector3> featurePoints, bool filter = false)
    {
        List<Vector3> localCoordinates = new List<Vector3>();
        for (int i = 0; i < featurePoints.Count; i++)
        {
            float distance = Vector3.Distance(cameraPosition, featurePoints[i]);
            float z = plane.GetDistanceToPoint(featurePoints[i]);
            if (!filter || z > 0.2f)
            {
                Vector2 planePosition = ConvertToPlaneCoordinates(featurePoints[i]);
                Vector3 actualVector = new Vector3(planePosition.x, planePosition.y, z);
                localCoordinates.Add(actualVector);
            }
        }
        return localCoordinates;
    }

    public static List<Vector3> Aggregate3D(List<Vector3> input, float resolution)
    {
        List<Vector3> result = new List<Vector3>();

        foreach (Vector3 element in input)
        {
            Vector3 aggregated = new Vector3(RoundToTarget(element.x, resolution),
                                             RoundToTarget(element.y, resolution),
                                             RoundToTarget(element.z, resolution));
            if (!result.Contains(aggregated))
                result.Add(aggregated);
        }

        return result;
    }

    private static float RoundToTarget(float value, float target)
    {
        return ((float)Math.Round(value / target)) * target;
    }

    public Vector3 ConvertToWorldCoordinates(Vector2 input)
    {
        return Origin + (WidthScale.normalized * input.x) + (HeightScale.normalized * input.y);
    }

    public Vector3[] ConvertToWorldCoordinates(Vector2[] input)
    {
        Vector3[] result = new Vector3[input.Length];
        for (int i = 0; i < input.Length; i++)
            result[i] = Origin + (WidthScale.normalized * input[i].x) + (HeightScale.normalized * input[i].y);
        return result;
    }

    private void UpdateCamera()
    {
        cameraPosition = Camera.current.transform.position;
        cameraForward = Camera.current.transform.forward;
    }

    private void UpdatePlacementPose()
    {
        var screenRayOrigin = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();

        rayManager.Raycast(screenRayOrigin, hits, UnityEngine.XR.ARSubsystems.TrackableType.All);
        placementPoseIsValid = hits.Count > 0;

        if (!placementPoseIsValid)
        {
            float[] xShifts = new float[] { -0.02f, 0.02f, -0.05f, 0.05f };
            float[] yShifts = new float[] { -0.02f, 0.02f, -0.05f, 0.05f };
            for (int x_i = 0; ((x_i < xShifts.Length) && !placementPoseIsValid); x_i++)
                for (int y_i = 0; ((y_i < yShifts.Length) && !placementPoseIsValid); y_i++)
                {
                    screenRayOrigin = Camera.current.ViewportToScreenPoint(new Vector3(0.5f + xShifts[x_i], 0.5f + yShifts[y_i]));
                    rayManager.Raycast(screenRayOrigin, hits, UnityEngine.XR.ARSubsystems.TrackableType.All);
                    placementPoseIsValid = hits.Count > 0;
                }
        }

        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;
            var cameraForward = Camera.current.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, cameraForward.y, cameraForward.z);
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }

    private void UpdateIndicator()
    {
        if (placementPoseIsValid && placementCounter != 3)
        {
            targetIndicator.SetActive(true);
            targetIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            targetIndicator.SetActive(false);
        }
    }
}