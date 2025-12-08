using UnityEngine;

// Display effect for Key items - makes the pedestal glow when a key is placed
public class KeyDisplayEffect : MonoBehaviour, IItemEffect
{
    [Header("Effect Settings")]
    public string effectName = "Key Display";
    public string requiredItemName;
    
    [Header("Glow Settings")]
    public Color glowColor = Color.yellow;
    public float glowIntensity = 2f;
    public float glowRange = 5f;
    
    private Light glowLight;
    private Renderer pedestalRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    
    // Implement interface properties
    public string EffectName { get { return effectName; } }
    public string RequiredItemName { get { return requiredItemName; } }
    
    void Start()
    {
        // Get the pedestal renderer
        pedestalRenderer = GetComponent<Renderer>();
        if (pedestalRenderer != null)
        {
            // debug
            Debug.Log("Pedestal renderer found");
            originalMaterial = pedestalRenderer.material;
        }
        
        // Create glow material
        CreateGlowMaterial();
    }
    
    void CreateGlowMaterial()
    {
        if (pedestalRenderer != null)
        {
            // Create a copy of the original material for glowing
            glowMaterial = new Material(pedestalRenderer.material);
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
        }
    }
    
    public void TriggerEffect(Pedestal pedestal, Pickable item)
    {
        Debug.Log($"Key display effect triggered by {item.itemName}");
        
        // Add glow light
        AddGlowLight();
        
        // Change material to glowing version
        ChangeToGlowMaterial();
        
        Debug.Log("Key display pedestal is now glowing!");
    }
    
    public void StopEffect(Pedestal pedestal, Pickable item)
    {
        Debug.Log("Key display effect stopped");
        
        // Remove glow light
        RemoveGlowLight();
        
        // Restore original material
        RestoreOriginalMaterial();
        
        Debug.Log("Key display pedestal stopped glowing");
    }
    
    public bool CanTriggerWith(Pickable item)
    {
        return item.itemName == requiredItemName;
    }
    
    void AddGlowLight()
    {
        if (glowLight == null)
        {
            // Add point light to the pedestal
            glowLight = gameObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.intensity = glowIntensity;
            glowLight.range = glowRange;
            glowLight.enabled = true;
        }
    }
    
    void RemoveGlowLight()
    {
        if (glowLight != null)
        {
            Destroy(glowLight);
            glowLight = null;
        }
    }
    
    void ChangeToGlowMaterial()
    {
        if (pedestalRenderer != null && glowMaterial != null)
        {
            pedestalRenderer.material = glowMaterial;
        }
    }
    
    void RestoreOriginalMaterial()
    {
        if (pedestalRenderer != null && originalMaterial != null)
        {
            pedestalRenderer.material = originalMaterial;
        }
    }
    
    void OnDestroy()
    {
        // Clean up glow material
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }
}
