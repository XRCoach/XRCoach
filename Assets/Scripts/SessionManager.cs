using UnityEngine;
using TMPro;
using System.Collections;

public class SessionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI exerciseText;
    [SerializeField] private TextMeshProUGUI confidenceText;
    [SerializeField] private TextMeshProUGUI repText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem confetti;
    [SerializeField] private Animator avatarAnimator;

    [Header("Settings")]
    [SerializeField] private float minConfidence = 0.75f;
    [SerializeField] private float repCooldown = 1.8f;

    private bool isActive = false;
    private int repCount = 0;
    private float lastRepTime = 0f;
    private int currentExercise = -1;

    private readonly string[] exercises = { "Squat", "Push-Up", "Lunge", "Jumping Jack", "Rest" };

    private void Start()
    {
        UpdateStatus("Ready - Tap START");
        FindObjectOfType<SensorManager>()?.Calibrate();
    }

    public void StartSession()
    {
        isActive = true;
        repCount = 0;
        currentExercise = -1;
        lastRepTime = 0f;
        UpdateStatus("GO!");
        StartCoroutine(BlinkStart());
    }

    public void StopSession()
    {
        isActive = false;
        UpdateStatus($"FINISHED!\nReps: {repCount}");
        confetti?.Play();
    }

    public void OnPrediction(int classId, float[] probs)
    {
        if (!isActive || classId >= probs.Length) return;

        float conf = probs[classId];
        string name = classId < exercises.Length ? exercises[classId] : "Unknown";

        exerciseText.text = $"<color=orange>{name}</color>";
        confidenceText.text = $"{conf:P1}";
        repText.text = $"Reps: <color=lime>{repCount}</color>";

        // Avatar animation
        if (avatarAnimator != null)
            avatarAnimator.SetTrigger(name.Replace(" ", ""));

        // Rep counting logic
        if (conf > minConfidence && classId != 4) // Not "Rest"
        {
            if (currentExercise != classId || Time.time - lastRepTime > repCooldown)
            {
                if (currentExercise == classId)
                {
                    repCount++;
                    lastRepTime = Time.time;
                    StartCoroutine(PulseRep());
                    confetti?.Play();
                }
                currentExercise = classId;
            }
        }
        else if (conf < 0.4f)
        {
            currentExercise = -1;
        }
    }

    private void UpdateStatus(string msg)
    {
        statusText.text = msg;
    }

    private IEnumerator BlinkStart()
    {
        for (int i = 0; i < 6; i++)
        {
            statusText.enabled = !statusText.enabled;
            yield return new WaitForSeconds(0.3f);
        }
        statusText.enabled = true;
        UpdateStatus("Detecting...");
    }

    private IEnumerator PulseRep()
    {
        repText.fontSize = 80;
        yield return new WaitForSeconds(0.2f);
        repText.fontSize = 60;
    }
}