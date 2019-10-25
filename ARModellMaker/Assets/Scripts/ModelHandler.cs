using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System;

[RequireComponent(typeof(PlacementHandler), typeof(PointCloudHandler))]
[RequireComponent(typeof(Communication))]
public class ModelHandler : MonoBehaviour
{
    PlacementHandler placementHandler;
    PointCloudHandler pointCloudHandler;
    Communication communication;

    /*UI elements*/
    public Button saveButton;
    public Button placeButton;
    public Button settingsButton;
    public Text featurePointCount;
    public Text trackingStatus;
    private ARSession my_session;
    private List<Vector3> points = new List<Vector3>();

    /*Settings*/
    public GameObject settingsPanel;
    public Toggle visualizationToggle;

    /*Save */
    public GameObject savePanel;
    public InputField saveNameInputField;
    private string saveName;
    public Button confirmSaveButton;

    private bool placementFinished = false;

    // Start is called before the first frame update
    void Start()
    {
        placementHandler = GetComponent<PlacementHandler>();
        pointCloudHandler = GetComponent<PointCloudHandler>();
        communication = GetComponent<Communication>();

        savePanel.SetActive(false);
        settingsPanel.SetActive(false);

        saveButton.onClick.AddListener(SetActiveSavePanel);
        placeButton.onClick.AddListener(placementHandler.PlaceObject_async);
        settingsButton.onClick.AddListener(SettingsClicked);
        visualizationToggle.onValueChanged.AddListener(delegate { SetPointCloudVisualization(pointCloudVisualizationToggle); });
        saveNameInputField.onEndEdit.AddListener(SetSaveName);
        confirmSaveButton.onClick.AddListener(SavePointCloud);

        confirmSaveButton.interactable = false;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateTracking();
        if (pointCloudHandler.updateCloudFlag && placementFinished)
        {
            UpdatePointCloud();
            pointCloudHandler.updateCloudFlag = false;
        }
        if (points.Count >= 5000)
            SendFeaturePoint();
        featurePointCount.text = points.Count.ToString(); //5000-ként törlődik, kezelni!
    }

    private void UpdateTracking()
    {
        trackingStatus.text = my_session.subsystem.trackingState.ToString();
        if (my_session.subsystem.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            trackingStatus.text = "";
        else
        {
            trackingStatus.text = "Tracking Lost!";
            trackingStatus.color = Color.red;
        }
    }

    private void UpdatePointCloud()
    {
        for (int i = 0; i < pointCloudHandler.FeaturePoints.Count; i++)
            points.Add(pointCloudHandler.FeaturePoints[i]);
        pointCloudHandler.CleanUp();
    }

    private void SettingsClicked()
    {
        settingsPanel.SetActive(true);
        //beállítási lehetőségek, panel
    }

    private void SavePointCloud()
    {
        //panel, név, perzisztencia
    }

    public void PlacementFinished()
    {
        placementFinished = true;
    }

    private void SendFeaturePoint()
    {
        List<byte> byteList = new List<byte>();
        byte[] timeStamp = BitConverter.GetBytes(DateTimeOffset.Now.ToUnixTimeMilliseconds());
        for (int i = 0; i < timeStamp.Length; i++)
            byteList.Add(timeStamp[i]);
        byte msgIdentifier = 1;
        byteList.Add(msgIdentifier);
        foreach (Vector3 element in points)
        {
            byteList.AddRange(BitConverter.GetBytes(element.x));
            byteList.AddRange(BitConverter.GetBytes(element.y));
            byteList.AddRange(BitConverter.GetBytes(element.z));
        }
        byte[] msg_bin = byteList.ToArray();
        communication.Publish(msg_bin);
        points.Clear();
    }

    private void SetPointCloudVisualization(Toggle toggle)
    {
        pointCloudHandler.SetPointCloudVisualization(toggle.isOn);    
    }

    private void SetSaveName(string arg){
        saveName = arg;
        if(arg.Length > 0)
            confirmSaveButton.interactable = true;
    }

    private void SetActiveSavePanel(){
        savePanel.SetActive(true);
    }
}
