using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioPitchEstimator))]
public class RangeBasedMicPitchWide18 : MonoBehaviour
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
    }

    [Header("Discrete Pitch Ranges")]
    [Tooltip("List of discrete pitch ranges. If the pitch stays in one range enough, we smooth to that range's scale.")]
    public PitchRange[] pitchRanges = new PitchRange[]
    {
        // 18 intervals from 80–600 Hz (~30 Hz each)
        new PitchRange { minFreq =  80f, maxFreq = 110f, scale =  1f },
        new PitchRange { minFreq = 110f, maxFreq = 130f, scale =  2f },
        new PitchRange { minFreq = 130f, maxFreq = 150f, scale =  3f },
        new PitchRange { minFreq = 150f, maxFreq = 170f, scale =  4f },
        new PitchRange { minFreq = 170f, maxFreq = 190f, scale =  5f },
        new PitchRange { minFreq = 190f, maxFreq = 210f, scale =  6f },
        new PitchRange { minFreq = 210f, maxFreq = 230f, scale =  7f },
        new PitchRange { minFreq = 230f, maxFreq = 250f, scale =  8f },
        new PitchRange { minFreq = 250f, maxFreq = 270f, scale =  9f },
        new PitchRange { minFreq = 270f, maxFreq = 300f, scale = 10f },
        new PitchRange { minFreq = 300f, maxFreq = 330f, scale = 11f },
        new PitchRange { minFreq = 330f, maxFreq = 360f, scale = 12f },
        new PitchRange { minFreq = 360f, maxFreq = 390f, scale = 13f },
        new PitchRange { minFreq = 390f, maxFreq = 420f, scale = 14f },
        new PitchRange { minFreq = 420f, maxFreq = 450f, scale = 15f },
        new PitchRange { minFreq = 450f, maxFreq = 500f, scale = 16f },
        new PitchRange { minFreq = 500f, maxFreq = 550f, scale = 17f },
        new PitchRange { minFreq = 550f, maxFreq = 600f, scale = 18f },
    };

    [Header("Consecutive Detections")]
    [Tooltip("Number of consecutive pitch detections in the same range required before updating sphere scale.")]
    public int consecutiveThreshold = 2; // lower = more responsive

    [Header("Sphere / Smoothing")]
    public Transform sphereTransform;
    [Tooltip("How quickly we lerp to the new scale once triggered.")]
    public float scaleLerpSpeed = 8f;

    // Once a clap is detected, resizing stops
    private bool isResizingFinished = false;

    // We'll track the "current" displayed scale (smooth), and the "target" scale
    private float currentSphereScale = 1f;
    private float targetSphereScale = 1f;

    // We'll track how many consecutive times we've detected the same pitch range
    private int currentRangeIndex = -1;
    private int consecutiveCount = 0;

    // Clap amplitude
    private float latestAmplitude = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        pitchEstimator = GetComponent<AudioPitchEstimator>();

        // Check for mic
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        // Start mic
        audioSource.clip = Microphone.Start(Microphone.devices[0], true, 10, 44100);
        while (Microphone.GetPosition(null) <= 0) { }

        // Mute if you don't want to hear yourself (optional):
        // audioSource.mute = true;

        audioSource.loop = true;
        audioSource.Play();

        // Start pitch detection
        InvokeRepeating(nameof(EstimatePitch), 0f, estimateInterval);

        // Initialize sphere scale
        currentSphereScale = 1f;
        targetSphereScale = currentSphereScale;

        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * currentSphereScale;
        }
    }

    void Update()
    {
        // If user clapped, freeze resizing
        if (isResizingFinished) return;

        // Clap check
        if (latestAmplitude > clapAmplitudeThreshold)
        {
            Debug.Log($"Clap detected! amplitude: {latestAmplitude:F3} > threshold: {clapAmplitudeThreshold}");
            isResizingFinished = true;
            return;
        }

        // Smoothly lerp from currentSphereScale to targetSphereScale
        currentSphereScale = Mathf.Lerp(
            currentSphereScale,
            targetSphereScale,
            scaleLerpSpeed * Time.deltaTime
        );

        // Apply scale to the sphere
        if (sphereTransform != null)
        {
            sphereTransform.localScale = Vector3.one * currentSphereScale;
        }
    }

    /// <summary>
    /// Detects pitch, finds range, requires consecutive detections, sets 'targetSphereScale' if threshold met.
    /// </summary>
    void EstimatePitch()
    {
        // If we've stopped resizing, skip
        if (isResizingFinished) return;

        // 1) Check amplitude before detection
        if (latestAmplitude < minAmplitudeForDetection)
        {
            // Not loud enough, skip pitch detection
            // Debug.Log("Amplitude too low, skipping pitch detection.");
            return;
        }

        if (audioSource.clip == null) return;

        // 2) Try to estimate pitch
        float newPitch = pitchEstimator.Estimate(audioSource);
        if (float.IsNaN(newPitch))
        {
            // No pitch detected, ignore
            return;
        }

        // 3) Determine which range the pitch belongs to
        int foundIndex = -1;
        for (int i = 0; i < pitchRanges.Length; i++)
        {
            if (newPitch >= pitchRanges[i].minFreq && newPitch < pitchRanges[i].maxFreq)
            {
                foundIndex = i;
                break;
            }
        }

        // If out of all defined ranges, ignore
        if (foundIndex < 0)
        {
            return;
        }

        // 4) Check consecutive detection
        if (foundIndex == currentRangeIndex)
        {
            consecutiveCount++;
        }
        else
        {
            currentRangeIndex = foundIndex;
            consecutiveCount = 1;
        }

        // 5) Once threshold is reached, update target scale
        if (consecutiveCount >= consecutiveThreshold)
        {
            float newScale = pitchRanges[currentRangeIndex].scale;
            if (!Mathf.Approximately(targetSphereScale, newScale))
            {
                Debug.Log($"==> Target scale set to {newScale} (range index {currentRangeIndex}, freq: {newPitch:F2} Hz)");
                targetSphereScale = newScale;
            }
        }
    }

    /// <summary>
    /// Captures the peak amplitude in OnAudioFilterRead for clap detection and amplitude gating.
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        float peak = 0f;
        for (int i = 0; i < data.Length; i += channels)
        {
            float absSample = Mathf.Abs(data[i]);
            if (absSample > peak)
            {
                peak = absSample;
            }
        }

        latestAmplitude = peak;
    }
}
