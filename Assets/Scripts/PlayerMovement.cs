using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 12f;
    public float groundDrag = 5f;
    public float movementMultiplier = 10f;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDepletionRate = 33.33f;
    public float staminaRegenRate = 20f;
    public float minStaminaToSprint = 10f;

    [Header("UI References")]
    public Slider staminaBar;
    public Image staminaFill;

    [Header("Visual Feedback")]
    public Color normalStaminaColor = Color.green;
    public Color lowStaminaColor = Color.red;

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip walkFootstepClip;
    public AudioClip runFootstepClip;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    // Sprint and stamina variables
    private float currentStamina;
    private bool isSprinting = false;
    private bool canSprint = true;

    // Footstep variables
    private AudioClip currentFootstepClip;
    private bool wasMovingLastFrame = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("No Rigidbody component found on player!");
            return;
        }

        // Configure rigidbody properly
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;

        // Initialize stamina
        currentStamina = maxStamina;

        // Setup UI
        if (staminaBar != null)
        {
            staminaBar.maxValue = maxStamina;
            staminaBar.value = currentStamina;
        }

        // Setup footstep audio
        SetupFootstepAudio();

        // Check orientation - this should be assigned in the inspector
        if (orientation == null)
        {
            Debug.LogError("Orientation Transform is not assigned! Please assign it in the inspector.");
        }

        Debug.Log("PlayerMovement initialized successfully");
    }

    private void Update()
    {
        // Only process input if this script is enabled
        if (!enabled) return;

        // Ground check
        GroundCheck();

        // Get input - using direct key input for reliability
        MyInput();

        // Handle sprint logic
        HandleSprint();

        // Handle stamina
        HandleStamina();

        // Handle footsteps
        HandleFootsteps();

        // Update UI
        UpdateUI();

        // Apply drag
        ApplyDrag();

        // Debug key for testing
        if (Input.GetKeyDown(KeyCode.T))
        {
            DebugPlayerState();
        }
    }

    private void FixedUpdate()
    {
        // Only move if this script is enabled
        if (!enabled) return;

        MovePlayer();
    }

    private void GroundCheck()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.1f;
        float rayDistance = (playerHeight * 0.5f) + 0.3f;

        // Try multiple ground check methods
        bool raycastCheck = Physics.Raycast(rayStart, Vector3.down, rayDistance, whatIsGround);
        bool sphereCheck = Physics.CheckSphere(transform.position - Vector3.up * (playerHeight * 0.5f), 0.2f, whatIsGround);

        grounded = raycastCheck || sphereCheck;

        // Debug ground checking
        Debug.DrawRay(rayStart, Vector3.down * rayDistance, grounded ? Color.green : Color.red);

        // Additional debug info
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Ground Check - Raycast: {raycastCheck}, Sphere: {sphereCheck}, LayerMask: {whatIsGround.value}, Player Y: {transform.position.y}");

            // Check what objects are below
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance))
            {
                Debug.Log($"Hit object: {hit.collider.name}, Layer: {hit.collider.gameObject.layer}, Distance: {hit.distance}");
            }
            else
            {
                Debug.Log("No ground detected below player");
            }
        }
    }

    private void MyInput()
    {
        // Reset input
        horizontalInput = 0f;
        verticalInput = 0f;

        // Direct key input - most reliable method
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            verticalInput = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            verticalInput = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontalInput = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontalInput = -1f;

        // Try Input Manager as backup (with error handling)
        try
        {
            float axisH = Input.GetAxisRaw("Horizontal");
            float axisV = Input.GetAxisRaw("Vertical");

            // Only use axis input if direct keys didn't register anything
            if (horizontalInput == 0f && axisH != 0f)
                horizontalInput = axisH;
            if (verticalInput == 0f && axisV != 0f)
                verticalInput = axisV;
        }
        catch (System.Exception)
        {
            // Input Manager axes not configured, continue with direct key input
        }

        // Debug input every few frames
        if (Time.frameCount % 30 == 0 && IsMoving())
        {
            Debug.Log($"Input detected: H={horizontalInput}, V={verticalInput}");
        }
    }

    private void HandleSprint()
    {
        bool wantsToSprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (wantsToSprint && canSprint && currentStamina > minStaminaToSprint && IsMoving())
        {
            isSprinting = true;
        }
        else
        {
            isSprinting = false;
        }

        if (currentStamina <= 0)
        {
            canSprint = false;
        }
        else if (currentStamina >= minStaminaToSprint)
        {
            canSprint = true;
        }
    }

    private void HandleStamina()
    {
        if (isSprinting && IsMoving())
        {
            currentStamina -= staminaDepletionRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
        }
        else if (!isSprinting)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(maxStamina, currentStamina);
        }
    }

    private void MovePlayer()
    {
        if (!IsMoving())
        {
            return;
        }

        // This ensures movement is relative to where the player's looking
        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;

        // Calculate movement direction based on input
        // W/S uses forward/backward, A/D uses left/right
        moveDirection = (forward * verticalInput + right * horizontalInput).normalized;

        // Get current speed
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Apply force
        Vector3 forceToApply = moveDirection * currentSpeed * movementMultiplier;

        // Apply force regardless of grounded state (for debugging)
        rb.AddForce(forceToApply, ForceMode.Force);

        // Debug movement every frame when moving
        Debug.Log($"Applying Force: {forceToApply}, Grounded: {grounded}, Current Velocity: {rb.linearVelocity}");

        // Limit velocity if needed
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > currentSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void ApplyDrag()
    {
        if (grounded)
        {
            rb.linearDamping = groundDrag;
        }
        else
        {
            rb.linearDamping = 0f;
        }
    }

    private void HandleFootsteps()
    {
        bool isCurrentlyMoving = IsMoving();

        if (isCurrentlyMoving && grounded && footstepAudioSource != null)
        {
            AudioClip clipToUse = isSprinting ? runFootstepClip : walkFootstepClip;

            if (clipToUse != null)
            {
                if (!footstepAudioSource.isPlaying || currentFootstepClip != clipToUse || !wasMovingLastFrame)
                {
                    currentFootstepClip = clipToUse;
                    footstepAudioSource.clip = clipToUse;
                    footstepAudioSource.loop = true;
                    footstepAudioSource.Play();
                }
            }
        }
        else
        {
            if (footstepAudioSource != null && footstepAudioSource.isPlaying)
            {
                footstepAudioSource.Stop();
            }
        }

        wasMovingLastFrame = isCurrentlyMoving;
    }

    private void SetupFootstepAudio()
    {
        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.loop = false;
            footstepAudioSource.playOnAwake = false;
        }
    }

    private void UpdateUI()
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina;
            bool shouldShowBar = currentStamina < maxStamina || isSprinting;
            staminaBar.gameObject.SetActive(shouldShowBar);
        }

        if (staminaFill != null)
        {
            float staminaPercentage = currentStamina / maxStamina;
            Color targetColor;
            if (staminaPercentage > 0.6f)
                targetColor = normalStaminaColor;
            else if (staminaPercentage > 0.3f)
                targetColor = Color.yellow;
            else
                targetColor = lowStaminaColor;

            staminaFill.color = Color.Lerp(staminaFill.color, targetColor, Time.deltaTime * 5f);
        }
    }

    private void DebugPlayerState()
    {
        Debug.Log("=== PLAYER MOVEMENT DEBUG ===");
        Debug.Log($"Script enabled: {enabled}");
        Debug.Log($"GameObject active: {gameObject.activeInHierarchy}");
        Debug.Log($"Input: H={horizontalInput}, V={verticalInput}");
        Debug.Log($"Position: {transform.position}");
        Debug.Log($"Velocity: {rb.linearVelocity}");
        Debug.Log($"Grounded: {grounded}");
        Debug.Log($"Rigidbody isKinematic: {rb.isKinematic}");
        Debug.Log($"Cursor state: {Cursor.lockState}");
        Debug.Log($"Orientation forward: {(orientation != null ? orientation.forward : Vector3.zero)}");
        Debug.Log($"Move direction: {moveDirection}");

        // Test keys directly
        Debug.Log($"Keys: W={Input.GetKey(KeyCode.W)}, A={Input.GetKey(KeyCode.A)}, S={Input.GetKey(KeyCode.S)}, D={Input.GetKey(KeyCode.D)}");
    }

    // Helper methods
    bool IsMoving()
    {
        return Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f;
    }

    // Public methods for other scripts
    public bool IsSprinting() { return isSprinting; }
    public bool IsWalking() { return IsMoving() && !isSprinting && grounded; }
    public bool IsRunning() { return IsMoving() && isSprinting && grounded; }
    public float GetStaminaPercentage() { return currentStamina / maxStamina; }
    public bool CanSprint() { return canSprint; }

    // Method to enable movement (called by TitleScreenManager)
    public void EnableMovement()
    {
        enabled = true;
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        Debug.Log("PlayerMovement enabled via EnableMovement() method");
    }

    // Method to disable movement
    public void DisableMovement()
    {
        enabled = false;
        horizontalInput = 0f;
        verticalInput = 0f;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        Debug.Log("PlayerMovement disabled via DisableMovement() method");
    }
}