using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TitleScreenManager : MonoBehaviour
{
    [Header("Cameras")]
    public Camera titleCamera;
    public Camera gameplayCamera;

    [Header("UI Elements")]
    public Canvas titleUI;
    public GameObject mainMenuPanel;
    public Button newGameButton;
    public Button continueButton;
    public Button optionsButton;
    public Button exitButton;

    [Header("Options Panel")]
    public GameObject optionsPanel;
    public Slider brightnessSlider;
    public Slider volumeSlider;
    public Button backButton;

    [Header("Player References")]
    public PlayerMovement playerMovement;
    public PlayerCamera playerCamera;
    public GameObject playerObject;

    [Header("Game Manager")]
    public GameManager gameManager;

    [Header("Transition Settings")]
    public float transitionDuration = 0.5f;
    public bool useFadeTransition = true;
    public Image fadeImage;

    [Header("Pause/Return Settings")]
    public KeyCode returnToTitleKey = KeyCode.Backspace;
    public bool enableReturnToTitle = true;

    private bool isInTitleScreen = true;

    void Start()
    {
        SetupTitleScreen();
        SetupButtons();
        LoadPlayerPrefs();
    }

    void Update()
    {
        // Force cursor to be visible and unlocked during title screen
        if (isInTitleScreen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Check for return to title screen input during gameplay
            if (enableReturnToTitle && Input.GetKeyDown(returnToTitleKey))
            {
                Debug.Log("Return to title screen requested via " + returnToTitleKey);
                ReturnToTitleScreen();
            }
        }
    }

    void SetupTitleScreen()
    {
        // Enable title screen camera
        if (titleCamera != null)
            titleCamera.enabled = true;
        if (gameplayCamera != null)
            gameplayCamera.enabled = false;

        // Show title UI
        if (titleUI != null)
            titleUI.gameObject.SetActive(true);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        // Disable player controls
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }
        if (playerCamera != null)
            playerCamera.enabled = false;

        // Unlock cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isInTitleScreen = true;
        Debug.Log("Title screen setup complete");
    }

    void SetupButtons()
    {
        // Setup button listeners
        if (newGameButton != null)
            newGameButton.onClick.AddListener(StartNewGame);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
            continueButton.interactable = HasSaveData();
        }

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        if (backButton != null)
            backButton.onClick.AddListener(CloseOptions);

        // Setup sliders
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(SetBrightness);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    void LoadPlayerPrefs()
    {
        // Load and apply saved settings
        if (brightnessSlider != null)
        {
            float brightness = PlayerPrefs.GetFloat("Brightness", 1.0f);
            brightnessSlider.value = brightness;
            SetBrightness(brightness);
        }

        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("Volume", 1.0f);
            volumeSlider.value = volume;
            SetVolume(volume);
        }
    }

    public void StartNewGame()
    {
        if (!isInTitleScreen) return;

        Debug.Log("Starting new game...");
        StartCoroutine(TransitionToGameplay());
    }

    public void ContinueGame()
    {
        if (!isInTitleScreen) return;

        LoadGameData();
        StartCoroutine(TransitionToGameplay());
    }

    public void OpenOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void CloseOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        SaveSettings();
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator TransitionToGameplay()
    {
        Debug.Log("=== STARTING TRANSITION TO GAMEPLAY ===");

        // Set flag immediately
        isInTitleScreen = false;

        // Optional fade out
        if (useFadeTransition && fadeImage != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // Hide title UI
        if (titleUI != null)
        {
            titleUI.gameObject.SetActive(false);
            Debug.Log("Title UI hidden");
        }

        // Switch cameras
        if (titleCamera != null)
            titleCamera.enabled = false;
        if (gameplayCamera != null)
            gameplayCamera.enabled = true;

        Debug.Log("Cameras switched");

        // Enable player controls with detailed debugging
        if (playerMovement != null)
        {
            // Ensure the player object is active
            if (playerMovement.gameObject != null)
            {
                playerMovement.gameObject.SetActive(true);
                Debug.Log($"Player GameObject active: {playerMovement.gameObject.activeInHierarchy}");
            }

            // Enable our player movement
            playerMovement.EnableMovement(); // Use the specific method
            Debug.Log($"PlayerMovement enabled: {playerMovement.enabled}");

            // Verify rigidbody setup
            Rigidbody playerRb = playerMovement.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.isKinematic = false;
                playerRb.useGravity = true;
                Debug.Log($"Rigidbody setup - Kinematic: {playerRb.isKinematic}, Gravity: {playerRb.useGravity}");
            }
            else
            {
                Debug.LogError("No Rigidbody found on player!");
            }
        }
        else
        {
            Debug.LogError("PlayerMovement reference is null!");
        }

        // Enable player camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            Debug.Log($"PlayerCamera enabled: {playerCamera.enabled}");
        }

        // Set cursor for gameplay
        SetGameplayCursor();

        // Optional fade in
        if (useFadeTransition && fadeImage != null)
        {
            yield return StartCoroutine(FadeIn());
        }

        Debug.Log("=== TRANSITION COMPLETE ===");

        // Wait a frame to ensure everything is set up
        yield return null;

        // Start the game via GameManager
        if (gameManager != null)
        {
            Debug.Log("Starting game via GameManager");
            gameManager.StartGame();
        }
        else
        {
            Debug.LogError("GameManager reference is null! Please assign it in the inspector.");
        }

        // Test input after a brief delay
        yield return new WaitForSeconds(0.1f);
        TestInputAfterTransition();
    }

    private IEnumerator TransitionToTitleScreen()
    {
        Debug.Log("=== STARTING TRANSITION TO TITLE SCREEN ===");

        // Set flag immediately to prevent multiple transitions
        isInTitleScreen = true;

        // Optional fade out
        if (useFadeTransition && fadeImage != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // Disable player controls first
        if (playerMovement != null)
        {
            playerMovement.DisableMovement(); // Use the specific method
            Debug.Log("Player movement disabled");
        }

        if (playerCamera != null)
        {
            playerCamera.enabled = false;
            Debug.Log("Player camera disabled");
        }

        // Switch cameras
        if (gameplayCamera != null)
            gameplayCamera.enabled = false;
        if (titleCamera != null)
            titleCamera.enabled = true;

        Debug.Log("Cameras switched to title");

        // Show title UI
        if (titleUI != null)
        {
            titleUI.gameObject.SetActive(true);
            Debug.Log("Title UI shown");
        }

        // Ensure player's on the main menu panel, not options
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        // Set cursor for menu interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Optional fade in
        if (useFadeTransition && fadeImage != null)
        {
            yield return StartCoroutine(FadeIn());
        }

        Debug.Log("=== RETURN TO TITLE SCREEN COMPLETE ===");
    }

    private void SetGameplayCursor()
    {
        // Set cursor state for gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Force the cursor state (Unity sometimes needs this)
        StartCoroutine(ForceCursorState());

        Debug.Log($"Cursor set - Locked: {Cursor.lockState == CursorLockMode.Locked}, Visible: {Cursor.visible}");
    }

    private IEnumerator ForceCursorState()
    {
        // Wait a few frames and force cursor state again
        for (int i = 0; i < 3; i++)
        {
            yield return null;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void TestInputAfterTransition()
    {
        Debug.Log("=== POST-TRANSITION INPUT TEST ===");
        Debug.Log($"Application focused: {Application.isFocused}");
        Debug.Log($"Time scale: {Time.timeScale}");
        Debug.Log($"Cursor state: {Cursor.lockState}");

        // Test direct key input
        Debug.Log($"Direct keys - W: {Input.GetKey(KeyCode.W)}, A: {Input.GetKey(KeyCode.A)}, S: {Input.GetKey(KeyCode.S)}, D: {Input.GetKey(KeyCode.D)}");

        // Test if PlayerMovement is responding
        if (playerMovement != null)
        {
            Debug.Log($"PlayerMovement enabled: {playerMovement.enabled}");
            Debug.Log($"PlayerMovement active: {playerMovement.gameObject.activeInHierarchy}");
        }
    }

    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0;
        Color startColor = fadeImage.color;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsedTime / transitionDuration);
            fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, 1);
    }

    private IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0;
        Color startColor = fadeImage.color;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsedTime / transitionDuration);
            fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(startColor.r, startColor.g, startColor.b, 0);
    }

    private void SetBrightness(float value)
    {
        RenderSettings.ambientIntensity = value;
        PlayerPrefs.SetFloat("Brightness", value);
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }

    private bool HasSaveData()
    {
        return PlayerPrefs.HasKey("SaveData");
    }

    private void LoadGameData()
    {
        // Implement save/load system here
    }

    private void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    public void ReturnToTitleScreen()
    {
        if (isInTitleScreen) return; // Prevent returning if already in title screen

        Debug.Log("Returning to title screen...");
        StartCoroutine(TransitionToTitleScreen());
    }
}