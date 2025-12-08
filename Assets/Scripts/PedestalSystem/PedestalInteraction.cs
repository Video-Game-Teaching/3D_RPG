using UnityEngine;

// Handles player interaction with pedestals
public class PedestalInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 3f;
    public LayerMask pedestalLayerMask = -1;
    public KeyCode interactionKey = KeyCode.E;
    public Camera playerCamera;
    
    [Header("UI Prompt")]
    public GameObject interactionPrompt;
    public string placeItemText = "Press E to place item";
    public string removeItemText = "Press E to remove item";
    
    private Pedestal currentPedestal;
    private PlayerController playerController;

    
    void Start()
    {
        playerController = PlayerController.Instance;
        
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    void Update()
    {
        CheckForPedestal();
        HandleInteraction();
    }
    
    // Check if player is looking at a pedestal
    void CheckForPedestal()
    {
        if (playerCamera == null) return;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, pedestalLayerMask))
        {
            Pedestal pedestal = hit.collider.GetComponent<Pedestal>();
            if (pedestal != null)
            {
                currentPedestal = pedestal;
                ShowInteractionPrompt();
            }
            else
            {
                ClearCurrentPedestal();
            }
        }
        else
        {
            ClearCurrentPedestal();
        }
    }
    
    // Handle interaction input
    void HandleInteraction()
    {
        if (currentPedestal == null) return;
        if (Input.GetKeyDown(interactionKey))
        {
            if (currentPedestal.GetCurrentItem() == null)
            {
                TryPlaceItemOnPedestal();
            }
            else
            {
                TryRemoveItemFromPedestal();
            }
        }
    }
    
    // Try to place current equipped item on pedestal
    void TryPlaceItemOnPedestal()
    {
        if (playerController == null) return;

        // Check if player has an equipped item
        Pickable equippedItem = playerController.GetCurrentEquippedItem();
        if (equippedItem == null)
        {
            return;
        }

        // Try to place the item
        bool placed = currentPedestal.TryPlaceItem(equippedItem);
        if (placed)
        {
            // Remove item from player inventory
            RemoveItemFromPlayer(equippedItem);
        }
    }
    
    // Try to remove item from pedestal
    void TryRemoveItemFromPedestal()
    {
        if (playerController == null) return;
        
        // Check if player inventory is full
        if (playerController.GetInventoryCount() >= playerController.maxItems)
        {
            return;
        }
        
        // Remove item from pedestal
        Pickable removedItem = currentPedestal.RemoveItem();
        if (removedItem != null)
        {
            // Add item back to player inventory
            AddItemToPlayer(removedItem);
        }
    }
    
    // Remove item from player inventory
    void RemoveItemFromPlayer(Pickable item)
    {
        // Remove the currently equipped item from inventory and destroy it
        // The pedestal will create a copy for display
        playerController.RemoveCurrentEquippedItem();
    }
    
    // Add item to player inventory
    void AddItemToPlayer(Pickable item)
    {
        // Show the equipped item again
        playerController.ShowCurrentEquippedItem();
        
        // Destroy the pedestal copy
        if (item != null)
        {
            Destroy(item.gameObject);
        }
    }
    
    // Show interaction prompt
    void ShowInteractionPrompt()
    {
        if (interactionPrompt == null) return;
        
        interactionPrompt.SetActive(true);
        
        // Update prompt text
        var textComponent = interactionPrompt.GetComponentInChildren<UnityEngine.UI.Text>();
        if (textComponent != null)
        {
            if (currentPedestal.GetCurrentItem() == null)
            {
                textComponent.text = placeItemText;
            }
            else
            {
                textComponent.text = removeItemText;
            }
        }
    }
    
    // Hide interaction prompt
    void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    // Clear current pedestal reference
    void ClearCurrentPedestal()
    {
        if (currentPedestal != null)
        {
            currentPedestal = null;
            HideInteractionPrompt();
        }
    }
    
    // Check if player can interact with pedestal
    public bool CanInteractWithPedestal()
    {
        return currentPedestal != null;
    }
    
    // Get current pedestal
    public Pedestal GetCurrentPedestal()
    {
        return currentPedestal;
    }
}
