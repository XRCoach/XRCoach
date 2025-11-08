using UnityEngine;

public class SensorManager : MonoBehaviour
{
    // This static property will allow any other script to easily access this manager.
    public static SensorManager Instance { get; private set; }

    // Public properties to allow other scripts to READ the latest sensor data.
    public Vector3 Acceleration { get; private set; }
    public Vector3 Gyroscope { get; private set; }

    // Awake is called when the script instance is being loaded.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update.
    void Start()
    {
        InitializeSensors();
    }


    private void InitializeSensors()
    {
        Debug.Log("SensorManager Initialized. Enabling Gyroscope...");
        Input.gyro.enabled = true; // This turns on the phone's gyroscope.
    }

    // Update is called once per frame.
    // THIS IS THE NEW, IMPROVED CODE
    private void Update()
    {
        // --- Platform Dependent Input ---

        // If we are running inside the Unity Editor on a PC...
        if (Application.isEditor)
        {
            // ...SIMULATE the sensor data.
            // Simulate a phone lying flat: gravity is pulling down on the Y axis.
            Acceleration = new Vector3(0, -1, 0);

            // Simulate a slow rotation using the mouse movement.
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            Gyroscope = new Vector3(-mouseY, mouseX, 0) * 5f; // Multiplier for sensitivity

            // Print the simulated values to the console for testing.
            Debug.Log($"[SIMULATED] Accel: {Acceleration}, Gyro: {Gyroscope}");
        }
        // Otherwise, if we are on a real device (like a phone)...
        else
        {
            // ...use the REAL sensor data.
            if (Input.gyro.enabled)
            {
                Acceleration = Input.acceleration;
                Gyroscope = Input.gyro.rotationRate;

                // Print the real values to the console for testing.
                Debug.Log($"[REAL] Accel: {Acceleration}, Gyro: {Gyroscope}");
            }
        }
    }
}