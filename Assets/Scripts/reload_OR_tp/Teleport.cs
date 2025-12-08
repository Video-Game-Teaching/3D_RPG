using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform targetLocation;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>())
        {
            TeleportPlayer(targetLocation);
        }
    }

    /// <summary>
    /// Teleport player to target location (public static method for reuse)
    /// </summary>
    public static void TeleportPlayer(Transform targetLocation)
    {
        if (targetLocation == null)
        {
            Debug.LogWarning("Target location is null!");
            return;
        }

        CharacterController playerController = FindObjectOfType<CharacterController>();
        if (playerController != null)
        {
            // IMPORTANT: Must disable CharacterController before changing position
            playerController.enabled = false;
            playerController.transform.position = targetLocation.position;
            playerController.enabled = true;
            
            Debug.Log($"Player teleported to {targetLocation.position}");
        }
        else
        {
            Debug.LogWarning("Could not find player CharacterController!");
        }
    }

}
