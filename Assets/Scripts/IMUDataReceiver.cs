/// <summary>
/// An interface that defines a contract for any class that can provide IMU data.
/// The SensorFusion script depends on this to receive sensor readings.
/// </summary>
public interface IMUDataReceiver
{
    /// <summary>
    /// This method must be implemented by any data provider class.
    /// It should return the most recent data point from the sensor.
    /// </summary>
    /// <returns>A struct containing the latest accelerometer, gyroscope, and magnetometer data.</returns>
    SensorFusion.IMUData GetLatestData();
}