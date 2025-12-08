using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "StartUI";

    [Header("Transition")]
    [SerializeField] private float fadeOutDuration = 0.3f;

    public static bool IsPaused { get; private set; } = false;

    // Static fade overlay that persists across scenes
    private static GameObject fadeOverlay;
    private static Image fadeImage;
    private static Camera transitionCamera;

    void Start()
    {
        IsPaused = false;
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Create fade overlay if it doesn't exist
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }
        else
        {
            // Hide the overlay and disable transition camera when a new scene starts
            fadeOverlay.SetActive(false);
            if (transitionCamera != null)
            {
                transitionCamera.gameObject.SetActive(false);
            }
        }
    }

    private void CreateFadeOverlay()
    {
        // Create a simple camera that just renders black - this prevents "No cameras rendering"
        // This is a SEPARATE object from the overlay so it can stay active independently
        GameObject camObj = new GameObject("TransitionCamera");
        DontDestroyOnLoad(camObj);
        transitionCamera = camObj.AddComponent<Camera>();
        transitionCamera.clearFlags = CameraClearFlags.SolidColor;
        transitionCamera.backgroundColor = Color.black;
        transitionCamera.cullingMask = 0; // Render nothing
        transitionCamera.depth = -100; // Render first (lowest priority)
        camObj.SetActive(false); // Entire GameObject disabled by default

        // Create a standalone canvas for the fade overlay
        fadeOverlay = new GameObject("SceneTransitionOverlay");
        DontDestroyOnLoad(fadeOverlay);

        // Add a helper component to handle scene loaded events
        fadeOverlay.AddComponent<SceneTransitionHelper>();

        Canvas canvas = fadeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = fadeOverlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Create the black image directly on the canvas
        fadeImage = fadeOverlay.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;

        fadeOverlay.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;

        // Lock and hide cursor for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Restart()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAndDestroyPersistent(SceneManager.GetActiveScene().name));
    }

    public void GoToMainMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAndDestroyPersistent(mainMenuSceneName));
    }

    private IEnumerator LoadSceneAndDestroyPersistent(string sceneName)
    {
        // Step 1: Fade to black first
        yield return StartCoroutine(FadeOut());

        // Step 2: Enable transition camera to prevent "No cameras rendering"
        if (transitionCamera != null)
        {
            transitionCamera.gameObject.SetActive(true);
        }

        // Step 3: Clear the singleton reference BEFORE destroying
        // This allows the new scene's Player to become the new Instance
        PlayerController.Instance = null;

        // Step 4: Destroy all persistent objects (including camera)
        DestroyPersistentObjects();

        // Step 5: Load the new scene (transition camera keeps rendering black)
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeOut()
    {
        fadeOverlay.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaledDeltaTime since timeScale might be 0
            float alpha = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = Color.black;
    }

    private void DestroyPersistentObjects()
    {
        // Find and destroy all objects in DontDestroyOnLoad scene
        // This includes Player and any other persistent objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject transitionCamObj = transitionCamera != null ? transitionCamera.gameObject : null;

        foreach (GameObject obj in allObjects)
        {
            // Check if this object is in DontDestroyOnLoad (has no scene or scene name is "DontDestroyOnLoad")
            if (obj.scene.name == "DontDestroyOnLoad" || obj.scene.buildIndex == -1)
            {
                // Don't destroy essential Unity objects, fade overlay, or transition camera
                if (obj.name != "EventSystem" &&
                    !obj.name.Contains("Canvas") &&
                    obj != fadeOverlay &&
                    obj != transitionCamObj)
                {
                    Destroy(obj);
                }
            }
        }
    }

    void OnDestroy()
    {
        // Ensure time scale and pause state are reset when this object is destroyed
        Time.timeScale = 1f;
        IsPaused = false;
    }

    // Public method to hide transition elements (called by SceneTransitionHelper)
    public static void HideTransitionOverlay()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.SetActive(false);
        }
        if (transitionCamera != null)
        {
            transitionCamera.gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// Helper component that listens for scene load events and hides the transition overlay.
/// This ensures the overlay is hidden even in scenes without PauseMenuController.
/// </summary>
public class SceneTransitionHelper : MonoBehaviour
{
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Wait one frame to ensure the new scene's camera is ready, then hide overlay
        StartCoroutine(HideOverlayNextFrame());
    }

    private IEnumerator HideOverlayNextFrame()
    {
        yield return null; // Wait one frame
        PauseMenuController.HideTransitionOverlay();
    }
}
