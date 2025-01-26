using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Import TextMeshPro namespace

public class LevelManager : MonoBehaviour
{
    [Header("Settings")]
    public GameObject bubblePrefab; // Bubble prefab to instantiate
    public MicPitch micPitchScript; // Reference to the Bubble's pitch detection script
    public List<Target> targets; // List of available Targets
    public float levelDuration = 10f; // Duration of the level in seconds
    public float idleInterval = 0.1f; // Time to stay in the idle state between levels

    [Header("UI Settings")]
    public Image countdownImage; // UI Image for the countdown fill
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI scoreText; // TextMeshPro Text for displaying the score
    public TextMeshProUGUI timeText; // TextMeshPro Text for displaying the remaining time

    [Header("Character Animation")]
    public Animator characterAnimator; // Animator for the character

    private Target currentTarget;
    public bool levelActive = false;
    private int totalScore = 0; // Total score tracker
    private GameObject currentBubble; // Current instantiated Bubble

    public TextMeshProUGUI gameOverText; // TextMeshPro Text for displaying "Game Over"
    public GameObject restartButton; // Button to restart the game

    public AudioClip successSound; // Sound to play when the user scores
    public AudioClip errorSound; // Sound to play when error
    public AudioSource audioSource; // AudioSource to play sounds
    private bool resultChecked = false;

    void Start()
    {
        UpdateScoreText(); // Initialize score text
        StartNewLevel();
    }

    /// <summary>
    /// Starts a new level by selecting a random target, instantiating the Bubble, and starting the timer.
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

        // Instantiate the Bubble prefab
        if (bubblePrefab != null)
        {
            if (currentBubble != null)
            {
                Destroy(currentBubble); // Remove the previous Bubble
            }

            // Instantiate the Bubble at position (0, 0, -13.1)
            Vector3 bubblePosition = new Vector3(0f, 0f, -13.1f);
            currentBubble = Instantiate(bubblePrefab, bubblePosition, Quaternion.identity);
            currentBubble.SetActive(false);
            micPitchScript.sphereTransform = currentBubble.transform;
        }
        else
        {
            Debug.LogError("Bubble prefab is not assigned!");
        }

        // Trigger the idle animation before starting the level
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("FullAire"); // Play Player_Idle animation
        }

        StartCoroutine(CountdownToStart());

        // Delay before transitioning to inhalation
        Invoke(nameof(PlayInhalationAnimation), idleInterval);
    }

    private IEnumerator CountdownToStart()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true); // Show the countdown text
            for (int i = 3; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSeconds(1f); // Wait for 1 second
            }

            countdownText.gameObject.SetActive(false); // Hide the countdown text
        }

    }

    private void PlayInhalationAnimation()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Inhala"); // Play Player_inhalate animation
        }

        Invoke(nameof(StartLevelTimer), 1.0f);
    }

    private void StartLevelTimer()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Sopla"); // Play Player_sopla animation
        }
        currentBubble.SetActive(true);
        levelActive = true;
        micPitchScript.allowResizing = true; // Allow resizing
        StartCoroutine(LevelTimer());
    }

    private IEnumerator LevelTimer()
    {
        float timeRemaining = levelDuration;

        while (timeRemaining > 0)
        {
            if (countdownImage != null)
            {
                countdownImage.fillAmount = timeRemaining / levelDuration;
            }

            if (timeText != null)
            {
                timeText.text = $"{timeRemaining:F1}s";
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        if (timeText != null)
        {
            timeText.text = "0.0s";
        }

        levelActive = false;

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("SinAire"); // Play Player_sinaire animation
        }

        micPitchScript.allowResizing = false; // Disable resizing
        Invoke(nameof(TransitionToIdle), 1.0f); // Adjust timing as needed
    }

    private void TransitionToIdle()
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("FullAire"); // Play Player_Idle animation
        }

        Invoke(nameof(CheckResult), 0.5f);
    }

    private void CheckResult()
    {
        if (micPitchScript == null || currentTarget == null)
        {
            Debug.LogError("Bubble script or current target is missing!");
            return;
        }

        float bubbleScale = micPitchScript.currentSphereScale;
        float targetScale = currentTarget.TargetScale;

        Debug.Log($"Bubble Scale: {bubbleScale:F2}, Target Scale: {targetScale:F2}");

        int score = 0;
        if (Mathf.Approximately(bubbleScale, targetScale))
        {
            score = 100;
        }
        else if (bubbleScale < targetScale)
        {
            score = Mathf.Max(0, Mathf.RoundToInt(100 - Mathf.Abs(targetScale - bubbleScale*10)));
        }
        else if(bubbleScale > targetScale)
        {
            Debug.Log("Here");
            // Play error sound
            if (errorSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(errorSound);
            }

        }

        totalScore += score;
        Debug.Log($"Score for this level: {score}, Total Score: {totalScore}");

        // Play success sound if the user scores
        if (score > 0 && successSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(successSound);
        }

        UpdateScoreText();

        currentTarget.gameObject.SetActive(false);

        Invoke(nameof(StartNewLevel), 2f);
    }

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

    public void GameOver()
    {
        Debug.Log("Game Over!");
        levelActive = false;
        CancelInvoke();
        StopAllCoroutines();

        if (gameOverText != null)
        {
            gameOverText.text = "Game Over!";
            gameOverText.gameObject.SetActive(true);
        }

        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }

        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("FullAire");
        }
    }
}
