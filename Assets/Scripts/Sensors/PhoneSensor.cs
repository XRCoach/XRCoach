using UnityEngine;

public class PhoneSensor : MonoBehaviour, IReadableSensor
{
    private bool hasNewData = false;
    private ImuDataPoint currentDataPoint;

    void Start()
    {
        if (!Application.isEditor)
        {
            Input.gyro.enabled = true;
        }
    }

    void Update()
    {
        Vector3 accel, gyro;
        string source;

        if (Application.isEditor)
        {
            accel = new Vector3(0, -1, 0);
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            gyro = new Vector3(-mouseY, mouseX, 0) * 5f;
            source = "Simulated";
        }
        else
        {
            // ... code pour le téléphone ...
            if (!Input.gyro.enabled) return;
            accel = Input.acceleration;
            gyro = Input.gyro.rotationRate;
            source = "Phone";
        }

        currentDataPoint = new ImuDataPoint
        {
            // ... attribution des valeurs ...
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Acceleration = accel,
            Gyroscope = gyro,
            Source = source
        };
        hasNewData = true;

        // ***** AJOUTEZ CETTE LIGNE POUR LE TEST *****
        if (Application.isEditor)
        {
            Debug.Log($"[SIMULATED] Gyro: {gyro}");
        }
    }

    public bool TryGetLatestData(out ImuDataPoint dataPoint)
    {
        if (hasNewData)
        {
            dataPoint = currentDataPoint;
            hasNewData = false;
            return true;
        }
        dataPoint = default;
        return false;
    }
}