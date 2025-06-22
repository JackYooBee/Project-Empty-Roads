using UnityEngine;
using UnityEngine.UI;

public class SprintController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDepletionRate = 33.33f; // Depletes in 3 seconds (100/3)
    public float staminaRegenRate = 20f; // Regeneration rate per second
    public float minStaminaToSprint = 10f; // Minimum stamina needed to start sprinting

    [Header("UI References")]
    public Slider staminaBar; // Drag stamina bar UI element here
    public Image staminaFill; // The fill image of the slider for color changes

    [Header("Visual Feedback")]
    public Color normalStaminaColor = Color.green;
    public Color lowStaminaColor = Color.red;

    // Private variables
    private float currentStamina;
    private bool isSprinting = false;
    private bool canSprint = true;
    private CharacterController characterController;
    private Vector3 moveDirection;

    void Start()
    {
        // Get the CharacterController component
        characterController = GetComponent<CharacterController>();

        // Initialize stamina
        currentStamina = maxStamina;

        // Setup UI
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }

        // If no CharacterController is found, add a warning
        if (characterController == null)
        {
            Debug.LogWarning("No CharacterController found! Please add a CharacterController component to this GameObject.");
        }
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleStamina();
        UpdateUI();
    }

    void HandleInput()
    {
        // Check if shift is held down and player can sprint
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Determine if we should be sprinting
        if (wantsToSprint && canSprint && currentStamina > minStaminaToSprint)
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        // If stamina is too low, prevent sprinting until it regenerates more
        if (currentStamina <= 0)
        {
            canSprint = false;
        }
        else if (currentStamina >= minStaminaToSprint)
        {
            canSprint = true;
        }
    }

    void HandleMovement()
    {
        if (characterController == null) return;

        // Get input for movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate movement direction
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        direction = transform.TransformDirection(direction);

        // Apply speed based on sprint state
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        moveDirection = direction * currentSpeed;

        // Apply gravity
        if (!characterController.isGrounded)
        {
            moveDirection.y -= 9.81f * Time.deltaTime;
        }

        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);
    }

    void HandleStamina()
    {
        if (isSprinting && IsMoving())
        {
            // Deplete stamina while sprinting and moving
            currentStamina -= staminaDepletionRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
        }
        else if (!isSprinting)
        {
            // Regenerate stamina when not sprinting
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
    }

    bool IsMoving()
    {
        // Check if player is providing movement input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        return Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
    }

    void UpdateUI()
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina;
        }

        if (staminaFill != null)
        {
            // Change color based on stamina level
            float staminaPercentage = currentStamina / maxStamina;
            staminaFill.color = Color.Lerp(lowStaminaColor, normalStaminaColor, staminaPercentage);
        }
    }

    // Public methods for external access
    public bool IsSprinting()
    {
        return isSprinting;
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    public bool CanSprint()
    {
        return canSprint;
    }
}