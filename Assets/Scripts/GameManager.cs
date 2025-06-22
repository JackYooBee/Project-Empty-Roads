using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Game References")]
    public TitleScreenManager titleScreenManager;
    public PlayerMovement playerMovement;

    [Header("UI Elements")]
    public Canvas gameplayUI;
    public GameObject tutorialPrompt;
    public Text tutorialText;
    public GameObject timerUI;
    public Text timerText;
    public Text promptText; // For pickup/clean prompts

    [Header("Game Objects")]
    public GameObject broomObject;
    public GameObject mrSmilesObject;
    public List<GameObject> trashPiles = new List<GameObject>(); // Assign Forecourt Mess (1-4)

    [Header("Audio")]
    public AudioSource gameAudioSource;
    public AudioClip sweepingSound;
    public AudioClip jumpscareSound;

    [Header("Game Settings")]
    public float tutorialDisplayTime = 3f;
    public float gameTimeLimit = 60f;
    public float cleaningTime = 4f;
    public float jumpscareDisplayTime = 3f;
    public float interactionRange = 3f; // Increased range

    // Game state variables
    private bool gameStarted = false;
    private bool broomCollected = false;
    private bool timerActive = false;
    private float currentTime;
    private int trashCleaned = 0;
    private bool isCleaningTrash = false;
    private bool gameEnded = false;

    // Interaction variables
    private GameObject currentInteractable = null;
    private Camera playerCamera;

    // Store original trash pile positions for reset
    private Dictionary<GameObject, bool> originalTrashStates = new Dictionary<GameObject, bool>();

    void Start()
    {
        // Get player camera reference
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = Object.FindFirstObjectByType<Camera>();
        }

        // Initialize UI
        SetupUI();

        // Hide Mr. Smiles initially
        if (mrSmilesObject != null)
            mrSmilesObject.SetActive(false);

        // Setup audio source
        if (gameAudioSource == null)
        {
            gameAudioSource = GetComponent<AudioSource>();
            if (gameAudioSource == null)
                gameAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Store original trash pile states
        foreach (GameObject trash in trashPiles)
        {
            if (trash != null)
            {
                originalTrashStates[trash] = trash.activeInHierarchy;
            }
        }

        Debug.Log($"GameManager initialized with {trashPiles.Count} trash piles");
    }

    void Update()
    {
        if (!gameStarted || gameEnded) return;

        // Handle interactions
        HandleInteractions();

        // Handle input
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }

        // Update timer if active
        if (timerActive)
        {
            UpdateTimer();
        }
    }

    void SetupUI()
    {
        // Ensure gameplay UI is active but hide individual elements
        if (gameplayUI != null)
        {
            gameplayUI.gameObject.SetActive(true);
            Debug.Log("Gameplay UI activated");
        }

        // Hide all UI elements initially
        if (tutorialPrompt != null)
        {
            tutorialPrompt.SetActive(false);
            Debug.Log("Tutorial prompt hidden");
        }

        if (timerUI != null)
        {
            timerUI.SetActive(false);
            Debug.Log("Timer UI hidden");
        }

        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
            Debug.Log("Prompt text hidden");
        }
    }

    public void StartGame()
    {
        if (gameStarted) return;

        gameStarted = true;
        gameEnded = false;
        broomCollected = false;
        timerActive = false;
        trashCleaned = 0;
        currentTime = gameTimeLimit;

        Debug.Log("Game started - showing tutorial");
        StartCoroutine(ShowTutorial());
    }

    IEnumerator ShowTutorial()
    {
        if (tutorialPrompt != null && tutorialText != null)
        {
            tutorialText.text = "Go get the broom from the staff room and clean outside.\n\nBe careful...";
            tutorialPrompt.SetActive(true);
            Debug.Log("Tutorial displayed: " + tutorialText.text);

            yield return new WaitForSeconds(tutorialDisplayTime);

            tutorialPrompt.SetActive(false);
            Debug.Log("Tutorial hidden");
        }
        else
        {
            Debug.LogError("Tutorial UI elements are not assigned!");
        }

        Debug.Log("Tutorial finished - game active");
    }

    void HandleInteractions()
    {
        // Get the camera being used (could be different during gameplay)
        Camera currentCamera = playerCamera;
        if (currentCamera == null || !currentCamera.enabled)
        {
            // Find the active camera
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled)
                {
                    currentCamera = cam;
                    break;
                }
            }
        }

        if (currentCamera == null)
        {
            Debug.LogError("No active camera found for interactions!");
            // Hide prompt if no camera found
            if (promptText != null)
                promptText.gameObject.SetActive(false);
            return;
        }

        // Raycast from camera center to detect interactables
        Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        Ray ray = currentCamera.ScreenPointToRay(screenCenter);
        RaycastHit hit;

        GameObject previousInteractable = currentInteractable;
        currentInteractable = null;

        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green, 0.1f);

            // Check if it's an interactable object
            if (CanInteractWith(hitObject))
            {
                currentInteractable = hitObject;
                Debug.Log($"Can interact with: {hitObject.name}");
            }
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red, 0.1f);
        }

        // Always update prompt text (this will hide it if currentInteractable is null)
        UpdatePromptText();
    }

    void UpdatePromptText()
    {
        if (promptText == null) return;

        // If no interactable object or currently cleaning, hide the prompt
        if (currentInteractable == null || isCleaningTrash)
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        // Show the prompt and set appropriate text
        promptText.gameObject.SetActive(true);

        if (currentInteractable == broomObject && !broomCollected)
        {
            promptText.text = "Press E to pick up Broom";
        }
        else if (broomCollected && trashPiles.Contains(currentInteractable))
        {
            promptText.text = "Press E to clean trash";
        }
        else
        {
            // Failsafe: if somehow there's an interactable that doesn't match the conditions
            promptText.gameObject.SetActive(false);
            return;
        }

        Debug.Log($"Prompt text updated: {promptText.text}");
    }

    bool CanInteractWith(GameObject obj)
    {
        if (obj == null) return false;

        // Can interact with broom if not collected yet
        if (obj == broomObject && !broomCollected)
        {
            Debug.Log("Can interact with broom");
            return true;
        }

        // Can interact with trash if broom is collected, not currently cleaning, and trash is active
        if (broomCollected && !isCleaningTrash && trashPiles.Contains(obj) && obj.activeInHierarchy)
        {
            Debug.Log($"Can interact with trash: {obj.name}");
            return true;
        }

        Debug.Log($"Cannot interact with: {obj.name}");
        return false;
    }

    void TryInteract()
    {
        if (currentInteractable == null || isCleaningTrash)
        {
            Debug.Log("Cannot interact - no interactable or currently cleaning");
            return;
        }

        if (currentInteractable == broomObject && !broomCollected)
        {
            CollectBroom();
        }
        else if (broomCollected && trashPiles.Contains(currentInteractable))
        {
            StartCoroutine(CleanTrash(currentInteractable));
        }
    }

    void CollectBroom()
    {
        broomCollected = true;

        // Hide broom object
        if (broomObject != null)
            broomObject.SetActive(false);

        // Start timer
        StartTimer();

        Debug.Log("Broom collected - timer started");
    }

    void StartTimer()
    {
        timerActive = true;
        currentTime = gameTimeLimit;

        if (timerUI != null)
        {
            timerUI.SetActive(true);
            Debug.Log("Timer UI activated");
        }

        Debug.Log("60 second timer started");
    }

    void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        // Update timer display
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Change color when time is running low
            if (currentTime <= 10f)
                timerText.color = Color.red;
            else if (currentTime <= 20f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.white;
        }

        // Check for time up
        if (currentTime <= 0)
        {
            TriggerFailure();
        }
    }

    IEnumerator CleanTrash(GameObject trashPile)
    {
        if (trashPile == null || !trashPiles.Contains(trashPile))
        {
            Debug.LogError("Invalid trash pile for cleaning");
            yield break;
        }

        isCleaningTrash = true;

        // Play sweeping sound
        if (gameAudioSource != null && sweepingSound != null)
        {
            gameAudioSource.clip = sweepingSound;
            gameAudioSource.loop = true;
            gameAudioSource.Play();
        }

        Debug.Log($"Cleaning {trashPile.name}...");

        // Show cleaning feedback
        if (promptText != null)
        {
            promptText.text = "Cleaning... Please wait";
        }

        yield return new WaitForSeconds(cleaningTime);

        // Stop sweeping sound
        if (gameAudioSource != null)
        {
            gameAudioSource.Stop();
            gameAudioSource.loop = false;
        }

        // Remove trash pile from active list but keep reference for reset
        trashPile.SetActive(false);
        trashCleaned++;

        Debug.Log($"Trash cleaned! {trashCleaned}/4 complete");

        isCleaningTrash = false;

        // Check if all trash is cleaned
        if (trashCleaned >= 4)
        {
            CompleteGame();
        }
    }

    void CompleteGame()
    {
        timerActive = false;
        gameEnded = true;

        if (timerUI != null)
            timerUI.SetActive(false);

        if (promptText != null)
        {
            promptText.text = "Well done! All trash cleaned!";
            promptText.gameObject.SetActive(true);
        }

        Debug.Log("Game completed successfully!");

        // Return to title after a delay
        StartCoroutine(ReturnToTitleAfterDelay(3f));
    }

    void TriggerFailure()
    {
        if (gameEnded) return;

        timerActive = false;
        gameEnded = true;

        Debug.Log("Game failed - triggering jumpscare");
        StartCoroutine(ShowJumpscare());
    }

    IEnumerator ShowJumpscare()
    {
        // Hide UI
        if (timerUI != null)
            timerUI.SetActive(false);
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        // Get current camera
        Camera currentCamera = playerCamera;
        if (currentCamera == null || !currentCamera.enabled)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.enabled)
                {
                    currentCamera = cam;
                    break;
                }
            }
        }

        if (mrSmilesObject != null && currentCamera != null)
        {
            Transform cameraTransform = currentCamera.transform;

            // Get horizontal forward direction (no vertical component)
            Vector3 horizontalForward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;

            // Position at ground level
            float groundLevel = 0f;
            float distanceFromPlayer = 2.5f;

            Vector3 targetPosition = cameraTransform.position + (horizontalForward * distanceFromPlayer);
            targetPosition.y = groundLevel; // Force to ground level

            mrSmilesObject.transform.position = targetPosition;

            // Make sure Mr. Smiles faces the camera
            Vector3 directionToCamera = (cameraTransform.position - targetPosition).normalized;
            directionToCamera.y = 0; // Keep rotation horizontal only
            mrSmilesObject.transform.rotation = Quaternion.LookRotation(directionToCamera);

            mrSmilesObject.SetActive(true);

            Debug.Log($"Mr. Smiles grounded at: {targetPosition}");
        }

        // Play jumpscare sound
        if (gameAudioSource != null && jumpscareSound != null)
        {
            gameAudioSource.clip = jumpscareSound;
            gameAudioSource.loop = false;
            gameAudioSource.Play();
        }

        yield return new WaitForSeconds(jumpscareDisplayTime);

        if (mrSmilesObject != null)
            mrSmilesObject.SetActive(false);

        ReturnToTitle();
    }

    IEnumerator ReturnToTitleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToTitle();
    }

    void ReturnToTitle()
    {
        Debug.Log("Returning to title screen");

        // Reset game state
        ResetGameState();

        // Use title screen manager to return to title
        if (titleScreenManager != null)
        {
            titleScreenManager.ReturnToTitleScreen();
        }
    }

    void ResetGameState()
    {
        gameStarted = false;
        gameEnded = false;
        broomCollected = false;
        timerActive = false;
        trashCleaned = 0;
        isCleaningTrash = false;
        currentInteractable = null;

        // Reset broom
        if (broomObject != null)
            broomObject.SetActive(true);

        // Reset trash piles using stored original states
        foreach (var kvp in originalTrashStates)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }

        // Reset UI
        if (timerUI != null)
            timerUI.SetActive(false);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
        if (tutorialPrompt != null)
            tutorialPrompt.SetActive(false);

        // Hide Mr. Smiles
        if (mrSmilesObject != null)
            mrSmilesObject.SetActive(false);

        Debug.Log("Game state reset");
    }

    // Public methods for external scripts
    public bool IsBroomCollected() { return broomCollected; }
    public bool IsGameActive() { return gameStarted && !gameEnded; }
    public float GetTimeRemaining() { return currentTime; }
    public int GetTrashCleaned() { return trashCleaned; }

    // Debug method to test interactions
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, 0);
            Ray ray = playerCamera.ScreenPointToRay(screenCenter);
            Gizmos.DrawRay(ray.origin, ray.direction * interactionRange);
        }
    }
}
