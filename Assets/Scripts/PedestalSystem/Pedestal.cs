using UnityEngine;
using System.Collections.Generic;

// Main pedestal class that handles item placement and effect triggering
public class Pedestal : MonoBehaviour
{
    [Header("Pedestal Settings")]
    public Transform itemPlacePoint; // Where items will be placed
    public float placementRadius = 0.5f; // Detection radius for placement
    public LayerMask itemLayerMask = -1; // Layers for placeable items
    public Vector3 itemPlaceOffset = new Vector3(0, 1f, 0); // Offset to place items above pedestal
    
    [Header("Visual Effects")]
    public GameObject highlightEffect; // Highlight effect when empty
    public GameObject placementEffect; // Effect when item is placed
    public Color highlightColor = Color.yellow;
    
    [Header("Audio")]
    public AudioClip placeSound;
    public AudioClip removeSound;
    public AudioClip activateSound;
    
    [Header("Pedestal Type")]
    public PedestalType pedestalType = PedestalType.Universal;
    
    [Header("Item Effects")]
    public List<IItemEffect> itemEffects = new List<IItemEffect>(); // List of effects
    
    // Current state
    private Pickable currentItem;
    private bool isActivated = false;
    private AudioSource audioSource;
    private Renderer highlightRenderer;
    
    // Events
    public System.Action<Pickable> OnItemPlaced;
    public System.Action<Pickable> OnItemRemoved;
    public System.Action<Pedestal> OnPedestalActivated;
    
    public enum PedestalType
    {
        Universal,      // Can place any item
        Specific,       // Can only place specific items
        Combination     // Requires multiple specific items
    }
    
    void Start()
    {
        SetupComponents();
        SetupHighlight();
        // If no itemEffects were assigned via inspector (interface lists aren't serialized in Unity),
        // try to auto-populate from components on this GameObject that implement IItemEffect.
        AutoPopulateItemEffectsIfEmpty();
    }
    
    void Update()
    {
        UpdateHighlight();
    }
    
    void SetupComponents()
    {
        // Get or add audio component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Setup item place point
        if (itemPlacePoint == null)
        {
            GameObject placePoint = new GameObject("ItemPlacePoint");
            placePoint.transform.SetParent(transform);
            placePoint.transform.localPosition = Vector3.up * 0.5f;
            itemPlacePoint = placePoint.transform;
        }
    }
    
