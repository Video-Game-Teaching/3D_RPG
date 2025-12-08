using UnityEngine;

/// <summary>
/// Creates a floating, rotating, and glowing effect for collectible items like keys.
/// Attach this script to any object to give it a mystical floating appearance.
/// </summary>
public class FloatingRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second")]
    public float rotationSpeed = 45f;
    
    [Tooltip("Axis to rotate around (normalized automatically)")]
    public Vector3 rotationAxis = new Vector3(1, 1, 1);

    [Header("Floating Settings")]
    [Tooltip("How high/low the object floats from its starting position")]
    public float floatAmplitude = 0.15f;
    
    [Tooltip("Speed of the floating motion (cycles per second)")]
    public float floatFrequency = 0.3f;
    
    [Tooltip("Offset the floating phase (0-1)")]
    [Range(0f, 1f)]
    public float floatPhaseOffset = 0f;

    [Header("Glow/Emission Settings")]
    [Tooltip("Enable pulsing emission glow effect")]
    public bool enableGlow = true;
    
    [Tooltip("Base color for the glow effect (set your green color here)")]
    public Color baseColor = new Color(0f, 0.749f, 0.118f, 1f); // #00BF1E
    
    [Tooltip("Minimum brightness multiplier (1 = no glow)")]
    public float glowMinBrightness = 1f;
    
    [Tooltip("Maximum brightness multiplier (2+ = HDR highlight glow)")]
    public float glowMaxBrightness = 2f;
    
    [Tooltip("Speed of the glow pulse (cycles per second)")]
    public float glowFrequency = 0.3f;

    [Header("Scale Pulse Settings")]
    [Tooltip("Enable breathing/pulse scale effect")]
    public bool enableScalePulse = true;
    
    [Tooltip("How much the scale changes (0.1 = 10% bigger/smaller)")]
    public float scalePulseAmount = 0.1f;
    
    [Tooltip("Speed of the scale pulse")]
    public float scalePulseFrequency = 0.3f;

    // Private variables
    private Vector3 startPosition;
    private Vector3 startScale;
    private float timeOffset;
    private Renderer[] renderers;
    private MaterialPropertyBlock mpb;

    void Start()
    {
        // Store initial position and scale
        startPosition = transform.position;
        startScale = transform.localScale;
        
        // Apply random phase offset for variety when multiple objects use this script
        timeOffset = floatPhaseOffset * (1f / floatFrequency);
        
        // Cache renderers for emission effect
        renderers = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
        
        // Initialize materials with base color and enable emission
        if (enableGlow)
        {
            foreach (var rend in renderers)
            {
                if (rend.material != null)
                {
                    // Enable emission keyword
                    rend.material.EnableKeyword("_EMISSION");
                    // Set the main color to our base color
                    rend.material.SetColor("_Color", baseColor);
                    // Initialize emission color
                    rend.material.SetColor("_EmissionColor", baseColor);
                }
            }
        }
    }

    void Update()
    {
        float time = Time.time + timeOffset;
        
        // Apply rotation
        ApplyRotation();
        
        // Apply floating effect
        ApplyFloating(time);
        
        // Apply glow effect
        if (enableGlow)
        {
            ApplyGlow(time);
        }
        
        // Apply scale pulse
        if (enableScalePulse)
        {
            ApplyScalePulse(time);
        }
    }

    void ApplyRotation()
    {
        // Rotate around the specified axis
        transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, Space.World);
    }

    void ApplyFloating(float time)
    {
        // Calculate floating offset using sine wave
        float floatOffset = Mathf.Sin(time * floatFrequency * 2f * Mathf.PI) * floatAmplitude;
        
        // Apply new position
        Vector3 newPosition = startPosition;
        newPosition.y += floatOffset;
        transform.position = newPosition;
    }

    void ApplyGlow(float time)
    {
        // Calculate pulsing intensity using sine wave (0 to 1 range)
        float pulse = (Mathf.Sin(time * glowFrequency * 2f * Mathf.PI) + 1f) * 0.5f;
        
        // Emission intensity goes from 0 (no glow) to max brightness
        float emissionIntensity = Mathf.Lerp(glowMinBrightness, glowMaxBrightness, pulse);
        
        // Base color stays as-is, emission pulses for the glow highlight effect
        Color emissionColor = baseColor * (emissionIntensity - 1f); // subtract 1 so min=0 means no emission
        emissionColor = new Color(
            Mathf.Max(0, emissionColor.r),
            Mathf.Max(0, emissionColor.g),
            Mathf.Max(0, emissionColor.b),
            1f
        );
        
        foreach (var rend in renderers)
        {
            if (rend == null) continue;
            
            rend.GetPropertyBlock(mpb);
            // Keep the main color as base color
            mpb.SetColor("_Color", baseColor);
            // Pulse the emission for the glow effect
            mpb.SetColor("_EmissionColor", emissionColor);
            rend.SetPropertyBlock(mpb);
        }
    }

    void ApplyScalePulse(float time)
    {
        // Calculate scale factor using sine wave
        float pulse = Mathf.Sin(time * scalePulseFrequency * 2f * Mathf.PI) * scalePulseAmount;
        float scaleFactor = 1f + pulse;
        
        // Apply scale
        transform.localScale = startScale * scaleFactor;
    }

    /// <summary>
    /// Reset the starting position (useful if object is moved during gameplay)
    /// </summary>
    public void ResetStartPosition()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
    }
    
    /// <summary>
    /// Set a new base color at runtime
    /// </summary>
    public void SetBaseColor(Color newColor)
    {
        baseColor = newColor;
    }

#if UNITY_EDITOR
    // Visualize the floating range in the editor
    void OnDrawGizmosSelected()
    {
        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        
        Gizmos.color = baseColor;
        // Draw lines showing float range
        Gizmos.DrawLine(pos + Vector3.up * floatAmplitude, pos - Vector3.up * floatAmplitude);
        
        // Draw spheres at float limits
        Gizmos.DrawWireSphere(pos + Vector3.up * floatAmplitude, 0.08f);
        Gizmos.DrawWireSphere(pos - Vector3.up * floatAmplitude, 0.08f);
        
        // Draw a larger sphere to indicate the object
        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f);
        Gizmos.DrawSphere(pos, 0.2f);
    }
#endif
}
