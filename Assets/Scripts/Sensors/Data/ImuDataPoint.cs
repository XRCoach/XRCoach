// In Assets/Scripts/Sensors/Data/ImuDataPoint.cs

using UnityEngine;

// Une 'struct' est utilisée ici car c'est un simple conteneur de données.
public struct ImuDataPoint
{
    public long Timestamp;      // Heure en millisecondes UTC (Unix time)
    public Vector3 Acceleration;
    public Vector3 Gyroscope;
    public string Source;       // D'où viennent les données ? "Phone", "BLE_Sensor_1", etc.
}