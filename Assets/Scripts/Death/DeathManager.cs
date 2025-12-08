using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages player death sequence including rotation animation, red fade, and respawn
/// Supports multiple fire pits with different respawn points
/// </summary>
public class DeathManager : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Default respawn point (used if fire pit doesn't specify custom point)")]
    public Transform respawnPoint;

    [Header("Death Animation Settings")]
    [Tooltip("Death rotation animation duration in seconds")]
    public float rotationDuration = 2f;

    [Tooltip("Red fade duration in seconds")]
    public float fadeDuration = 2f;

    [Tooltip("Maximum rotation angle in degrees")]
    public float maxRotationAngle = 90f;

    [Header("UI References")]
    [Tooltip("Death canvas object")]
    public GameObject deathCanvas;

    [Tooltip("Red overlay image")]
    public Image redOverlay;

    [Tooltip("Death UI panel with button")]
    public GameObject deathUIPanel;

    // Singleton
    public static DeathManager Instance { get; private set; }

    private bool isDead = false;
    private GameObject playerObject; // The PLAYER object to rotate
    private Quaternion originalPlayerRotation;
    private Transform currentRespawnPoint; // Store the respawn point for current death

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Initialize: hide death UI
        if (deathCanvas != null)
        {
            deathCanvas.SetActive(false);
        }

        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        // Ensure red overlay is initially transparent
        if (redOverlay != null)
        {
            Color c = redOverlay.color;
            c.a = 0f;
            redOverlay.color = c;
        }

        // Find player object via CharacterController (the root object with CharacterController is the player pivot)
        CharacterController characterController = FindObjectOfType<CharacterController>();
        if (characterController != null)
        {
            playerObject = characterController.gameObject;
            originalPlayerRotation = playerObject.transform.rotation;
            Debug.Log($"DeathManager: Found player object '{playerObject.name}' via CharacterController");
        }
        else
        {
            Debug.LogWarning("DeathManager: Could not find CharacterController! Make sure the player has a CharacterController component.");
        }
    }

    /// <summary>
    /// Trigger death sequence
    /// </summary>
    /// <param name="customRespawnPoint">Custom respawn point (optional - uses default if null)</param>
    public void TriggerDeath(Transform customRespawnPoint = null)
    {
        if (isDead) return; // Prevent duplicate triggers

        isDead = true;
        
        // Store the respawn point for this death
        currentRespawnPoint = customRespawnPoint != null ? customRespawnPoint : respawnPoint;
        
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. disable player input - disable FirstPersonController to stop all movement and camera control
        var firstPersonController = FindObjectOfType<StarterAssets.FirstPersonController>();
        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
            Debug.Log("DeathManager: FirstPersonController disabled");
        }
        
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.DisableInput();
            Debug.Log("DeathManager: Player input disabled");
        }

        // Unlock and show cursor immediately (like pressing ESC)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. show death canvas (but UI panel is hidden for now)
        if (deathCanvas != null)
        {
            deathCanvas.SetActive(true);
        }

        // 3. rotate the PLAYER object and fade to red simultaneously
        float elapsed = 0f;
        float maxDuration = Mathf.Max(rotationDuration, fadeDuration);

        Quaternion startRotation = playerObject != null ? playerObject.transform.rotation : Quaternion.identity;
        // Simple fall forward: rotate 90 degrees on X-axis (lying down)
        Quaternion targetRotation = startRotation * Quaternion.Euler(-90f, 0f, 0f);

        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;

            // Slerp rotation animation on PLAYER object
            if (playerObject != null && elapsed < rotationDuration)
            {
                float rotationProgress = elapsed / rotationDuration;
                playerObject.transform.rotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    rotationProgress
                );
            }

            // Red overlay fade animation
            if (redOverlay != null && elapsed < fadeDuration)
            {
                float fadeProgress = elapsed / fadeDuration;
                Color c = redOverlay.color;
                c.a = Mathf.Lerp(0f, 0.5f, fadeProgress); // fade to semi-transparent red
                redOverlay.color = c;
            }

            yield return null;
        }

        // 4. ensure animation completion
        if (playerObject != null)
        {
            playerObject.transform.rotation = targetRotation;
        }

        if (redOverlay != null)
        {
            Color c = redOverlay.color;
            c.a = 0.5f;
            redOverlay.color = c;
        }

        // 5. show death UI panel (respawn button)
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(true);
        }

        // Cursor is already unlocked at the start of death sequence
        // Wait for player to click respawn button (button will call Respawn method)
    }

    public void Respawn()
    {
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        // 1. hide death UI panel
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }

        // 2. teleport player to respawn point (uses the stored custom respawn point)
        if (currentRespawnPoint != null)
        {
            Teleport.TeleportPlayer(currentRespawnPoint);
            Debug.Log("DeathManager: Player teleported to respawn point");
        }
        else
        {
            Debug.LogWarning("Respawn point is not set!");
        }

        // 3. reset PLAYER rotation
        if (playerObject != null)
        {
            playerObject.transform.rotation = originalPlayerRotation;
            Debug.Log("DeathManager: Player rotation reset");
        }

        // 4. fade out red overlay
        float elapsed = 0f;
        float fadeOutDuration = 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (redOverlay != null)
            {
                Color c = redOverlay.color;
                c.a = Mathf.Lerp(0.5f, 0f, elapsed / fadeOutDuration);
                redOverlay.color = c;
            }
            yield return null;
        }

        // 5. ensure fully transparent
        if (redOverlay != null)
        {
            Color c = redOverlay.color;
            c.a = 0f;
            redOverlay.color = c;
        }

        // 6. hide death canvas
        if (deathCanvas != null)
        {
            deathCanvas.SetActive(false);
        }

        // 7. re-enable player input
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.EnableInput();
            Debug.Log("DeathManager: Player input re-enabled");
        }

        // Re-enable FirstPersonController to restore movement and camera control
        var firstPersonController = FindObjectOfType<StarterAssets.FirstPersonController>();
        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
            Debug.Log("DeathManager: FirstPersonController re-enabled");
        }

        // 8. lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 9. reset death state
        isDead = false;
    }
}
