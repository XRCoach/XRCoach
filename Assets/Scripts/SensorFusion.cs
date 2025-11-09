using UnityEngine;
using System;

public class SensorFusion : MonoBehaviour
{
    [Header("Feature Settings")]
    [SerializeField] private bool applySmoothing = true;
    [SerializeField] private float smoothFactor = 0.9f;

    public float[] features { get; private set; } = new float[10];
    private float[] smoothed = new float[6];

    public event Action<float[]> OnFeaturesUpdated;

    private void Start()
    {
        var sensor = FindObjectOfType<SensorManager>();
        if (sensor != null)
            sensor.OnIMUUpdated += ProcessIMU;
        else
            Debug.LogError("[SensorFusion] SensorManager not found!");
    }

    private void ProcessIMU(float[] imu)
    {
        if (applySmoothing)
        {
            for (int i = 0; i < 6; i++)
                smoothed[i] = imu[i] * (1 - smoothFactor) + smoothed[i] * smoothFactor;
        }
        else
        {
            Array.Copy(imu, smoothed, 6);
        }

        float ax = smoothed[0], ay = smoothed[1], az = smoothed[2];
        float gx = smoothed[3], gy = smoothed[4], gz = smoothed[5];

        float magA = Mathf.Sqrt(ax * ax + ay * ay + az * az);
        float magG = Mathf.Sqrt(gx * gx + gy * gy + gz * gz);

        features[0] = ax; features[1] = ay; features[2] = az;
        features[3] = gx; features[4] = gy; features[5] = gz;
        features[6] = magA;
        features[7] = magG;
        features[8] = Mathf.Atan2(ay, az);
        features[9] = Mathf.Atan2(ax, az);

        OnFeaturesUpdated?.Invoke(features);
    }
}