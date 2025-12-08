using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("Respawn point for this specific fire pit (optional - if not set, uses DeathManager's default)")]
    public Transform customRespawnPoint;

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered (by CharacterController component)
        if (other.GetComponent<CharacterController>())
        {
            // Trigger death manager
            if (DeathManager.Instance != null)
            {
                // Use custom respawn point if set, otherwise use default
                Transform respawnPoint = customRespawnPoint != null ? customRespawnPoint : DeathManager.Instance.respawnPoint;
                DeathManager.Instance.TriggerDeath(respawnPoint);
            }
            else
            {
                Debug.LogError("DeathManager.Instance is null! Make sure DeathManager exists in the scene.");
            }
        }
    }
}
