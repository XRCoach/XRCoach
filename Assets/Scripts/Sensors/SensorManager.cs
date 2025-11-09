using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class SensorManager : MonoBehaviour
{
    public static SensorManager Instance { get; private set; }
    public List<ImuDataPoint> dataBuffer = new List<ImuDataPoint>();

    private List<IReadableSensor> allSensors = new List<IReadableSensor>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Find all components in the scene that are IReadableSensor
        allSensors = FindObjectsOfType<MonoBehaviour>().OfType<IReadableSensor>().ToList();
        Debug.Log($"[SensorManager] Found {allSensors.Count} readable sensor(s).");
    }

    private void Update()
    {
        foreach (var sensor in allSensors)
        {
            if (sensor.TryGetLatestData(out ImuDataPoint dataPoint))
            {
                dataBuffer.Add(dataPoint);
            }
        }
    }

    private void OnApplicationQuit()
    {
        SaveBufferToCsv();
    }

    public void SaveBufferToCsv()
    {
        if (dataBuffer.Count == 0) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Timestamp,AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ,Source");

        foreach (var point in dataBuffer)
        {
            sb.AppendLine($"{point.Timestamp},{point.Acceleration.x},{point.Acceleration.y},{point.Acceleration.z},{point.Gyroscope.x},{point.Gyroscope.y},{point.Gyroscope.z},{point.Source}");
        }

        string path = Path.Combine(Application.persistentDataPath, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Successfully saved {dataBuffer.Count} data points to: {path}");
        dataBuffer.Clear();
    }
}