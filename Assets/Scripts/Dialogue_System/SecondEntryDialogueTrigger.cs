using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 只有第二次进入才会触发对话的Trigger
/// </summary>
public class SecondEntryDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public int groupIndex = 0;  // 要显示的对话组索引 (0-based)

    [Header("Trigger Settings")]
    public string playerLayer = "Player"; // 用于识别玩家的层级

    [Header("Dialogue End Callbacks")]
    public UnityEvent onDialogueEnd;  // 对话结束时触发的事件

    private int entryCount = 0;  // 记录玩家进入的次数
    private bool hasTriggered = false;  // 是否已经触发过对话
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
        // 检查是否是玩家
        if (other.gameObject.layer != LayerMask.NameToLayer(playerLayer))
            return;

        // 已经触发过就不再触发
        if (hasTriggered)
        {
            Debug.Log($"SecondEntryDialogueTrigger on {gameObject.name}: 对话已触发过，跳过。");
            return;
        }

        // 增加进入次数
        entryCount++;
        Debug.Log($"SecondEntryDialogueTrigger on {gameObject.name}: 第 {entryCount} 次进入。");

        // 只有第二次进入才触发对话
        if (entryCount == 2)
        {
            TriggerDialogue();
        }
    }

    private void TriggerDialogue()
    {
        // check if DialogueManager exists
        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"SecondEntryDialogueTrigger on {gameObject.name}: DialogueManager.Instance not found!");
            return;
        }

        // check if DialogueUI exists
        if (DialogueManager.Instance.dialogueUI == null)
        {
            Debug.LogError($"SecondEntryDialogueTrigger on {gameObject.name}: DialogueUI not found!");
            return;
        }

        // check if level dialogue groups exist
        if (DialogueManager.Instance.levelDialogueGroups == null || DialogueManager.Instance.levelDialogueGroups.Count == 0)
        {
            Debug.LogError($"SecondEntryDialogueTrigger on {gameObject.name}: DialogueManager.levelDialogueGroups is empty!");
            return;
        }

        // check if group index is valid
        if (groupIndex < 0 || groupIndex >= DialogueManager.Instance.levelDialogueGroups.Count)
        {
            Debug.LogError($"SecondEntryDialogueTrigger on {gameObject.name}: Group index {groupIndex} is out of range (0-{DialogueManager.Instance.levelDialogueGroups.Count - 1})!");
            return;
        }

        // show the dialogue group
        DialogueManager.Instance.ShowDialogueGroup(groupIndex);
        hasTriggered = true;

        // 开始监听对话结束事件
        isWaitingForDialogueEnd = true;

        Debug.Log($"SecondEntryDialogueTrigger on {gameObject.name}: 第二次进入，触发对话组 {groupIndex}。");
    }

    // 重置触发器状态
    public void ResetTrigger()
    {
        entryCount = 0;
        hasTriggered = false;
        Debug.Log($"SecondEntryDialogueTrigger on {gameObject.name}: 触发器已重置。");
    }

    // 获取当前进入次数
    public int GetEntryCount()
    {
        return entryCount;
    }
}
