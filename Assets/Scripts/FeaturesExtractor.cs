using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FeaturesExtractor
{
    private int windowSize;
    private Queue<Vector3> accelBuffer;
    private Queue<Vector3> gyroBuffer;

    public FeaturesExtractor(float sampleRate)
    {
        windowSize = Mathf.RoundToInt(sampleRate * 0.5f); // 500ms window
        accelBuffer = new Queue<Vector3>();
        gyroBuffer = new Queue<Vector3>();
    }

    public void AddSample(Vector3 acceleration, Vector3 gyroscope)
    {
        accelBuffer.Enqueue(acceleration);
        gyroBuffer.Enqueue(gyroscope);

        if (accelBuffer.Count > windowSize)
        {
            accelBuffer.Dequeue();
            gyroBuffer.Dequeue();
        }
    }

    public float CalculateRMS(Vector3[] signals)
    {
        float sum = 0f;
        foreach (Vector3 signal in signals)
        {
            sum += signal.sqrMagnitude;
        }
        return Mathf.Sqrt(sum / signals.Length);
    }

    public float CalculateMeanCrossingRate(Vector3[] signals)
    {
        // Implementation of mean crossing rate feature
        int crossings = 0;
        Vector3 mean = CalculateMean(signals);

        for (int i = 1; i < signals.Length; i++)
        {
            // Check each axis for zero crossing
            for (int axis = 0; axis < 3; axis++)
            {
                if ((signals[i - 1][axis] - mean[axis]) * (signals[i][axis] - mean[axis]) < 0)
                    crossings++;
            }
        }

        return (float)crossings / (signals.Length * 3);
    }

    private Vector3 CalculateMean(Vector3[] signals)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 signal in signals)
        {
            sum += signal;
        }
        return sum / signals.Length;
    }
}