using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;


public class PointCloudHandler : MonoBehaviour
{
    ARSessionOrigin arSessionOrigin;
    ARPointCloud arPointCloud;
    ARPointCloudManager arPointCloudManager;
    ARPointCloudParticleVisualizer arVisualizer;

    public bool updateCloudFlag = false;

    private List<Vector3> points = new List<Vector3>();
    public List<Vector3> FeaturePoints
    {
        get { return new List<Vector3>(points); }
        private set { }
    }

    public bool placementFinished = false;
    private bool processFlag = true;

    // Start is called before the first frame update
    void Start()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        arPointCloudManager = FindObjectOfType<ARPointCloudManager>();
        arPointCloudManager.pointCloudsChanged += UpdatedCloudPoints;
        arVisualizer = GetComponent<ARPointCloudParticleVisualizer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (placementFinished && arPointCloud == null)
            arPointCloud = arSessionOrigin.trackablesParent.GetComponentInChildren<ARPointCloud>();
        if (arVisualizer == null)
            arVisualizer = arSessionOrigin.trackablesParent.GetComponentInChildren<ARPointCloudParticleVisualizer>();
    }

    public void UpdatedCloudPoints(ARPointCloudChangedEventArgs change)
    {
        for (int i = 0; i < arPointCloud.identifiers.Value.Length; i++)
            if (arPointCloud.confidenceValues.Value[i] > 0.4f)
            {
                points.Add(arPointCloud.positions.Value[i]);
            }
        if (arPointCloud.identifiers.Value.Length >= 500)
            updateCloudFlag = true;
    }

    public void CleanUp()
    {
        points.Clear();
    }

    public void SetPointCloudVisualization(bool visualization)
    {
        arVisualizer.gameObject.SetActive(visualization);
    }

    public void PointCloudProcessChanged(bool isActive)
    {
        processFlag = isActive;
        SetPointCloudVisualization(isActive);
    }
}
