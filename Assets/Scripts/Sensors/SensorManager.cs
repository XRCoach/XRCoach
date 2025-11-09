// In Assets/Scripts/Sensors/SensorManager.cs

using UnityEngine;
using System; // Pour DateTimeOffset
using System.Collections.Generic; // Pour les Listes (List<T>)
using System.IO; // Pour la gestion des fichiers (File, Path)
using System.Text; // Pour le StringBuilder

public class SensorManager : MonoBehaviour
{
    // --- Singleton Instance ---
    // Permet d'accéder à ce manager depuis n'importe quel autre script via SensorManager.Instance
    public static SensorManager Instance { get; private set; }

    // --- Data Buffer ---
    // Stocke toutes les mesures de la session actuelle avant de les sauvegarder.
    public List<ImuDataPoint> dataBuffer = new List<ImuDataPoint>();

    // --- Unity Lifecycle Methods ---

    private void Awake()
    {
        // Logique du Singleton pour s'assurer qu'il n'y a qu'une seule instance de ce manager.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Garde ce manager actif même si on change de scène.
    }

    private void Start()
    {
        InitializeSensors();
    }

    private void Update()
    {
        // --- Étape 1: Collecter les données (réelles ou simulées) ---
        Vector3 currentAccel;
        Vector3 currentGyro;
        string source;

        if (Application.isEditor)
        {
            // Mode SIMULATION (quand on est dans l'éditeur Unity)
            currentAccel = new Vector3(0, -1, 0);
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            currentGyro = new Vector3(-mouseY, mouseX, 0) * 5f;
            source = "Simulated";
        }
        else
        {
            // Mode RÉEL (quand l'application tourne sur un téléphone)
            if (Input.gyro.enabled)
            {
                currentAccel = Input.acceleration;
                currentGyro = Input.gyro.rotationRate;
                source = "Phone";
            }
            else
            {
                return; // Si le gyroscope n'est pas activé, on ne fait rien.
            }
        }

        // --- Étape 2: Horodater et stocker les données ---

        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        ImuDataPoint dataPoint = new ImuDataPoint
        {
            Timestamp = timestamp,
            Acceleration = currentAccel,
            Gyroscope = currentGyro,
            Source = source
        };

        // Ajoute le nouveau point de données au buffer.
        dataBuffer.Add(dataPoint);
    }

    private void OnApplicationQuit()
    {
        // Cette fonction est appelée automatiquement par Unity juste avant que l'application ne se ferme.
        Debug.Log("Application is quitting. Saving collected data...");
        SaveBufferToCsv();
    }


    // --- Data Handling Methods ---

    private void InitializeSensors()
    {
        Debug.Log("SensorManager Initialized. Enabling Gyroscope...");
        Input.gyro.enabled = true;
    }

    /// <summary>
    /// Sauvegarde toutes les données actuellement dans le buffer dans un fichier CSV.
    /// </summary>
    public void SaveBufferToCsv()
    {
        if (dataBuffer.Count == 0)
        {
            Debug.Log("Data buffer is empty. Nothing to save.");
            return;
        }

        StringBuilder sb = new StringBuilder();

        // Ajoute la ligne d'en-tête pour le CSV.
        sb.AppendLine("Timestamp,AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ,Source");

        // Ajoute chaque point de données comme une nouvelle ligne dans le CSV.
        foreach (var point in dataBuffer)
        {
            sb.AppendLine($"{point.Timestamp},{point.Acceleration.x},{point.Acceleration.y},{point.Acceleration.z},{point.Gyroscope.x},{point.Gyroscope.y},{point.Gyroscope.z},{point.Source}");
        }

        // Crée un chemin de fichier unique pour éviter d'écraser les anciennes sessions.
        // Application.persistentDataPath est un dossier sûr où l'application a le droit d'écrire.
        string path = Path.Combine(Application.persistentDataPath, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        File.WriteAllText(path, sb.ToString());

        // Affiche le chemin du fichier pour que l'utilisateur puisse le trouver facilement.
        Debug.Log($"Successfully saved {dataBuffer.Count} data points to: {path}");

        dataBuffer.Clear();
    }
}