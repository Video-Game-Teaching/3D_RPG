using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 10)]
    public string dialogueText;
}

// Declare a typo variable to store the dialogue data
[CreateAssetMenu(fileName = "New Dialogue", menuName = "!!Dialogue System/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string speakerName;
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
}
