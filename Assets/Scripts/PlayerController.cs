using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum InteractionMode
{
    EmptyHand = 0,
    Grab = 1,
    Pick = 2,
}

public class PlayerController : MonoBehaviour
{
    [Header("Mode Settings")]
    public InteractionMode currentMode = InteractionMode.EmptyHand;

    [Header("UI References")]
    public TextMeshProUGUI modeDisplayText; // UI text to show current mode
    public GameObject modeUI; // UI container for mode display

    [Header("Raycast Settings")]
    public Camera cam; // Main camera used for raycasting
    public float interactDistance = 3f; // Maximum distance to pick up items

    [Header("Grab System")]
    public Transform holdPoint; // The transform where held items will follow
    public float followSpeed = 15f; // Speed for item to follow holdPoint
    public float rotateSpeed = 120f; // Speed for rotating the item
    public bool useParentingToHoldPoint = false; // If true, use parenting for holding

    [Header("Place Validation")]
    public LayerMask placeOnMask; // Layers where items can be placed
    public float placeCheckRadius = 0.25f; // Radius for validating placement
    public LayerMask overlapBlockMask; // Layers that block item placement

    [Header("Pick System")]
    public Transform handPoint; // Transform where equipped items will be displayed
    public int maxItems = 5; // Maximum number of items that can be carried

    [Header("UI")]
    public CrosshairUI crosshairUI; // Reference to the crosshair UI component

    [Header("Input Control")]
    public bool inputEnabled = true; // 控制玩家输入是否启用 / Control if player input is enabled

    // Grab system variables
    private Rigidbody heldRb; // Rigidbody of the currently held item
    private Transform heldTf; // Transform of the currently held item
    private bool prevUseGravity; // Previous gravity state of the item
    private bool prevKinematic; // Previous kinematic state of the item

    // Pick system variables
    private List<Pickable> inventory = new List<Pickable>(); // List of picked items
    private int currentItemIndex = -1; // Index of currently equipped item (-1 = none)
    private GameObject currentEquippedObject; // Currently equipped 3D object

    // UI management
    private Vector2 lastScreenSize;

