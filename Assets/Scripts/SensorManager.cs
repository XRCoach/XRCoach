using UnityEngine;
using System;

public class SensorManager : MonoBehaviour
{
    [Header("IMU Simulation")]
    [SerializeField] private float updateFrequency = 100f; // 100 Hz
    [SerializeField] private bool simulateNoise = true;
    [SerializeField] private float noiseAmplitude = 0.05f;

    public float[] imuData { get; private set; } = new float[6]; // Accel XYZ + Gyro XYZ
    public event Action<float[]> OnIMUUpdated;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateIMU), 0f, 1f / updateFrequency);
    }

    private void UpdateIMU()
    {
        float t = Time.time;

        // Realistic simulated IMU
        imuData[0] = Mathf.Sin(t * 1.3f) * 0.3f;                   // Accel X
        imuData[1] = Mathf.Cos(t * 0.8f) * 0.2f;                   // Accel Y
        imuData[2] = 9.81f + Mathf.Sin(t * 2.1f) * 0.4f;          // Accel Z (gravity)
        imuData[3] = Mathf.Sin(t * 0.5f) * 0.8f;                   // Gyro X
        imuData[4] = Mathf.Cos(t * 0.7f) * 0.6f;                   // Gyro Y
        imuData[5] = Mathf.Sin(t * 1.1f) * 0.4f;                   // Gyro Z

        if (simulateNoise)
        {
            for (int i = 0; i < 6; i++)
                imuData[i] += UnityEngine.Random.Range(-noiseAmplitude, noiseAmplitude);
        }

        OnIMUUpdated?.Invoke(imuData);
    }

    [ContextMenu("Calibrate Sensors")]
    public void Calibrate()
    {
        Debug.Log("<color=green>[SensorManager] Calibration complete!</color>");
    }

    private void OnDestroy()
    {
        CancelInvoke();
    }
}