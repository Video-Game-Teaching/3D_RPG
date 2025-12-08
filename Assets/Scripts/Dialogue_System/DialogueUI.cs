using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI Factors")]
    public GameObject dialogueContainer;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Animation Settings")]
    // typewriter speed & fade in duration
    public float typewriterSpeed = 0.05f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float fadeInDuration = 0.5f;
    public bool skipFadeInOnFirstShow = false;  // if true, first show will be instant

    private CanvasGroup containerCanvasGroup;
    private Coroutine typewriterCoroutine;
    private Coroutine fadeCoroutine;
    private bool isTyping = false;
    private string currentFullText = "";
    private bool isFirstShow = true;  // track if this is the first time showing dialogue

    void Awake()
    {
        // Get CanvasGroup reference (works even if container is inactive)
        if (dialogueContainer != null)
        {
            containerCanvasGroup = dialogueContainer.GetComponent<CanvasGroup>();

            // Immediately hide dialogue container on scene load (no fade, instant hide)
            if (containerCanvasGroup != null)
            {
                containerCanvasGroup.alpha = 0;
            }
            dialogueContainer.SetActive(false);
        }

        // Reset state on scene load
        ResetState();
    }

    private void ResetState()
    {
        isTyping = false;
        currentFullText = "";
        isFirstShow = true;
        typewriterCoroutine = null;
        fadeCoroutine = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isTyping)
            {
                // if still typing, skip the effect of this line
                CompleteCurrentText();
            }
            else
            {
                // if already ended typing, check if we're in dialogue mode
                if (DialogueManager.Instance != null)
                {
                    // If in dialogue mode, go to next line; otherwise hide the dialogue
                    if (DialogueManager.Instance.IsDialogueActive())
                    {
                        DialogueManager.Instance.NextLine();
                    }
                    else
                    {
                        // Single line mode - just hide the dialogue
                        HideDialogue();
                    }
                }
            }
        }
    }


    public void ShowDialogue()
    {
        if (dialogueContainer != null)
        {
            // Ensure canvas group reference exists even if object started inactive
            if (containerCanvasGroup == null)
            {
                containerCanvasGroup = dialogueContainer.GetComponent<CanvasGroup>();
            }
            dialogueContainer.SetActive(true);

            // If skipFadeInOnFirstShow is enabled and this is the first show, show instantly
            if (skipFadeInOnFirstShow && isFirstShow)
            {
                containerCanvasGroup.alpha = 1;
                isFirstShow = false;
            }
            else
            {
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }
                fadeCoroutine = StartCoroutine(FadeInContainer());
            }
        }
    }

    public void HideDialogue()
    {
        if (dialogueContainer != null)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOutContainer());
        }
    }


    public void SetDialogueLine(string speakerName, string text)
    {
        SetSpeakerName(speakerName);
        SetDialogueText(text);
    }

    public void SetSpeakerName(string speakerName)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }
    }

    public void SetDialogueText(string text)
    {
        currentFullText = text;
        Debug.Log($"DialogueUI: Setting dialogue text: {text}");

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        for (int i = 0; i <= text.Length; i++)
        {
            dialogueText.text = text.Substring(0, i);
            yield return new WaitForSecondsRealtime(typewriterSpeed);   // use realtime to work with Time.timeScale = 0
        }

        isTyping = false;
    }

    private void CompleteCurrentText()
    {
        // stop the typewriter effect and directly show the full text
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
        // if i can get into this func, it means that "SetDialogueText" -> "TypewriterEffect" -> the variable "currentFullText" has already been assigned value
        dialogueText.text = currentFullText;
        isTyping = false;
    }


    private IEnumerator FadeInContainer()
    {
        float elapsedTime = 0;
        containerCanvasGroup.alpha = 0;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;  // use unscaled time to work with Time.timeScale = 0
            float normalizedTime = elapsedTime / fadeInDuration;
            containerCanvasGroup.alpha = fadeInCurve.Evaluate(normalizedTime);
            yield return null;
        }

        containerCanvasGroup.alpha = 1;
    }

    private IEnumerator FadeOutContainer()
    {
        float elapsedTime = 0;
        float startAlpha = containerCanvasGroup.alpha;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;  // use unscaled time to work with Time.timeScale = 0
            float normalizedTime = elapsedTime / fadeInDuration;
            containerCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, normalizedTime);
            yield return null;
        }

        containerCanvasGroup.alpha = 0;
        dialogueContainer.SetActive(false);
    }
}
