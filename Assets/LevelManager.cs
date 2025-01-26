using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Import TextMeshPro namespace

public class LevelManager : MonoBehaviour
{
    [Header("Settings")]
    public MicPitch micPitchScript; // Reference to the Bubble's pitch detection script
    public List<Target> targets; // List of available Targets
    public float levelDuration = 10f; // Duration of the level in seconds

    [Header("UI Settings")]
    public Image countdownImage; // UI Image for the countdown fill
    public TextMeshProUGUI scoreText; // TextMeshPro Text for displaying the score
    public TextMeshProUGUI timeText; // TextMeshPro Text for displaying the remaining time

    [Header("Character Animation")]
    public Animator characterAnimator; // Animator for the character

    private Target currentTarget;
    private bool levelActive = false;
    private int totalScore = 0; // Total score tracker

    void Start()
    {
        UpdateScoreText(); // Initialize score text
        StartNewLevel();
    }

    /// <summary>
    /// Starts a new level by selecting a random target and starting the timer.
    /// </summary>
    public void StartNewLevel()
    {
        if (targets.Count == 0)
        {
            Debug.LogError("No targets available!");
            return;
        }

        // Deactivate all targets
        foreach (var target in targets)
        {
            target.gameObject.SetActive(false);
        }

        // Select and activate a random target
        currentTarget = targets[Random.Range(0, targets.Count)];
        currentTarget.gameObject.SetActive(true);

        Debug.Log($"New Target selected: {currentTarget.name} with scale value: {currentTarget.TargetScale}");

        // Reset the countdown fill
        if (countdownImage != null)
        {
            countdownImage.fillAmount = 1f; // Full circle at the start
        }

        // Reset time display
        if (timeText != null)
        {
            timeText.text = $"{levelDuration:F1}s";
        }

        // Trigger inhalation animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Inhala"); // Play Player_inhalate animation
        }

        // Start the level timer after inhalation animation
        Invoke(nameof(StartLevelTimer), 1.5f); // Assuming the inhalation animation takes 1.5 seconds
    }

    /// <summary>
    /// Starts the level timer and the blowing animation.
    /// </summary>
    private void StartLevelTimer()
    {
        // Trigger blowing animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Sopla"); // Play Player_sopla animation
        }

        // Start the level timer
        levelActive = true;
        StartCoroutine(LevelTimer());
    }

    /// <summary>
    /// Timer coroutine for the level duration.
    /// </summary>
    private IEnumerator LevelTimer()
    {
        float timeRemaining = levelDuration;

        while (timeRemaining > 0)
        {
            // Update the countdown fill
            if (countdownImage != null)
            {
                countdownImage.fillAmount = timeRemaining / levelDuration;
            }

            // Update the time display
            if (timeText != null)
            {
                timeText.text = $"{timeRemaining:F1}s";
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        // Final time update
        if (timeText != null)
        {
            timeText.text = "0.0s";
        }

        // End the level
        levelActive = false;

        // Trigger "no air" animation
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("SinAire"); // Play Player_sinaire animation
        }

        // Delay returning to idle
        Invoke(nameof(TransitionToIdle), 2.5f); // Assuming the "no air" animation takes 1.5 seconds
    }

    /// <summary>
    /// Transitions the character back to the idle animation.
    /// </summary>
    private void TransitionToIdle()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("FullAire"); // Play Player_Idle animation
        }

        CheckResult();
    }

    /// <summary>
    /// Compares the Bubble's scale to the Target's value at the end of the timer and calculates the score.
    /// </summary>
    private void CheckResult()
    {
        if (micPitchScript == null || currentTarget == null)
        {
            Debug.LogError("Bubble script or current target is missing!");
            return;
        }

        // Use currentSphereScale from the Bubble script
        float bubbleScale = micPitchScript.currentSphereScale;
        float targetScale = currentTarget.TargetScale;

        Debug.Log($"Bubble Scale: {bubbleScale:F2}, Target Scale: {targetScale:F2}");

        // Calculate the score
        int score = 0;
        if (Mathf.Approximately(bubbleScale, targetScale))
        {
            // Perfect match
            score = 100;
        }
        else if (bubbleScale < targetScale)
        {
            // Partial score for close match
            score = Mathf.Max(0, Mathf.RoundToInt(100 - Mathf.Abs(targetScale - bubbleScale)));
        }

        // Update total score
        totalScore += score;
        Debug.Log($"Score for this level: {score}, Total Score: {totalScore}");

        // Update score UI
        UpdateScoreText();

        // Deactivate the current target
        currentTarget.gameObject.SetActive(false);

        // Optionally: Start a new level after a short delay
        Invoke(nameof(StartNewLevel), 2f);
    }

    /// <summary>
    /// Updates the score text on the screen.
    /// </summary>
    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }
        else
        {
            Debug.LogWarning("Score Text is not assigned!");
        }
    }
}