    public static PlayerController Instance { get; set; }

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Debug.Log($"PlayerController Awake on {gameObject.name}");
    }

    void Start()
    {
        // Force set to empty hand mode on start
        currentMode = InteractionMode.EmptyHand;
        SetupUI();
        UpdateModeDisplay();

        // Lock and hide cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("PlayerController initialized with EmptyHand mode");
    }

    void SetupUI()
    {
        if (modeUI != null)
        {
            RectTransform modeUIRect = modeUI.GetComponent<RectTransform>();
            if (modeUIRect != null)
            {
                // Set anchors to top center
                modeUIRect.anchorMin = new Vector2(0.5f, 1f);
                modeUIRect.anchorMax = new Vector2(0.5f, 1f);

                // Set position to top center with offset from top
                modeUIRect.anchoredPosition = new Vector2(0f, -10f);

                // Ensure the UI is always visible
                modeUIRect.pivot = new Vector2(0.5f, 1f);

                Debug.Log("ModeUI positioned at top center of screen");
            }
        }
    }

    void Update()
    {
        // Skip input handling when game is paused or input is disabled
        if (PauseMenuController.IsPaused || !inputEnabled)
            return;

        // Check for screen resolution changes and update UI position if needed
        CheckAndUpdateUIPosition();

        // Handle different input based on current mode
        switch (currentMode)
        {
            case InteractionMode.EmptyHand:
                HandleEmptyHandInput();
                break;
            case InteractionMode.Grab:
                HandleGrabInput();
                break;
            case InteractionMode.Pick:
                HandlePickInput();
                break;
        }

        // R/T keys for item switching (handled across all modes)
        if (Input.GetKeyDown(KeyCode.R))
        {
            HandleRKey();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            HandleTKey();
        }
    }

    void CheckAndUpdateUIPosition()
    {
        Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
        if (lastScreenSize != currentScreenSize)
        {
            lastScreenSize = currentScreenSize;
            SetupUI(); // Re-setup UI when screen size changes
            Debug.Log(
                $"Screen resolution changed to {currentScreenSize.x}x{currentScreenSize.y}, updating UI position"
            );
        }
    }

    #region Input Handling

    void HandleEmptyHandInput()
    {
        // F key: try to pick item
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickItem();
        }

        // Left mouse: try to grab item
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab();
        }
    }

    void HandleGrabInput()
    {
        // Left mouse button: pick up or place item
        if (Input.GetMouseButtonDown(0))
        {
            if (heldRb == null)
                TryGrab();
            else
                TryPlace();
        }

        if (heldRb != null)
        {
            HoldFollow();

            // Right mouse button: drop item immediately
            if (Input.GetMouseButtonDown(1))
                DropNow();
        }
    }

    void HandlePickInput()
    {
        // F key: try to pick item
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryPickItem();
        }
    }

    void HandleRKey()
    {
        if (currentMode == InteractionMode.Pick)
        {
            SwitchToNextItem();
        }
        else if (currentMode == InteractionMode.Grab && IsHoldingItem())
        {
            // Drop held item when switching to next item
            PlaceHeldItem();
            Debug.Log("Dropped held item when pressing R");
        }
        else if (currentMode == InteractionMode.EmptyHand && GetInventoryCount() > 0)
        {
            // Switch to Pick mode and equip first item
            SwitchToMode(InteractionMode.Pick);
            EquipFirstItem();
        }
    }

    void HandleTKey()
    {
        if (currentMode == InteractionMode.Pick)
        {
            SwitchToPreviousItem();
        }
        else if (currentMode == InteractionMode.Grab && IsHoldingItem())
        {
            // Drop held item when switching to previous item
            PlaceHeldItem();
            Debug.Log("Dropped held item when pressing T");
        }
        else if (currentMode == InteractionMode.EmptyHand && GetInventoryCount() > 0)
        {
            // Switch to Pick mode and equip last item
            SwitchToMode(InteractionMode.Pick);
            EquipLastItem();
        }
    }

    #endregion

    #region Mode Management

    public void SwitchToMode(InteractionMode newMode)
    {
        Debug.Log($"SwitchToMode called: {currentMode} -> {newMode}");
        if (currentMode == newMode)
        {
            Debug.Log("Same mode, returning early");
            return;
        }

        // Handle equipment when switching away from Pick mode
        if (currentMode == InteractionMode.Pick && newMode == InteractionMode.Grab)
        {
            HideCurrentEquippedItem();
        }
        // Handle equipment when switching to Pick mode
        else if (currentMode == InteractionMode.Grab && newMode == InteractionMode.Pick)
        {
            // Auto-throw any held item before switching to Pick mode
            if (IsHoldingItem())
            {
                PlaceHeldItem();
                Debug.Log("Auto-threw held item when switching from Grab to Pick mode");
            }

            ShowCurrentEquippedItem();
        }
        // Handle switching to empty hand
        else if (newMode == InteractionMode.EmptyHand)
        {
            if (currentMode == InteractionMode.Pick)
            {
                HideCurrentEquippedItem();
            }
            else if (currentMode == InteractionMode.Grab && IsHoldingItem())
            {
                PlaceHeldItem();
                Debug.Log("Dropped held item when switching to empty hand");
            }
        }

        currentMode = newMode;
        UpdateModeDisplay();

        Debug.Log($"Switched to {currentMode} mode");
    }

    void UpdateModeDisplay()
    {
        if (modeDisplayText != null)
        {
            string modeText;
            Color modeColor;

            switch (currentMode)
            {
                case InteractionMode.EmptyHand:
                    modeText = "EMPTY HAND";
                    modeColor = Color.white;
                    break;
                case InteractionMode.Grab:
                    modeText = "GRAB MODE";
                    modeColor = Color.cyan;
                    break;
                case InteractionMode.Pick:
                    modeText = "PICK MODE";
                    modeColor = Color.yellow;
                    break;
                default:
                    modeText = "UNKNOWN";
                    modeColor = Color.red;
                    break;
            }

            Debug.Log($"UpdateModeDisplay: Setting UI to '{modeText}' with color {modeColor}");
            modeDisplayText.text = modeText;
            modeDisplayText.color = modeColor;
        }
        else
        {
            Debug.LogWarning("modeDisplayText is null! Cannot update UI display.");
        }
    }

    #endregion

    #region Grab System

    void TryGrab()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            var rb = hit.rigidbody;
            if (rb == null)
                return;

            // Only pick up objects with Grabbable script
            if (hit.collider.GetComponent<Grabbable>() == null)
                return;

            // set isGrabbing to true
            hit.collider.GetComponent<Grabbable>().SetGrabbingState(true);

            heldRb = rb;
            heldTf = rb.transform;

            prevUseGravity = heldRb.useGravity;
            prevKinematic = heldRb.isKinematic;

            if (useParentingToHoldPoint)
            {
                heldRb.isKinematic = true;
                heldRb.useGravity = false;
                heldTf.SetParent(holdPoint, true);
                heldTf.position = holdPoint.position;
                heldTf.rotation = holdPoint.rotation;
            }
            else
            {
                heldRb.isKinematic = false;
                heldRb.useGravity = false;
                heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // Switch to Grab mode when picking up an item
            Debug.Log("Attempting to switch to Grab mode");
            SwitchToMode(InteractionMode.Grab);
            Debug.Log($"After switch, current mode is: {currentMode}");

            Debug.Log($"Grab successful! heldRb: {heldRb != null}, heldTf: {heldTf != null}");
        }
    }

    void HoldFollow()
    {
        if (useParentingToHoldPoint)
            return;

        // Move item towards holdPoint
        Vector3 toTarget = holdPoint.position - heldTf.position;
        Vector3 desiredVel = toTarget * followSpeed;

        float maxSpeed = 20f;
        if (desiredVel.magnitude > maxSpeed)
            desiredVel = desiredVel.normalized * maxSpeed;

        heldRb.velocity = desiredVel;

        // Rotate item to match holdPoint
        Quaternion targetRot = holdPoint.rotation;
        heldRb.MoveRotation(Quaternion.Slerp(heldTf.rotation, targetRot, Time.deltaTime * 10f));
    }

    void TryPlace()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (
            !Physics.Raycast(
                ray,
                out RaycastHit hit,
                100f,
                placeOnMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            DropNow();
            return;
        }

        Vector3 targetPos = hit.point;

        // Check if placement is valid
        if (
            Physics.CheckSphere(
                targetPos,
                placeCheckRadius,
                overlapBlockMask,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return; // Invalid position, cannot place
        }

        if (useParentingToHoldPoint)
            heldTf.SetParent(null, true);
        heldTf.position = targetPos;

        RestorePhysics(dropWithSmallImpulse: true);
    }

    void DropNow()
    {
        // set isGrabbing to false
        heldRb.GetComponent<Grabbable>().SetGrabbingState(false);
        if (useParentingToHoldPoint)
            heldTf.SetParent(null, true);
        RestorePhysics(dropWithSmallImpulse: false);
    }

    void RestorePhysics(bool dropWithSmallImpulse)
    {
        if (heldRb == null)
            return;

        heldRb.useGravity = prevUseGravity;
        heldRb.isKinematic = prevKinematic;

        if (dropWithSmallImpulse)
            heldRb.AddForce(Vector3.down * 1.5f, ForceMode.Impulse);

        heldRb = null;
        heldTf = null;

        // Switch to empty hand mode when dropping item
        SwitchToMode(InteractionMode.EmptyHand);
    }

    // Public method for CrosshairUI to check if we can interact with something
    public bool CanInteractWithSomething()
    {
        // If we're already holding an item, check if we can place it
        if (heldRb != null)
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    100f,
                    placeOnMask,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                Vector3 targetPos = hit.point;
                // Check if placement is valid
                return !Physics.CheckSphere(
                    targetPos,
                    placeCheckRadius,
                    overlapBlockMask,
                    QueryTriggerInteraction.Ignore
                );
            }
            return false;
        }
        // If we're not holding anything, check if we can pick something up
        else
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactDistance,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                var rb = hit.rigidbody;
                return rb != null && hit.collider.GetComponent<Grabbable>() != null;
            }
            return false;
        }
    }

    /// Public method to place/throw held item (calls existing TryPlace logic)
    /// Used when switching modes to automatically throw held items
    public void PlaceHeldItem()
    {
        if (heldRb != null)
        {
            TryPlace();
        }
    }

    /// Check if currently holding an item
    public bool IsHoldingItem()
    {
        return heldRb != null;
    }

    #endregion

    #region Pick System

    void TryPickItem()
    {
        // Check if inventory is full
        if (inventory.Count >= maxItems)
        {
            Debug.Log("Inventory is full!");
            return;
        }

        // Raycast from screen center
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            Pickable pickableItem = hit.collider.GetComponent<Pickable>();
            if (pickableItem != null)
            {
                PickUpItem(pickableItem);
            }
        }
    }

    void PickUpItem(Pickable item)
    {
        // Add to inventory
        inventory.Add(item);

        // Hide the original object
        item.gameObject.SetActive(false);

        // Equip the item if it's the first one
        if (inventory.Count == 1)
        {
            currentItemIndex = 0;
            EquipItem(0);

            // Switch to Pick mode when picking up first item
            SwitchToMode(InteractionMode.Pick);
        }
    }

    public void SwitchToNextItem()
    {
        if (inventory.Count == 0)
        {
            // No items, go to empty hand
            SetEmptyHand();
            return;
        }

        // If only one item, go to empty hand when pressing R
        if (inventory.Count == 1)
        {
            SetEmptyHand();
            return;
        }

        currentItemIndex = (currentItemIndex + 1) % inventory.Count;

        // If we looped back to the first item from the last, go to empty hand
        if (currentItemIndex == 0)
        {
            SetEmptyHand();
        }
        else
        {
            EquipItem(currentItemIndex);
        }
    }

    public void SwitchToPreviousItem()
    {
        if (inventory.Count == 0)
        {
            // No items, go to empty hand
            SetEmptyHand();
            return;
        }

        currentItemIndex--;
        if (currentItemIndex < 0)
        {
            // If we went below 0, go to empty hand instead of wrapping to last item
            SetEmptyHand();
        }
        else
        {
            EquipItem(currentItemIndex);
        }
    }

    void EquipItem(int index)
    {
        if (index < 0 || index >= inventory.Count)
        {
            Debug.LogWarning(
                $"EquipItem: Invalid index {index}, inventory count: {inventory.Count}"
            );
            return;
        }

        // Remove current equipped object
        if (currentEquippedObject != null)
        {
            // Check if equipped object has magnetic gun functionality and disable it
            var magneticGun = currentEquippedObject.GetComponent<MagneticGun>();
            if (magneticGun != null)
            {
                magneticGun.OnUnequipped();
            }

            Debug.Log("Destroying previous equipped object");
            Destroy(currentEquippedObject);
        }

        // Create new equipped object
        Pickable itemToEquip = inventory[index];
        Debug.Log($"Trying to equip: {itemToEquip.itemName}");

        if (handPoint == null)
        {
            Debug.LogError(
                "HandPoint is null! Please assign HandPoint in PlayerController script."
            );
            return;
        }

        if (itemToEquip.equippedPrefab != null)
        {
            Debug.Log($"Creating equipped object at HandPoint position: {handPoint.position}");
            currentEquippedObject = Instantiate(itemToEquip.equippedPrefab, handPoint);
            currentEquippedObject.transform.localPosition = Vector3.zero;
            currentEquippedObject.transform.localRotation = Quaternion.identity;
            Debug.Log(
                $"Successfully equipped: {itemToEquip.itemName} at {currentEquippedObject.transform.position}"
            );

            // Debug: report the equipped prefab and MagneticGun presence
            var gunsFound = currentEquippedObject.GetComponentsInChildren<MagneticGun>(true);
            Debug.Log($"Equipped object '{currentEquippedObject.name}' has {gunsFound.Length} MagneticGun component(s)");
            if (gunsFound.Length == 0)
            {
                var comps = currentEquippedObject.GetComponents<Component>();
                string compNames = string.Join(", ", System.Array.ConvertAll(comps, c => c != null ? c.GetType().Name : "<null>"));
                Debug.Log($"Root components on equipped object: [{compNames}]");
            }

            // Check if equipped object has magnetic gun functionality
            var magneticGun = currentEquippedObject.GetComponentInChildren<MagneticGun>(true);
            if (magneticGun != null)
            {
                Debug.Log($"PlayerController: Found MagneticGun component on {itemToEquip.itemName}");
                magneticGun.OnEquipped();
            }
            else
            {
                Debug.LogWarning($"PlayerController: No MagneticGun component found on {itemToEquip.itemName} prefab!");
            }
        }
        else
        {
            Debug.LogError($"EquippedPrefab is null for item: {itemToEquip.itemName}");
        }
    }

    void SetEmptyHand()
    {
        // Remove current equipped object
        if (currentEquippedObject != null)
        {
            // Check if equipped object has magnetic gun functionality and disable it
            var magneticGun = currentEquippedObject.GetComponentInChildren<MagneticGun>(true);
            if (magneticGun != null)
            {
                magneticGun.OnUnequipped();
            }

            Debug.Log("Setting empty hand - destroying equipped object");
            Destroy(currentEquippedObject);
            currentEquippedObject = null;
        }

        currentItemIndex = -1; // -1 represents empty hand
        Debug.Log("Set to empty hand state");

        // Switch to empty hand mode
        if (currentMode != InteractionMode.EmptyHand)
        {
            SwitchToMode(InteractionMode.EmptyHand);
        }
    }

    // Method to equip first item when switching from empty hand
    public void EquipFirstItem()
    {
        if (inventory.Count > 0)
        {
            currentItemIndex = 0;
            EquipItem(0);
        }
    }

    // Method to equip last item when switching from empty hand
    public void EquipLastItem()
    {
        if (inventory.Count > 0)
        {
            currentItemIndex = inventory.Count - 1;
            EquipItem(inventory.Count - 1);
        }
    }

    // Public methods for other systems to check state
    public bool CanPickItem()
    {
        if (inventory.Count >= maxItems)
            return false;
        if (cam == null)
            return false;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            return hit.collider.GetComponent<Pickable>() != null;
        }
        return false;
    }

    public Pickable GetCurrentEquippedItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
            return inventory[currentItemIndex];
        return null;
    }

    public int GetInventoryCount()
    {
        return inventory.Count;
    }

    /// Hide the currently equipped item (when switching to Grab mode)
    public void HideCurrentEquippedItem()
    {
        if (currentEquippedObject != null)
        {
            // Check if equipped object has magnetic gun functionality and disable it
            var magneticGun = currentEquippedObject.GetComponentInChildren<MagneticGun>(true);
            if (magneticGun != null)
            {
                magneticGun.OnUnequipped();
            }

            currentEquippedObject.SetActive(false);
            Debug.Log("Hidden equipped item");
        }
    }

    // Show the currently equipped item (when switching to Pick mode)
    public void ShowCurrentEquippedItem()
    {
        if (currentEquippedObject != null)
        {
            currentEquippedObject.SetActive(true);

            // Check if equipped object has magnetic gun functionality and enable it
            var magneticGun = currentEquippedObject.GetComponentInChildren<MagneticGun>(true);
            if (magneticGun != null)
            {
                magneticGun.OnEquipped();
            }

            Debug.Log("Showing equipped item");
        }
        else if (inventory.Count > 0 && currentItemIndex >= 0)
        {
            // If no item is currently equipped but we have items in inventory, equip the current one
            EquipItem(currentItemIndex);
        }
    }

    #endregion

    #region Public API (for backward compatibility)

    public bool IsEmptyHandMode()
    {
        return currentMode == InteractionMode.EmptyHand;
    }

    public bool IsGrabMode()
    {
        return currentMode == InteractionMode.Grab;
    }

    public bool IsPickMode()
    {
        return currentMode == InteractionMode.Pick;
    }

    public InteractionMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Public method to manually reset UI position
    /// Call this if UI positioning issues occur
    /// </summary>
    public void ResetUIPosition()
    {
        SetupUI();
        Debug.Log("UI position manually reset");
    }

    /// <summary>
    /// Remove the currently equipped item from inventory
    /// Used by PedestalInteraction when placing items on pedestals
    /// </summary>
    public void RemoveCurrentEquippedItem()
    {
        if (currentItemIndex >= 0 && currentItemIndex < inventory.Count)
        {
            // Hide the equipped item
            HideCurrentEquippedItem();

            // Remove from inventory
            inventory.RemoveAt(currentItemIndex);

            // Update current item index
            if (inventory.Count == 0)
            {
                currentItemIndex = -1;
                SwitchToMode(InteractionMode.EmptyHand);
            }
            else if (currentItemIndex >= inventory.Count)
            {
                currentItemIndex = inventory.Count - 1;
                EquipItem(currentItemIndex);
            }
            else
            {
                EquipItem(currentItemIndex);
            }
        }
    }

    /// <summary>
    /// Disable player input (for death sequence, etc.)
    /// </summary>
    public void DisableInput()
    {
        inputEnabled = false;
        Debug.Log("Player input disabled");
    }

    /// <summary>
    /// Enable player input
    /// </summary>
    public void EnableInput()
    {
        inputEnabled = true;
        Debug.Log("Player input enabled");
    }

    #endregion
}
