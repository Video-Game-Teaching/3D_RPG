using UnityEngine;
using System.Collections.Generic;

// Very small, easy-to-read Door script.
// - Caches player transform once.
// - Uses squared distance for efficiency.
// - Simple green check: green component must exceed red and blue by a tolerance.
public class Door : MonoBehaviour
{
    public Animator animator;                // assign in Inspector or leave null to auto-find
    public Renderer colorRenderer;           // assign renderer that has the door material
    public float openDistance = 3f;          // open when player is within this distance
    public float colorTolerance = 0.2f;      // how much greener the G channel must be
    public bool treatMissingAsGreen = false; // if true, missing renderer/material will be treated as green (allow open)

    public Transform player;
    float openDistanceSqr;
    // Optional: set in Inspector the pedestals that must be activated for this door to open.
    // If empty, no pedestal requirement is enforced.
    public List<Pedestal> requiredPedestals = new List<Pedestal>();
    // material property block for non-destructive color changes
    MaterialPropertyBlock mpb;

    [Header("Dialogue Settings")]
    public int allPedestalsCompleteDialogueIndex = 4;  // Dialogue group to show when all pedestals are activated
    public int doorLockedWarningDialogueIndex = 5;     // Dialogue group to show when player approaches locked door

    public float dialogueTriggerDistance = 2f;         // Distance to trigger door locked warning

    private bool hasShownCompleteDialogue = false;     // Flag to ensure we only show completion dialogue once
    private bool hasShownWarningDialogue = false;      // Flag to ensure we only show warning dialogue once

    private bool wasAllActivatedLastFrame = false;     // Track state changes

    void Start()
    {
        openDistanceSqr = openDistance * openDistance;

        // subscribe to pedestal events so we update instantly when pedestal states change
        if (requiredPedestals != null)
        {
            foreach (var p in requiredPedestals)
            {
                if (p == null) continue;
                p.OnPedestalActivated += OnPedestalChanged;
                p.OnItemPlaced += OnPedestalChanged;
                p.OnItemRemoved += OnPedestalChanged;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // squared distance is faster than Vector3.Distance
        float dsq = (player.position - transform.position).sqrMagnitude;
        // decide whether pedestals allow opening
        bool pedActive = CheckAllPedestalsActivated();

        // Handle dialogue triggers
        HandleDialogueTriggers(dsq, pedActive);

        // update visual color: green when all conditions are met, red otherwise
        UpdateVisual(pedActive);

        if (dsq <= openDistanceSqr && pedActive)
        {
            if (animator != null) animator.SetTrigger("Open");
        }

        // Update state tracking
        wasAllActivatedLastFrame = pedActive;
    }

    void HandleDialogueTriggers(float distanceSqr, bool allPedestalsActivated)
    {
        // Check if DialogueManager exists
        if (DialogueManager.Instance == null) return;

        // Trigger "All Pedestals Complete" dialogue when all pedestals just became activated
        // Only trigger if index is valid (>= 0)
        if (allPedestalsCompleteDialogueIndex >= 0 && allPedestalsActivated && !wasAllActivatedLastFrame && !hasShownCompleteDialogue)
        {
            hasShownCompleteDialogue = true;
            DialogueManager.Instance.ShowDialogueGroup(allPedestalsCompleteDialogueIndex);
            Debug.Log("Door: All pedestals activated! Showing completion dialogue.");
        }

        // Trigger "Door Locked Warning" when player approaches but door is locked
        // Only trigger if index is valid (>= 0)
        if (doorLockedWarningDialogueIndex >= 0)
        {
            float dialogueDistSqr = dialogueTriggerDistance * dialogueTriggerDistance;
            if (distanceSqr <= dialogueDistSqr && !allPedestalsActivated && !hasShownWarningDialogue)
            {
                hasShownWarningDialogue = true;
                DialogueManager.Instance.ShowDialogueGroup(doorLockedWarningDialogueIndex);
                Debug.Log("Door: Player approached locked door. Showing warning dialogue.");
            }

            // Reset warning dialogue flag when player moves away (allow re-trigger)
            if (distanceSqr > dialogueDistSqr)
            {
                hasShownWarningDialogue = false;
            }
        }
    }

    // Called when any subscribed pedestal changes state
    void OnPedestalChanged(Pickable ignored)
    {
        // no-op body: presence of this callback triggers runtime update via Update()
        // (keeps changes minimal). Could call CheckAllPedestalsActivated() here
        // to do immediate action if desired.
    }

    void OnPedestalChanged(Pedestal ignored)
    {
        // same as above for the other event signature
    }

    // Return true if there are no required pedestals or all listed pedestals are activated
    bool CheckAllPedestalsActivated()
    {
        if (requiredPedestals == null || requiredPedestals.Count == 0)
            return true;

        foreach (var p in requiredPedestals)
        {
            if (p == null) return false;
            if (!p.IsActivated()) return false;
        }
        return true;
    }

    void OnDestroy()
    {
        // unsubscribe to avoid dangling delegates
        if (requiredPedestals == null) return;
        foreach (var p in requiredPedestals)
        {
            if (p == null) continue;
            p.OnPedestalActivated -= OnPedestalChanged;
            p.OnItemPlaced -= OnPedestalChanged;
            p.OnItemRemoved -= OnPedestalChanged;
        }
    }

    bool IsGreen()
    {
        // Prefer the renderer material color if available
        if (colorRenderer != null && colorRenderer.sharedMaterial != null)
        {
            return IsColorGreenByTolerance(colorRenderer.sharedMaterial.color);
        }

        // Fallback: if no renderer, use treatMissingAsGreen
        return treatMissingAsGreen;
    }

    void UpdateVisual(bool canOpen)
    {
        if (colorRenderer == null)
            return;

        if (mpb == null) mpb = new MaterialPropertyBlock();
        colorRenderer.GetPropertyBlock(mpb);

        Color outColor = canOpen ? Color.green : Color.red;
        mpb.SetColor("_Color", outColor);
        // if material uses emission, set emission color too
        if (colorRenderer.sharedMaterial != null && colorRenderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
        {
            mpb.SetColor("_EmissionColor", outColor * (canOpen ? 1f : 0.2f));
        }

        colorRenderer.SetPropertyBlock(mpb);
    }

    bool IsColorGreenByTolerance(Color c)
    {
        float sum = c.r + c.g + c.b;
        if (sum <= 1e-5f) return treatMissingAsGreen;
        float nr = c.r / sum;
        float ng = c.g / sum;
        float nb = c.b / sum;
        float otherMax = Mathf.Max(nr, nb);
        return (ng - otherMax) >= colorTolerance;
    }

}
