using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class ShowTextOnTriggerTMP : MonoBehaviour
{
    public TMP_Text targetText;           // 指向 Canvas 上的 TextMeshProUGUI
    public string textToShow = "Thanks for playing";
    
    [Tooltip("If false the trigger ignores collisions until ActivateEnd() is called.")]
    public bool enabledOnStart = false;

    

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    bool isEnabledForGame = false;

    void Start()
    {
        isEnabledForGame = enabledOnStart;
        Debug.Log($"[ShowTextOnTrigger] Start: '{name}' enabledOnStart={enabledOnStart} targetTextAssigned={(targetText!=null)}");
        if (targetText != null)
            targetText.gameObject.SetActive(enabledOnStart);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ShowTextOnTrigger] OnTriggerEnter: '{name}' hit by '{other.gameObject.name}' (enabled={isEnabledForGame})");

        // detect player: allow if object has a CharacterController OR is tagged as Player
        bool isPlayer = other.GetComponent<CharacterController>() != null || other.CompareTag("Player");
        if (!isPlayer)
        {
            Debug.Log($"[ShowTextOnTrigger] Ignored trigger: '{other.gameObject.name}' is not recognized as player (hasCC={other.GetComponent<CharacterController>()!=null}, tag={other.tag})");
            return;
        }

        // If the trigger is currently disabled, auto-activate on first player entry
        if (!isEnabledForGame)
        {
            Debug.Log($"[ShowTextOnTrigger] Auto-activating on first player entry for '{name}'");
            ActivateEnd();
        }

        if (targetText != null)
        {
            Debug.Log($"[ShowTextOnTrigger] Showing text on '{name}': {textToShow}");
            targetText.text = textToShow;
            targetText.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log($"[ShowTextOnTrigger] Warning: targetText is null on '{name}'");
        }
    }

    /// <summary>
    /// Activate the End trigger so it can respond to the player.
    /// </summary>
    public void ActivateEnd()
    {
        isEnabledForGame = true;
        Debug.Log($"[ShowTextOnTrigger] ActivateEnd() called on '{name}'");
    }

    /// <summary>
    /// Deactivate the End trigger so it will ignore collisions.
    /// </summary>
    public void DeactivateEnd()
    {
        isEnabledForGame = false;
        Debug.Log($"[ShowTextOnTrigger] DeactivateEnd() called on '{name}'");
    }
    
}