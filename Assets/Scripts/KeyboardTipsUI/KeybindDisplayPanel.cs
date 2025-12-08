using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Dynamic keybind display panel.
/// Automatically updates the display based on player state, level, and equipment.
/// </summary>
public class KeybindDisplayPanel : MonoBehaviour
{
    [System.Serializable]
    public class KeybindInfo
    {
        public string actionName;
        public string keyDisplay;
        public KeybindCategory category;
        public Color displayColor = Color.white;

        [HideInInspector] public bool isAvailable = false;
        [HideInInspector] public bool isLocked = false;
        [HideInInspector] public bool isEnabledInCurrentLevel = true;
    }

    public enum KeybindCategory
    {
        Movement,
        Interaction,
        MagneticGun,
        SpecialAbility,
        Dialogue,
        ItemManagement
    }

    [Header("UI References")]
    public GameObject keybindItemPrefab;
    public Transform contentContainer;          // Container for keybind items
    public CanvasGroup panelCanvasGroup;        // for showing/hiding the panel

    [Header("Display Settings")]
    public bool showOnlyAvailable = false;
    public bool groupByCategory = true;
    public float updateInterval = 0.2f;

    [Header("Colors")]
    public Color availableColor = Color.green;
    public Color lockedColor = Color.gray;
    public Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    // Internal state
    private List<KeybindInfo> allKeybinds = new List<KeybindInfo>();
    private Dictionary<KeybindInfo, GameObject> keybindUIObjects = new Dictionary<KeybindInfo, GameObject>();
    private float lastUpdateTime = 0f;
    private PlayerController playerController;
    private MagneticGun magneticGun;
    private Scene1_Gravity gravityController;
    private string currentSceneName;

    void Start()
    {
        currentSceneName = SceneManager.GetActiveScene().name;
        InitializeKeybinds();
        UpdateLevelSpecificKeybinds();
        CreateUIElements();
        UpdateDisplay();
    }

    void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateKeybindAvailability();
            UpdateDisplay();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Update which keybinds are enabled based on the current level
    /// </summary>
    void UpdateLevelSpecificKeybinds()
    {
        foreach (var keybind in allKeybinds)
        {
            keybind.isEnabledInCurrentLevel = IsKeybindEnabledInLevel(keybind.actionName, currentSceneName);
        }
    }

