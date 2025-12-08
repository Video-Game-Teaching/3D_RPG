using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelDialoguePage
{
    public string speakerName = "Speaker";
    [TextArea(3, 10)]
    public string dialogueText;
}

[System.Serializable]
public class LevelDialogueGroup
{
    public string groupName = "Dialogue Group";  // For organization in Inspector
    public List<LevelDialoguePage> pages = new List<LevelDialoguePage>();
}

public class DialogueManager : MonoBehaviour
{
    public DialogueUI dialogueUI;
    
    [Header("Level Dialogue Data")]
    public List<LevelDialogueGroup> levelDialogueGroups = new List<LevelDialogueGroup>();  // Dialogue groups for this level

    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    private float originalTimeScale;
    
    // For multi-page level dialogues
    private List<LevelDialoguePage> currentLevelPages;
    private int currentLevelPageIndex = 0;
    private bool isLevelDialogueActive = false;

    public static DialogueManager Instance;

    void Awake()
    {
        Instance = this;

        // Reset dialogue state on scene load to ensure clean state
        isDialogueActive = false;
        isLevelDialogueActive = false;
        currentDialogue = null;
        currentLevelPages = null;
        currentLineIndex = 0;
        currentLevelPageIndex = 0;
    }

    void OnEnable()
    {
        // Re-register instance when enabled (handles scene reload)
        Instance = this;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive || isLevelDialogueActive;
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (isDialogueActive)
        {
            Debug.LogWarning("DialogueManager: Dialogue already in progress");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        // pause game time
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // show dialogue
        dialogueUI.ShowDialogue();
        DisplayCurrentLine();
    }


    private void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.dialogueLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];

        dialogueUI.SetDialogueLine(currentDialogue.speakerName, currentLine.dialogueText);
    }

    // enter next line
    public void NextLine()
    {
        // Handle level dialogue pages
        if (isLevelDialogueActive)
        {
            currentLevelPageIndex++;
            
            if (currentLevelPageIndex >= currentLevelPages.Count)
            {
                EndLevelDialogue();
            }
            else
            {
                DisplayCurrentLevelPage();
            }
            return;
        }

        // Handle normal DialogueData
        if (!isDialogueActive || currentDialogue == null)
        {
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.dialogueLines.Count)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentLine();
        }
    }


    public void EndDialogue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        isDialogueActive = false;
        currentDialogue = null;
        currentLineIndex = 0;

        // resume game time
        Time.timeScale = originalTimeScale;

        // hide dialogue
        dialogueUI.HideDialogue();
    }

    // Show a single line of dialogue without entering dialogue mode
    public void ShowSingleLine(string speakerName, string text)
    {
        if (dialogueUI == null)
        {
            return;
        }

        dialogueUI.ShowDialogue();
        dialogueUI.SetDialogueLine(speakerName, text);
    }

    // Helper method to show a specific line from DialogueData without entering dialogue mode
    public void ShowDialogueLine(DialogueData dialogueData, int lineIndex)
    {
        if (dialogueData == null)
        {
            return;
        }

        if (lineIndex < 0 || lineIndex >= dialogueData.dialogueLines.Count)
        {
            return;
        }

        DialogueLine line = dialogueData.dialogueLines[lineIndex];
        ShowSingleLine(dialogueData.speakerName, line.dialogueText);
    }

    // Show a dialogue group from the level's dialogue data (supports multiple pages)
    public void ShowDialogueGroup(int groupIndex)
    {
        if (levelDialogueGroups == null || levelDialogueGroups.Count == 0)
        {
            Debug.LogError("DialogueManager: levelDialogueGroups is empty!");
            return;
        }

        if (groupIndex < 0 || groupIndex >= levelDialogueGroups.Count)
        {
            Debug.LogError($"DialogueManager: Group index {groupIndex} is out of range (0-{levelDialogueGroups.Count - 1})!");
            return;
        }

        LevelDialogueGroup group = levelDialogueGroups[groupIndex];
        
        if (group.pages == null || group.pages.Count == 0)
        {
            Debug.LogError($"DialogueManager: Dialogue group {groupIndex} has no pages!");
            return;
        }

        // Start multi-page dialogue
        StartLevelDialogue(group.pages);
    }

    private void StartLevelDialogue(List<LevelDialoguePage> pages)
    {
        if (isDialogueActive || isLevelDialogueActive)
        {
            Debug.LogWarning("DialogueManager: Dialogue already in progress");
            return;
        }

        currentLevelPages = pages;
        currentLevelPageIndex = 0;
        isLevelDialogueActive = true;

        // pause game time
        originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // show first page
        dialogueUI.ShowDialogue();
        DisplayCurrentLevelPage();
    }

    private void DisplayCurrentLevelPage()
    {
        if (currentLevelPages == null || currentLevelPageIndex >= currentLevelPages.Count)
        {
            EndLevelDialogue();
            return;
        }

        LevelDialoguePage currentPage = currentLevelPages[currentLevelPageIndex];
        dialogueUI.SetDialogueLine(currentPage.speakerName, currentPage.dialogueText);
    }

    private void EndLevelDialogue()
    {
        if (!isLevelDialogueActive)
        {
            return;
        }

        isLevelDialogueActive = false;
        currentLevelPages = null;
        currentLevelPageIndex = 0;

        // resume game time
        Time.timeScale = originalTimeScale;

        // hide dialogue
        dialogueUI.HideDialogue();
    }

}
