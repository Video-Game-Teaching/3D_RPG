using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair Settings")]
    public Image crosshairImage; // Image component for the crosshair

    [Header("Visual States")]
    public Color normalColor = Color.white; // Default color
    public Color interactableColor = new Color(0.5f, 1f, 0.5f, 1f); // Color when interactable
    public float normalSize = 20f; // Default size
    public float interactableSize = 25f; // Size when interactable

    [Header("Animation")]
    public float animationSpeed = 5f; // Speed of the animation
    private bool isIneractable = false; // Whether the crosshair is in an interactable state
    private Vector3 targetScale;
    private Color targetColor;

    void Start()
    {
        if (crosshairImage != null)
        {
            // Force set initial color and scale
            crosshairImage.color = normalColor;
            crosshairImage.rectTransform.localScale = Vector3.one;
        }
        SetNormalState();
    }

    void Update()
    {
        if(crosshairImage == null) return;
        
        // Update crosshair based on current interaction mode
        UpdateCrosshairForCurrentMode();
        
        // Smoothly animate the crosshair to the target scale and color
        crosshairImage.rectTransform.localScale = Vector3.Lerp(
            crosshairImage.rectTransform.localScale, 
            targetScale, 
            animationSpeed * Time.deltaTime
        );

        crosshairImage.color = Color.Lerp(
            crosshairImage.color, 
            targetColor, 
            animationSpeed * Time.deltaTime
        );
    }

    void UpdateCrosshairForCurrentMode()
    {
        // Check if player controller exists
        if (PlayerController.Instance == null) return;
        
        bool canInteract = false;
        
        if (PlayerController.Instance.IsEmptyHandMode())
        {
            // In empty hand mode, check for both grabbable and pickable objects
            bool canGrab = PlayerController.Instance.CanInteractWithSomething();
            bool canPick = PlayerController.Instance.CanPickItem();
            
            canInteract = canGrab || canPick;
        }
        else if (PlayerController.Instance.IsGrabMode())
        {
            // Check for grabbable objects
            canInteract = PlayerController.Instance.CanInteractWithSomething();
        }
        else if (PlayerController.Instance.IsPickMode())
        {
            // Check for pickable objects
            canInteract = PlayerController.Instance.CanPickItem();
        }
        
        // Update crosshair state
        if (canInteract)
            SetInteractableState();
        else
            SetNormalState();
    }

    // Set the normal state of the crosshair
    public void SetNormalState()
    {
        isIneractable = false;
        targetScale = Vector3.one;
        targetColor = normalColor;
    }

    // Set the interactable state of the crosshair
    public void SetInteractableState()
    {
        isIneractable = true;
        targetScale = Vector3.one * (interactableSize / normalSize);
        targetColor = interactableColor;
    }   

}