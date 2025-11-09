using UnityEngine;

public class BLEWrapper : MonoBehaviour, IReadableSensor
{
    [Header("BLE Device Configuration")]
    [Tooltip("The name of the BLE device to connect to.")]
    public string targetDeviceName = "XR_Coach_Sensor";

    [Tooltip("The Service UUID of the sensor data.")]
    public string serviceUUID = "YOUR_SERVICE_UUID"; // Example: "180D"

    [Tooltip("The Characteristic UUID for the IMU data.")]
    public string characteristicUUID = "YOUR_CHARACTERISTIC_UUID"; // Example: "2A37"

    private BleConnector ble_connector;
    private ImuDataPoint? latestDataPoint = null;

    void Start()
    {
        // Add the BleConnector component from the plugin to this GameObject
        ble_connector = gameObject.AddComponent<BleConnector>();

        // Configure it with our parameters
        ble_connector.DeviceName = targetDeviceName;
        ble_connector.ServiceUUID = serviceUUID;
        ble_connector.Characteristic = characteristicUUID;

        // Start scanning for the device
        Debug.Log($"[BLEWrapper] Starting scan for {targetDeviceName}");
        ble_connector.Scan();
    }

    void Update()
    {
        if (ble_connector != null && ble_connector.ble_device != null && ble_connector.ble_device.IsConnected())
        {
            byte[] data = ble_connector.ble_device.GetData();
            if (data != null)
            {
                ParseData(data);
            }
        }
    }

    private void ParseData(byte[] data)
    {
        // This is a PLACEHOLDER for your specific sensor's data format.
        // You MUST replace this with the correct parsing logic.
        // Example: Assuming 24 bytes for 2 Vector3s (accel + gyro)
        if (data.Length >= 24)
        {
            Vector3 accel = new Vector3(
                System.BitConverter.ToSingle(data, 0),
                System.BitConverter.ToSingle(data, 4),
                System.BitConverter.ToSingle(data, 8)
            );
            Vector3 gyro = new Vector3(
                System.BitConverter.ToSingle(data, 12),
                System.BitConverter.ToSingle(data, 16),
                System.BitConverter.ToSingle(data, 20)
            );

            latestDataPoint = new ImuDataPoint
            {
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Acceleration = accel,
                Gyroscope = gyro,
                Source = $"BLE_{targetDeviceName}"
            };
        }
    }

    public bool TryGetLatestData(out ImuDataPoint dataPoint)
    {
        if (latestDataPoint.HasValue)
        {
            dataPoint = latestDataPoint.Value;
            latestDataPoint = null; // Mark data as "read"
            return true;
        }
        dataPoint = default;
        return false;
    }
}