    /// <summary>
    /// Determine if a specific keybind should be shown in the current level
    /// </summary>
    bool IsKeybindEnabledInLevel(string actionName, string sceneName)
    {
        // Level 1 (Level1_demo) - Grab large cubes with Mouse Button 1
        if (sceneName == "Level1_demo")
        {
            switch (actionName)
            {
                case ": Grab Objects(big)":        // Mouse Button 1 - grab cubes
                case ": Place Object":              // Mouse Button 2 - place/drop cubes
                    return true;
                case ": Pick Up Items(small)":      // F key - not used in Level 1
                case ": Interact with Pedestals":   // E key - no pedestals in Level 1
                case ": Red Mode":
                case ": Blue Mode":
                case ": Magnetic Lock":
                case ": Next Item":
                case ": Previous Item":
                    return false;
                default:
                    return false;
            }
        }

        // Level 2 (Level2_demo) - Magnetic Gun mechanics
        if (sceneName == "Level2_demo")
        {
            switch (actionName)
            {
                case ": Pick Up Items(small)":      // F key - pick up magnetic gun
                case ": Magnetic Lock":             // Mouse Button 1 - lock/pull magnetic objects
                case ": Next Item":                 // R key - switch to empty hand / re-equip gun
                case ": Previous Item":             // T key - switch to empty hand / re-equip gun
                    return true;
                case ": Red Mode":                  // Q key - NOT in Level 2
                case ": Blue Mode":                 // E key - NOT in Level 2
                case ": Grab Objects(big)":         // Mouse Button 1 grab - NOT used in Level 2
                case ": Place Object":              // Mouse Button 2 - NOT used in Level 2
                case ": Interact with Pedestals":   // E key pedestals - NOT used in Level 2
                    return false;
                default:
                    return false;
            }
        }

        // Level 3 (Level3) - Pick small items (keys) with F, interact with pedestals with E
        if (sceneName == "Level3")
        {
            switch (actionName)
            {
                case ": Pick Up Items(small)":      // F key - pick up keys
                case ": Interact with Pedestals":   // E key - place keys on pedestals
                case ": Next Item":                 // R key - switch between keys
                case ": Previous Item":             // T key - switch between keys
                    return true;
                case ": Place Object":              // Mouse Button 2 - only works for Grab mode, NOT Pick mode
                case ": Grab Objects(big)":         // Mouse Button 1 - NO large grabbables in Level 3
                case ": Red Mode":                  // No magnetic gun in Level 3
                case ": Blue Mode":
                case ": Magnetic Lock":
                    return false;
                default:
                    return false;
            }
        }

        // Level 4 (Level4_demo) - Pure platforming, no magnetic gun or grabbing
        if (sceneName == "Level4_demo")
        {
            // No special keybinds needed in Level 4
            return false;
        }

        // Level 5 (Level5_demo) - Magnetic puzzle with maze
        if (sceneName == "Level5_demo")
        {
            switch (actionName)
            {
                case ": Red Mode":
                case ": Blue Mode":
                case ": Magnetic Lock":
                    return true;
                case ": Pick Up Items(small)":
                case ": Grab Objects(big)":
                case ": Place Object":
                case ": Interact with Pedestals":
                case ": Next Item":
                case ": Previous Item":
                    return false; // Not needed in Level 5
                default:
                    return false;
            }
        }

        // Level 6 (Level6) - Minimal mechanics
        if (sceneName == "Level6")
        {
            // No special keybinds needed in Level 6
            return false;
        }

        // Default for other scenes (testing scenes, etc.) - show all keybinds
        return true;
    }

