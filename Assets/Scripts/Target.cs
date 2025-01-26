using UnityEngine;

public class Target : MonoBehaviour
{
    [Header("Target Properties")]
    public float TargetScale = 5f; // The scale value for comparison

    void Start()
    {
        // Optional: Log the target's scale value for debugging
        Debug.Log($"Target {name} initialized with scale value: {TargetScale}");
    }
}
