using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueLineTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public int groupIndexOnEnter = 0;  // which dialogue group to show on enter (0-based index)
    public int groupIndexOnExit = 1;   // which dialogue group to show on exit (0-based index)

    [Header("Trigger Settings")]
    public bool triggerOnEnter = true;  // trigger when player enters
    public bool triggerOnExit = true;   // trigger when player exits
    public bool oneTimeOnlyEnter = true;     // only trigger enter once
    public bool oneTimeOnlyExit = true;      // only trigger exit once
    public string playerLayer = "Player"; // layer to identify player

    [Header("Dialogue End Callbacks")]
    public UnityEvent onDialogueEnd;  // 对话结束时触发的事件

    private bool hasTriggeredEnter = false;
    private bool hasTriggeredExit = false;
    private bool isWaitingForDialogueEnd = false;

    private void Update()
    {
        // 检测对话是否结束
        if (isWaitingForDialogueEnd && DialogueManager.Instance != null)
        {
            if (!DialogueManager.Instance.IsDialogueActive())
            {
                isWaitingForDialogueEnd = false;
                onDialogueEnd?.Invoke();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;

        if (other.gameObject.layer == LayerMask.NameToLayer(playerLayer))
        {
            TriggerDialogueGroup(groupIndexOnEnter, "Enter", ref hasTriggeredEnter, oneTimeOnlyEnter);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerOnExit) return;

        if (other.gameObject.layer == LayerMask.NameToLayer(playerLayer))
        {
            TriggerDialogueGroup(groupIndexOnExit, "Exit", ref hasTriggeredExit, oneTimeOnlyExit);
        }
    }

    private void TriggerDialogueGroup(int groupIndex, string triggerType, ref bool hasTriggered, bool oneTimeOnly)
    {
        // check if already triggered
        if (oneTimeOnly && hasTriggered)
        {
            Debug.Log($"DialogueLineTrigger on {gameObject.name}: {triggerType} already triggered, skipping.");
            return;
        }

        // check if DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"DialogueLineTrigger on {gameObject.name}: DialogueManager.Instance not found!");
            return;
        }

        // check if DialogueUI exists
        if (DialogueManager.Instance.dialogueUI == null)
        {
            Debug.LogError($"DialogueLineTrigger on {gameObject.name}: DialogueUI not found!");
            return;
        }

        // check if level dialogue groups exist
        if (DialogueManager.Instance.levelDialogueGroups == null || DialogueManager.Instance.levelDialogueGroups.Count == 0)
        {
            Debug.LogError($"DialogueLineTrigger on {gameObject.name}: DialogueManager.levelDialogueGroups is empty!");
            return;
        }

        // check if group index is valid
        if (groupIndex < 0 || groupIndex >= DialogueManager.Instance.levelDialogueGroups.Count)
        {
            Debug.LogError($"DialogueLineTrigger on {gameObject.name}: Group index {groupIndex} is out of range (0-{DialogueManager.Instance.levelDialogueGroups.Count - 1})!");
            return;
        }

        // show the dialogue group (supports multiple pages)
        DialogueManager.Instance.ShowDialogueGroup(groupIndex);

        hasTriggered = true;

        // 开始监听对话结束事件
        isWaitingForDialogueEnd = true;
    }

    // Optional: reset triggers (can be called from other scripts)
    public void ResetTriggers()
    {
        hasTriggeredEnter = false;
        hasTriggeredExit = false;
    }

    public void ResetEnterTrigger()
    {
        hasTriggeredEnter = false;
    }

    public void ResetExitTrigger()
    {
        hasTriggeredExit = false;
    }
}

