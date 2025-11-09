<<<<<<< HEAD
// THIS IS THE CORRECT VERSION. NO CHANGES ARE NEEDED FROM WHAT YOU ALREADY HAVE.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SensorFusion : MonoBehaviour
{
    [Header("=== SENSOR FUSION CONFIGURATION ===")]
    [SerializeField] private float sampleRate = 100f;
    [SerializeField] private FusionAlgorithm algorithm = FusionAlgorithm.Madgwick;
    [SerializeField] private float beta = 0.1f; // Gain Madgwick
    [SerializeField] private float kp = 1.0f;   // Gain Mahony
    [SerializeField] private float ki = 0.0f;   // Gain Mahony

    [Header("=== SENSOR INPUT ===")]
    [SerializeField] private IMUDataReceiver dataReceiver;
    [SerializeField] private bool useMagnetometer = true;

    [Header("=== REFERENCE FRAMES ===")]
    [SerializeField] private Transform bodyFrame;
    [SerializeField] private Transform deviceFrame;

    [Header("=== DEBUG & VISUALIZATION ===")]
    [SerializeField] private bool enableVisualization = true;
    [SerializeField] private bool logFeatures = false;

    // Public access to fusion results
    public Quaternion Orientation { get; private set; }
    public Vector3 LinearAcceleration { get; private set; }
    public Vector3 BodyGyro { get; private set; }
    public Features CurrentFeatures { get; private set; }

    // Internal states
    private MadgwickAHRS madgwick;
    private MahonyAHRS mahony;
    private Queue<IMUData> dataBuffer;
    private FeaturesExtractor featuresExtractor;

    // Kalman filter for gravity (optional)
    private Vector3 estimatedGravity;
    private const float GRAVITY_LP_FILTER = 0.98f;

    public enum FusionAlgorithm
    {
        Madgwick,
        Mahony,
        Complementary
    }

    [System.Serializable]
    public struct IMUData
    {
        public Vector3 accelerometer;
        public Vector3 gyroscope;
        public Vector3 magnetometer;
        public double timestamp;

        public IMUData(Vector3 accel, Vector3 gyro, Vector3 mag, double time)
        {
            accelerometer = accel;
            gyroscope = gyro;
            magnetometer = mag;
            timestamp = time;
        }
    }

    [System.Serializable]
    public struct Features
    {
        public float jointAngle;
        public float angularVelocity;
        public float signalEnergy;
        public float motionIntensity;
        public Vector3 eulerAngles;

        public string ToCSV()
        {
            return $"{jointAngle:F4},{angularVelocity:F4},{signalEnergy:F4},{motionIntensity:F4},{eulerAngles.x:F4},{eulerAngles.y:F4},{eulerAngles.z:F4}";
        }

        public static string GetCSVHeader()
        {
            return "jointAngle,angularVelocity,signalEnergy,motionIntensity,eulerX,eulerY,eulerZ";
        }
    }

    // AFTER
    void Start()
    {
        InitializeSensorFusion();

        // We comment this out for now to fix the compilation error.
        // The SensorManager will need to assign itself to this script later.
        // if (dataReceiver == null)
        //    dataReceiver = FindObjectOfType<IMUDataReceiver>();

        Debug.Log($"Sensor Fusion initialized with {algorithm} algorithm");
    }

    void Update()
    {
        if (dataReceiver == null) return;

        // Get latest IMU data
        IMUData currentData = dataReceiver.GetLatestData();

        // Apply sensor fusion
        UpdateOrientation(currentData);

        // Process derived quantities
        ProcessLinearAcceleration(currentData.accelerometer);
        TransformToBodyFrame(currentData.gyroscope);

        // Extract features
        CurrentFeatures = ExtractFeatures(currentData);

        // Visualization
        if (enableVisualization)
            UpdateVisualization();

        // Debug logging
        if (logFeatures)
            Debug.Log($"Features: {CurrentFeatures.ToCSV()}");
    }

    private void InitializeSensorFusion()
    {
        Orientation = Quaternion.identity;
        estimatedGravity = Vector3.down * 9.81f;
        dataBuffer = new Queue<IMUData>();
        featuresExtractor = new FeaturesExtractor(sampleRate);

        switch (algorithm)
        {
            case FusionAlgorithm.Madgwick:
                madgwick = new MadgwickAHRS(beta, sampleRate);
                break;
            case FusionAlgorithm.Mahony:
                mahony = new MahonyAHRS(kp, ki, sampleRate);
                break;
        }
    }

    private void UpdateOrientation(IMUData data)
    {
        // Convert to right-handed coordinate system if needed
        Vector3 accel = TransformToRightHanded(data.accelerometer);
        Vector3 gyro = TransformToRightHanded(data.gyroscope);
        Vector3 mag = TransformToRightHanded(data.magnetometer);

        switch (algorithm)
        {
            case FusionAlgorithm.Madgwick:
                if (useMagnetometer)
                    madgwick.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z, mag.x, mag.y, mag.z);
                else
                    madgwick.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z);

                Orientation = new Quaternion(madgwick.Quaternion[1], madgwick.Quaternion[2],
                                           madgwick.Quaternion[3], madgwick.Quaternion[0]);
                break;

            case FusionAlgorithm.Mahony:
                if (useMagnetometer)
                    mahony.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z, mag.x, mag.y, mag.z);
                else
                    mahony.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z);

                Orientation = new Quaternion(mahony.Quaternion[1], mahony.Quaternion[2],
                                           mahony.Quaternion[3], mahony.Quaternion[0]);
                break;

            case FusionAlgorithm.Complementary:
                Orientation = UpdateComplementaryFilter(gyro, accel, Time.deltaTime);
                break;
        }

        // Add to buffer for feature extraction
        dataBuffer.Enqueue(data);
        if (dataBuffer.Count > Mathf.RoundToInt(sampleRate * 2)) // Keep 2 seconds of data
            dataBuffer.Dequeue();
    }

    private Quaternion UpdateComplementaryFilter(Vector3 gyro, Vector3 accel, float dt)
    {
        // Gyro integration
        Quaternion gyroQuat = Orientation * Quaternion.Euler(gyro * dt * Mathf.Rad2Deg);

        // Accel orientation (pitch/roll)
        Vector3 accelNormalized = accel.normalized;
        Quaternion accelQuat = Quaternion.FromToRotation(Vector3.down, accelNormalized);

        // Complementary filter
        float alpha = 0.98f;
        return Quaternion.Slerp(gyroQuat, accelQuat, 1.0f - alpha);
    }

    private void ProcessLinearAcceleration(Vector3 rawAccel)
    {
        // Remove gravity from accelerometer data
        Vector3 gravityInDeviceFrame = Orientation * Vector3.down * 9.81f;
        LinearAcceleration = rawAccel - gravityInDeviceFrame;

        // Low-pass filter for gravity estimation
        estimatedGravity = Vector3.Lerp(estimatedGravity, rawAccel.normalized * 9.81f, 1.0f - GRAVITY_LP_FILTER);
    }

    private void TransformToBodyFrame(Vector3 deviceGyro)
    {
        // Transform gyro from device frame to body frame
        BodyGyro = Quaternion.Inverse(Orientation) * deviceGyro;
    }

    private Vector3 TransformToRightHanded(Vector3 input)
    {
        // Convert from sensor-specific coordinate system to right-handed Unity system
        return new Vector3(input.x, input.z, input.y);
    }

    private Features ExtractFeatures(IMUData currentData)
    {
        Features features = new Features();

        // Euler angles for intuitive understanding
        features.eulerAngles = Orientation.eulerAngles;

        // Angular velocity magnitude
        features.angularVelocity = currentData.gyroscope.magnitude;

        // Signal energy (variance of recent accelerometer data)
        features.signalEnergy = CalculateSignalEnergy();

        // Motion intensity (combined accelerometer and gyro)
        features.motionIntensity = (LinearAcceleration.magnitude + features.angularVelocity) / 2.0f;

        return features;
    }

    private float CalculateSignalEnergy()
    {
        if (dataBuffer.Count < 10) return 0f;

        Vector3 mean = Vector3.zero;
        foreach (IMUData data in dataBuffer)
        {
            mean += data.accelerometer;
        }
        mean /= dataBuffer.Count;

        float variance = 0f;
        foreach (IMUData data in dataBuffer)
        {
            variance += (data.accelerometer - mean).sqrMagnitude;
        }
        variance /= dataBuffer.Count;

        return variance;
    }

    private void UpdateVisualization()
    {
        if (deviceFrame != null)
        {
            deviceFrame.rotation = Orientation;
        }
    }

    // Public methods for external access
    public Quaternion GetOrientation() => Orientation;
    public Vector3 GetLinearAcceleration() => LinearAcceleration;
    public Vector3 GetBodyFrameGyro() => BodyGyro;
    public Features GetCurrentFeatures() => CurrentFeatures;

    // Method to calculate joint angle between two IMUs
    public static float CalculateJointAngle(Quaternion q1, Quaternion q2)
    {
        Quaternion relativeRotation = Quaternion.Inverse(q1) * q2;
        return 2.0f * Mathf.Acos(Mathf.Clamp(relativeRotation.w, -1f, 1f)) * Mathf.Rad2Deg;
    }

    // Calibration method
    public void CalibrateMagnetometer()
    {
        Debug.Log("Magnetometer calibration started...");
        // Implementation for hard/soft iron calibration
    }

    void OnDestroy()
    {
        // Cleanup
        dataBuffer?.Clear();
=======
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
>>>>>>> origin/MOHAMED
    }
}