using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioPitchEstimator))]
public class MicPitch : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioPitchEstimator pitchEstimator;

    [Header("Mic / Estimation Settings")]
    [SerializeField] private float estimateInterval = 0.1f;

    [Header("Clap Detection (Using OnAudioFilterRead)")]
    [Tooltip("Peak amplitude above this threshold is considered a clap.")]
    public float clapAmplitudeThreshold = 0.3f;

    [Header("Amplitude Threshold")]
    [Tooltip("Minimum amplitude required to consider detecting pitch.")]
    public float minAmplitudeForDetection = 0.01f;

    // ------------------------------------------------------
    // 1) Define 18 discrete pitch ranges between 80–600 Hz.
    // ------------------------------------------------------
    [System.Serializable]
    public struct PitchRange
    {
        public float minFreq;
        public float maxFreq;
        public float scale;
        public int scaleIndex;
    }

    [Header("Discrete Pitch Ranges")]
    [Tooltip("List of discrete pitch ranges. If the pitch stays in one range enough, we smooth to that range's scale.")]
    public PitchRange[] pitchRanges = new PitchRange[]
    {
        new PitchRange { minFreq =  80f, maxFreq = 110f, scale =  1.5f, scaleIndex = 1 },
        new PitchRange { minFreq = 110f, maxFreq = 130f, scale =  2.5f, scaleIndex = 2 },
        new PitchRange { minFreq = 130f, maxFreq = 150f, scale =  3.5f, scaleIndex = 3 },
        new PitchRange { minFreq = 150f, maxFreq = 190f, scale =  6f, scaleIndex = 4 },
        new PitchRange { minFreq = 190f, maxFreq = 210f, scale =  10f, scaleIndex = 5 },
        new PitchRange { minFreq = 210f, maxFreq = 300f, scale =  12f, scaleIndex = 6 }
    };

    [Header("Consecutive Detections")]
    [Tooltip("Number of consecutive pitch detections in the same range required before updating sphere scale.")]
    public int consecutiveThreshold = 2; // lower = more responsive

    [Header("Sphere / Smoothing")]
    public Transform sphereTransform;
    [Tooltip("How quickly we lerp to the new scale once triggered.")]
    public float scaleLerpSpeed = 8f;

    [Header("Silence Settings")]
    public float silenceThreshold = 2f; // Time before shrinking starts
    public float shrinkSpeed = 0.6f; // Speed of shrinking
    public GameObject explosionEffect; // Explosion prefab
    public LevelManager levelManager; // Reference to the LevelManager

    private bool isResizingFinished = false;
    public float currentSphereScale = 1f;
    public int currentScaleIndex = -1;
    private float targetSphereScale = 1f;
    private int currentRangeIndex = -1;
    private int consecutiveCount = 0;
    private float latestAmplitude = 0f;
    private float silenceTimer = 0f; // Timer to track silence

    [Header("Resizing Control")]
    public bool allowResizing = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        pitchEstimator = GetComponent<AudioPitchEstimator>();

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        audioSource.clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
        while (Microphone.GetPosition(null) <= 0) { }
        audioSource.loop = true;

        audioSource.Play();

        InvokeRepeating(nameof(EstimatePitch), 0f, estimateInterval);
        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * currentSphereScale;
        }
    }

    void Update()
    {
        if (isResizingFinished || sphereTransform == null || !allowResizing) return;

        if (latestAmplitude < minAmplitudeForDetection)
        {
            silenceTimer += Time.deltaTime;
            if (silenceTimer >= silenceThreshold && levelManager.levelActive)
            {
                ShrinkBubble();
                return;
            }
        }
        else
        {
            silenceTimer = 0f; // Reset the timer
        }

        currentSphereScale = Mathf.Lerp(currentSphereScale, targetSphereScale, scaleLerpSpeed * Time.deltaTime);
        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * currentSphereScale;
        }
    }

    void ShrinkBubble()
    {
        if (sphereTransform == null) return;

        currentSphereScale -= shrinkSpeed * Time.deltaTime;
        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * Mathf.Max(currentSphereScale, 0f);
        }

        if (currentSphereScale <= 0f)
        {
            ExplodeBubble();
        }
    }

    void ExplodeBubble()
    {
        if (sphereTransform == null) return;

        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, sphereTransform.position, Quaternion.identity);
        }

        Debug.Log("Bubble exploded! You lose.");
        if (levelManager != null)
        {
            levelManager.GameOver();
        }

        Destroy(sphereTransform.gameObject);
        sphereTransform = null;
    }

    void EstimatePitch()
    {
        if (isResizingFinished || sphereTransform == null) return;
        if (latestAmplitude < minAmplitudeForDetection) return;

        float newPitch = pitchEstimator.Estimate(audioSource);
        if (float.IsNaN(newPitch)) return;

        int foundIndex = -1;
        for (int i = 0; i < pitchRanges.Length; i++)
        {
            if (newPitch >= pitchRanges[i].minFreq && newPitch < pitchRanges[i].maxFreq)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex < 0) return;

        if (foundIndex == currentRangeIndex)
        {
            consecutiveCount++;
        }
        else
        {
            currentRangeIndex = foundIndex;
            consecutiveCount = 1;
        }

        if (consecutiveCount >= consecutiveThreshold)
        {
            float newScale = pitchRanges[currentRangeIndex].scale;
            int newScaleIndex = pitchRanges[currentRangeIndex].scaleIndex;

            if (!Mathf.Approximately(targetSphereScale, newScale))
            {
                targetSphereScale = newScale;
                currentScaleIndex = newScaleIndex;
                Debug.Log(currentScaleIndex);
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float peak = 0f;
        for (int i = 0; i < data.Length; i += channels)
        {
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        }
        latestAmplitude = peak;
    }
}