    void SetupHighlight()
    {
        if (highlightEffect != null)
        {
            highlightRenderer = highlightEffect.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.material.color = highlightColor;
            }
        }
    }
    
    void UpdateHighlight()
    {
        if (highlightEffect != null)
        {
            // Show highlight when empty and not activated
            bool shouldHighlight = currentItem == null && !isActivated;
            highlightEffect.SetActive(shouldHighlight);
        }
    }
    
    // Try to place an item on the pedestal
    public bool TryPlaceItem(Pickable item)
    {
        if (currentItem != null)
        {
            return false;
        }
        
        // Check if pedestal type allows this item
        if (!CanPlaceItem(item))
        {
            return false;
        }
        
        // Place the item
        PlaceItem(item);
        return true;
    }
    
    // Check if this item can be placed
    bool CanPlaceItem(Pickable item)
    {
        // Temporary fix: always allow placement for testing
        if (item.itemName == "Key")
        {
            return true;
        }
        
        switch (pedestalType)
        {
            case PedestalType.Universal:
                return true;
                
            case PedestalType.Specific:
                // Check if item is in allowed list
                // collect required names for comparison logging if none match
                var requiredNames = new System.Collections.Generic.List<string>();
                foreach (var effect in itemEffects)
                {
                    requiredNames.Add(effect.RequiredItemName ?? "");
                    if (effect.RequiredItemName == item.itemName)
                        return true;
                }
                // Log only the comparison of required names vs the actual item name
                string reqs = string.Join(", ", requiredNames.ToArray());
                Debug.Log($"Pedestal comparison: actual='{item.itemName}', required=[{reqs}]");
                return false;
                
            case PedestalType.Combination:
                // Combination pedestals need to check combination requirements
                return CheckCombinationRequirement(item);
                
            default:
                return false;
        }
    }
    
    // Check combination requirements
    bool CheckCombinationRequirement(Pickable item)
    {
        // Here you can implement complex combination logic
        // For example: need specific order of items, or specific number of items
        return true; // Simplified implementation
    }

    // Unity won't serialize lists of interfaces in the inspector reliably.
    // If the designer attached concrete effect components (MonoBehaviours) to the Pedestal,
    // discover them at runtime and populate the itemEffects list so Specific pedestals work.
    void AutoPopulateItemEffectsIfEmpty()
    {
        if (itemEffects == null)
            itemEffects = new List<IItemEffect>();

        if (itemEffects.Count > 0)
            return; // already assigned

        // Find MonoBehaviour components that implement IItemEffect and add them
        var monos = GetComponents<MonoBehaviour>();
        foreach (var mb in monos)
        {
            if (mb is IItemEffect ie)
            {
                itemEffects.Add(ie);
            }
        }
    }
    
    // Actually place the item
    void PlaceItem(Pickable item)
    {
        // Create a copy of the item for display on pedestal
        GameObject itemCopy = Instantiate(item.gameObject);
        Pickable itemCopyComponent = itemCopy.GetComponent<Pickable>();
        
        // Set the copy as current item
        currentItem = itemCopyComponent;
        
        // Set item position and rotation with offset
        Vector3 placePosition = itemPlacePoint.position + itemPlaceOffset;
        itemCopy.transform.position = placePosition;
        itemCopy.transform.rotation = itemPlacePoint.rotation;
        itemCopy.transform.SetParent(itemPlacePoint);
        
        // Disable item physics
        var rb = itemCopy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        // Make sure the item is active
        itemCopy.SetActive(true);
        
        // Play sound
        if (placeSound != null)
            audioSource.PlayOneShot(placeSound);
        
        // Play placement effect
        if (placementEffect != null)
        {
            GameObject effect = Instantiate(placementEffect, itemPlacePoint.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Trigger effects
        TriggerItemEffect(itemCopyComponent);
        
        // Trigger event
        OnItemPlaced?.Invoke(itemCopyComponent);
    }
    
    // Remove item from pedestal
    public Pickable RemoveItem()
    {
        if (currentItem == null)
        {
            return null;
        }
        
        Pickable removedItem = currentItem;
        
        // Remove from pedestal
        removedItem.transform.SetParent(null);
        currentItem = null;
        isActivated = false;
        
        // Play sound
        if (removeSound != null)
            audioSource.PlayOneShot(removeSound);
        
        // Trigger event
        OnItemRemoved?.Invoke(removedItem);
        
        return removedItem;
    }
    
    // Trigger item effects
    void TriggerItemEffect(Pickable item)
    {
        foreach (var effect in itemEffects)
        {
            if (effect.RequiredItemName == item.itemName)
            {
                effect.TriggerEffect(this, item);
                isActivated = true;
                
                // Play activation sound
                if (activateSound != null)
                    audioSource.PlayOneShot(activateSound);
                
                // Trigger activation event
                OnPedestalActivated?.Invoke(this);
                break;
            }
        }
    }
    
    // Check if player is in interaction range
    public bool IsPlayerInRange(Transform player)
    {
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= placementRadius * 2f; // Expand detection range
    }
    
    // Get current item
    public Pickable GetCurrentItem()
    {
        return currentItem;
    }
    
    // Check if pedestal is activated
    public bool IsActivated()
    {
        return isActivated;
    }
    
    // Reset pedestal state
    public void ResetPedestal()
    {
        if (currentItem != null)
        {
            RemoveItem();
        }
        isActivated = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw placement detection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, placementRadius);
        
        // Draw item place point
        if (itemPlacePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(itemPlacePoint.position, 0.1f);
            
            // Draw actual placement position with offset
            Vector3 actualPlacePosition = itemPlacePoint.position + itemPlaceOffset;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(actualPlacePosition, 0.1f);
            Gizmos.DrawLine(itemPlacePoint.position, actualPlacePosition);
        }
    }
}