    void InitializeKeybinds()
    {
        allKeybinds.Clear();

        // -----------------Interaction
        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Pick Up Items(small)",
            keyDisplay = "F",
            category = KeybindCategory.Interaction
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Grab Objects(big)",
            keyDisplay = "Mouse Button 1",
            category = KeybindCategory.Interaction
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Place Object",
            keyDisplay = "Mouse Button 2",
            category = KeybindCategory.Interaction
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Interact with Pedestals",
            keyDisplay = "E",
            category = KeybindCategory.Interaction
        });

        // ------------------Item Management
        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Next Item",
            keyDisplay = "R",
            category = KeybindCategory.ItemManagement
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Previous Item",
            keyDisplay = "T",
            category = KeybindCategory.ItemManagement
        });

        // -------------Magnetic Gun
        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Red Mode",
            keyDisplay = "Q",
            category = KeybindCategory.MagneticGun
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Blue Mode",
            keyDisplay = "E",
            category = KeybindCategory.MagneticGun
        });

        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Magnetic Lock",
            keyDisplay = "Mouse Button 1",
            category = KeybindCategory.MagneticGun
        });

        // --------------------Special Abilities
        allKeybinds.Add(new KeybindInfo
        {
            actionName = ": Gravity Direction",
            keyDisplay = "1",
            category = KeybindCategory.SpecialAbility
        });
    }

    /// <summary>
    /// Update the availability status of all keybinds based on player state
    /// </summary>
    void UpdateKeybindAvailability()
    {
        if (playerController == null)
            playerController = PlayerController.Instance;

        foreach (var keybind in allKeybinds)
        {
            UpdateSingleKeybindAvailability(keybind);
        }
    }

    /// <summary>
    /// Update the availability status of a single keybind
    /// </summary>
    void UpdateSingleKeybindAvailability(KeybindInfo keybind)
    {
        keybind.isAvailable = false;
        keybind.isLocked = false;

        if (playerController == null)
            return;

        switch (keybind.actionName)
        {
            //---------------Interaction
            case ": Pick Up Items(small)":
                keybind.isAvailable = playerController.IsEmptyHandMode() ||
                                     playerController.IsPickMode();
                break;

            case ": Grab Objects(big)":
                keybind.isAvailable = playerController.IsEmptyHandMode();
                break;

            case ": Place Object":
                keybind.isAvailable = playerController.IsGrabMode() ||
                                     playerController.IsPickMode();
                break;

            case ": Interact with Pedestals":
                // 检查是否看向Pedestal（简化检查）
                keybind.isAvailable = playerController.IsPickMode();
                break;

            //---------------Item Management
            case ": Next Item":
            case ": Previous Item":
                // Show when player has items in inventory (can switch between equipped/empty hand)
                keybind.isAvailable = playerController.GetInventoryCount() > 0;
                break;

            //---------------Magnetic Gun
            case ": Red Mode":
            case ": Blue Mode":
            case ": Magnetic Lock":
                if (magneticGun == null)
                    magneticGun = FindObjectOfType<MagneticGun>();

                if (magneticGun != null)
                {
                    keybind.isAvailable = playerController.IsPickMode();

                    // Check unlock conditions for mode switching
                    if (keybind.actionName == ": Red Mode")
                    {
                        // Red mode is locked when lockBlueOnly is enabled
                        keybind.isLocked = magneticGun.lockBlueOnly;
                        keybind.isAvailable = keybind.isAvailable && !keybind.isLocked;
                    }
                    else if (keybind.actionName == ": Blue Mode")
                    {
                        // Blue mode is always available when magnetic gun is equipped
                        keybind.isLocked = false;
                    }
                }
                break;

            //---------------Special Abilities
            case ": Gravity Direction":
                if (gravityController == null)
                    gravityController = FindObjectOfType<Scene1_Gravity>();

                keybind.isAvailable = gravityController != null;
                break;
        }
    }


    /// </summary>
    void CreateUIElements()
    {
        if (keybindItemPrefab == null || contentContainer == null)
        {
            return;
        }

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }
        keybindUIObjects.Clear();

        // create UI items for each keybind
        foreach (var keybind in allKeybinds)
        {
            GameObject uiItem = Instantiate(keybindItemPrefab, contentContainer);
            keybindUIObjects[keybind] = uiItem;

            TextMeshProUGUI[] texts = uiItem.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 2)
            {
                texts[0].text = keybind.keyDisplay;
                texts[1].text = keybind.actionName;
            }
        }
    }

    void UpdateDisplay()
    {
        foreach (var kvp in keybindUIObjects)
        {
            KeybindInfo keybind = kvp.Key;
            GameObject uiObject = kvp.Value;

            // Hide keybinds not enabled in current level
            if (!keybind.isEnabledInCurrentLevel)
            {
                uiObject.SetActive(false);
                continue;
            }

            bool shouldShow = !showOnlyAvailable || keybind.isAvailable;
            uiObject.SetActive(shouldShow);

            if (!shouldShow)
                continue;

            // color & text update
            Color targetColor = availableColor;
            if (keybind.isLocked)
                targetColor = lockedColor;
            else if (!keybind.isAvailable)
                targetColor = unavailableColor;

            TextMeshProUGUI[] texts = uiObject.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                text.color = targetColor;
            }
            if (keybind.isLocked && texts.Length >= 2)
            {
                texts[1].text = keybind.actionName + " (Locked) ";
            }
            else if (texts.Length >= 2)
            {
                texts[1].text = keybind.actionName;
            }
        }
    }

    public void TogglePanel()
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = panelCanvasGroup.alpha > 0.5f ? 0f : 1f;
        }
    }

    public void SetPanelVisible(bool visible)
    {
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
