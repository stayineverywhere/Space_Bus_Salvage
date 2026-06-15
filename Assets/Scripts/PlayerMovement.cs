using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Player Stats")]
    public float maxHealth = 100f;
    public float health = 100f;
    public float maxStamina = 100f;
    public float stamina = 100f;
    public float maxOxygen = 100f;
    public float oxygen = 100f;

    [Header("Stat Rates")]
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 10f;
    public float oxygenDrainRate = 0.5f;
    public float oxygenDamageRate = 5f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;

    [Header("Visual")]
    public float bobSpeed = 10f;
    public float bobAmount = 0.05f;

    private CharacterController controller;
    private Camera cam;
    private float verticalRotation;
    private float defaultCamY;
    private float bobTimer;
    private float verticalVelocity;
    private const float Gravity = -15f;

    // Procedural Audio Fields
    private AudioSource footstepSource;
    private AudioSource breathingSource;
    private float footstepTimer;
    private AudioClip stepClip;
    private AudioClip breatheClip;

    public bool IsDead => health <= 0f;
    [HideInInspector] public bool isInsideBus = false;

    // ── Interaction State Machine and Carried Loot Fields ─────────────────────────
    public enum InteractionState { Idle, Highlighting, Interacting, Cooldown }
    [HideInInspector] public InteractionState currentInteractionState = InteractionState.Idle;
    [HideInInspector] public float interactionProgress = 0f;
    [HideInInspector] public string interactionActionName = "";
    [HideInInspector] public LootItem carriedLoot = null;

    private float interactionTimer = 0f;
    private float interactionDuration = 0.5f; // Standard 0.5 seconds pickup duration
    private float interactionCooldownTimer = 0f;
    private float interactionCooldownDuration = 0.3f; // 0.3s debounce cooldown
    
    private GameObject currentInteractTarget = null;
    private bool isInteractingWithBus = false;
    private bool isInteractingWithStorage = false;
    private LootStorageZone targetStorageZone = null;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null) controller = gameObject.AddComponent<CharacterController>();

        controller.slopeLimit = 45f;
        controller.stepOffset = 0.3f;

        cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            GameObject camGO = new GameObject("PlayerCamera");
            camGO.transform.SetParent(transform);
            camGO.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        defaultCamY = cam.transform.localPosition.y;

        SetupProceduralSounds();
    }

    private void SetupProceduralSounds()
    {
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.spatialBlend = 0.0f; // 2D sound for local player
        footstepSource.volume = 0.15f;

        breathingSource = gameObject.AddComponent<AudioSource>();
        breathingSource.spatialBlend = 0.0f;
        breathingSource.loop = true;
        breathingSource.volume = 0f;

        stepClip = CreateFootstepNoiseClip();
        breatheClip = CreateBreathingNoiseClip();
        
        if (breathingSource != null && breatheClip != null)
        {
            breathingSource.clip = breatheClip;
            breathingSource.Play();
        }
    }

    private AudioClip CreateFootstepNoiseClip()
    {
        int samplerate = 44100;
        float duration = 0.1f;
        int sampleCount = Mathf.RoundToInt(samplerate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float rawNoise = Random.Range(-1f, 1f);
            float envelope = Mathf.Exp(-50f * (float)i / samplerate);
            samples[i] = rawNoise * envelope * 0.2f;
        }
        AudioClip clip = AudioClip.Create("FootstepThump", sampleCount, 1, samplerate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBreathingNoiseClip()
    {
        int samplerate = 44100;
        float duration = 3.0f;
        int sampleCount = Mathf.RoundToInt(samplerate * duration);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float cycle = Mathf.Sin(t * Mathf.PI * 2f);
            float noise = Random.Range(-1f, 1f) * 0.15f;
            float amp = Mathf.Max(0f, cycle) * 0.3f + Mathf.Max(0f, -cycle) * 0.2f;
            samples[i] = noise * amp;
        }
        AudioClip clip = AudioClip.Create("HeavyBreathing", sampleCount, 1, samplerate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void Update()
    {
        if (IsDead) return;
        if (PlayerInputController.Instance == null) return;

        // Block all player controls and reset velocities during transitions (Section 3)
        if (BusTransitionController.Instance != null && BusTransitionController.Instance.IsInputLocked)
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            verticalVelocity = 0f;
            if (controller != null && controller.enabled)
            {
                controller.Move(Vector3.zero);
            }
            return;
        }

        // Only control player during active exploration
        bool inExploration = GameLoopManager.Instance == null ||
            GameLoopManager.Instance.CurrentState == GameLoopManager.GameState.Exploration;

        if (!inExploration)
        {
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (breathingSource != null) breathingSource.volume = 0f;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;

        // Cooldown tick
        if (currentInteractionState == InteractionState.Cooldown)
        {
            interactionCooldownTimer -= Time.deltaTime;
            if (interactionCooldownTimer <= 0f)
            {
                currentInteractionState = InteractionState.Idle;
            }
        }

        // Active interaction progress tick
        if (currentInteractionState == InteractionState.Interacting)
        {
            interactionTimer += Time.deltaTime;
            interactionProgress = Mathf.Clamp01(interactionTimer / interactionDuration);

            // Bypass target validation for seamless portal transitions to prevent rotating doors from cancelling warp
            bool isPortal = currentInteractTarget != null && 
                (currentInteractTarget.GetComponent<BusDoorInteractable>() != null || 
                 currentInteractTarget.GetComponentInParent<BusDoorInteractable>() != null);

            if (!isInteractingWithStorage && !isPortal && !ValidateInteractionTarget())
            {
                CancelActiveInteraction();
            }

            if (interactionTimer >= interactionDuration)
            {
                CompleteActiveInteraction();
            }
        }

        HandleRotation();
        HandleMovement();
        HandleStats();
        HandleHeadBob();
        HandleFOV();
        HandleInteraction();
        HandleFootstepsAndBreathing();

        // Return to base shortcut
        if (PlayerInputController.Instance.ReturnToBasePressed)
            GameLoopManager.Instance?.ReturnToGarage();
    }

    private void HandleInteraction()
    {
        if (cam == null || HUDManager.Instance == null) return;

        // Skip looking for new targets if we are actively interacting or in cooldown
        if (currentInteractionState == InteractionState.Interacting || currentInteractionState == InteractionState.Cooldown)
        {
            HUDManager.Instance.SetInteractionPrompt(false);
            return;
        }

        // E while carrying loot: deposit into nearby storage zone, or drop
        if (carriedLoot != null && PlayerInputController.Instance.InteractPressed)
        {
            // 1. Check for storage zone first (deposit takes priority over drop)
            Collider[] cols = Physics.OverlapSphere(transform.position, 2.0f);
            LootStorageZone depositZone = null;
            foreach (var col in cols)
            {
                LootStorageZone z = col.GetComponent<LootStorageZone>();
                if (z != null) { depositZone = z; break; }
            }

            if (depositZone != null)
            {
                // Deposit directly here in Update so WasPressedThisFrame is reliable
                StartLootDeposit(depositZone);
                return;
            }

            // 2. Check for bus door (don't drop near door)
            bool nearDoor = false;
            if (PerformSafeRaycast(out RaycastHit hitCheck, 4.5f))
            {
                if (hitCheck.transform.GetComponent<BusDoor>() != null || hitCheck.transform.GetComponentInParent<BusDoor>() != null ||
                    hitCheck.transform.GetComponent<BusDoorInteractable>() != null || hitCheck.transform.GetComponentInParent<BusDoorInteractable>() != null)
                    nearDoor = true;
            }

            if (!nearDoor)
            {
                DropCarriedLoot();
                return;
            }
        }

        RaycastHit hit;
        bool interactableFound = false;

        if (PerformSafeRaycast(out hit, 4.5f))
        {
            bool wantEntrance = true;
            if (BusTransitionController.Instance != null)
            {
                wantEntrance = (BusTransitionController.Instance.CurrentState == BusTransitionController.BusCabinState.OutsideBus);
            }
            else
            {
                wantEntrance = !isInsideBus;
            }

            // 0. Bus Door Interactable Portals — fire immediately on Q, no 0.5s wait
            BusDoorInteractable bdi = hit.transform.GetComponent<BusDoorInteractable>() ?? hit.transform.GetComponentInParent<BusDoorInteractable>();
            if (bdi != null && bdi.isEntrance == wantEntrance)
            {
                interactableFound = true;
                currentInteractionState = InteractionState.Highlighting;
                currentInteractTarget = bdi.gameObject;
                HUDManager.Instance.SetInteractionPrompt(true, "[Q] ENTER / EXIT");

                if (PlayerInputController.Instance.DoorOpenClosePressed)
                {
                    bdi.TriggerDoorTransition(this);
                    currentInteractionState = InteractionState.Cooldown;
                    interactionCooldownTimer = interactionCooldownDuration;
                    currentInteractTarget = null;
                }
            }
            // 1. Bus Door — show prompt; BusController.Update() handles the actual Q transition
            else if (hit.transform.GetComponent<BusDoor>() != null || hit.transform.GetComponentInParent<BusDoor>() != null)
            {
                BusDoor door = hit.transform.GetComponent<BusDoor>() ?? hit.transform.GetComponentInParent<BusDoor>();
                if (door != null)
                {
                    interactableFound = true;
                    currentInteractionState = InteractionState.Highlighting;
                    currentInteractTarget = door.gameObject;

                    bool isBusDoor = door.GetComponentInParent<BusController>() != null;
                    if (isBusDoor)
                    {
                        // BusController.Update() handles Q press — just show contextual prompt
                        HUDManager.Instance.SetInteractionPrompt(true, isInsideBus ? "[Q] EXIT BUS" : "[Q] ENTER BUS");
                    }
                    else
                    {
                        // Non-bus door: handle toggle with interaction timer
                        HUDManager.Instance.SetInteractionPrompt(true, "[Q] TOGGLE DOOR");
                        if (PlayerInputController.Instance.DoorOpenClosePressed)
                        {
                            currentInteractionState = InteractionState.Interacting;
                            interactionTimer = 0f;
                            interactionActionName = door.isOpen ? "Closing Cabin Door..." : "Opening Cabin Door...";
                            isInteractingWithBus = false;
                            isInteractingWithStorage = false;
                        }
                    }
                }
            }
            // 2. Loot pickup (only if NOT carrying anything)
            else if (carriedLoot == null)
            {
                LootPickup loot = hit.transform.GetComponent<LootPickup>() ?? hit.transform.GetComponentInParent<LootPickup>();
                if (loot != null)
                {
                    interactableFound = true;
                    currentInteractionState = InteractionState.Highlighting;
                    currentInteractTarget = loot.gameObject;
                    HUDManager.Instance.SetInteractionPrompt(true);

                    if (PlayerInputController.Instance.InteractPressed)
                    {
                        currentInteractionState = InteractionState.Interacting;
                        interactionTimer = 0f;
                        interactionActionName = "Securing " + (loot.GetComponent<LootItem>()?.itemName ?? "Loot") + "...";
                        isInteractingWithBus = false;
                        isInteractingWithStorage = false;
                    }
                }
            }

            // 3. Space Bus Driver Seat (only if NOT carrying cargo and looking directly at driver desk console/screens)
            if (!interactableFound && carriedLoot == null)
            {
                BusController bus = hit.transform.GetComponent<BusController>() ?? hit.transform.GetComponentInParent<BusController>();
                if (bus != null)
                {
                    string targetName = hit.transform.name.ToLower();
                    if (targetName.Contains("console") || targetName.Contains("desk") || targetName.Contains("screen") || targetName.Contains("steering"))
                    {
                        interactableFound = true;
                        currentInteractionState = InteractionState.Highlighting;
                        currentInteractTarget = bus.gameObject;
                        HUDManager.Instance.SetInteractionPrompt(true, "[T] PILOT BUS  |  [R] Return to Base");

                        if (PlayerInputController.Instance.PilotSeatPressed)
                        {
                            currentInteractionState = InteractionState.Interacting;
                            interactionTimer = 0f;
                            interactionActionName = "Taking pilot seat...";
                            isInteractingWithBus = true;
                            isInteractingWithStorage = false;
                        }
                    }
                }
            }
        }

        if (!interactableFound)
        {
            currentInteractionState = InteractionState.Idle;
            currentInteractTarget = null;
            HUDManager.Instance.SetInteractionPrompt(false);
        }
    }

    private void DropCarriedLoot()
    {
        if (carriedLoot == null) return;

        Debug.Log($"[Drop] Dropping {carriedLoot.itemName} safely (Section 2)");

        // 1. Unparent object immediately (NEVER parent dropped loot to Player or Camera)
        carriedLoot.transform.SetParent(null);

        // Position slightly in front of player and slightly ABOVE the ground (Section 2)
        Vector3 dropPos = transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;
        if (Physics.Raycast(dropPos, Vector3.down, out RaycastHit hit, 5f))
        {
            dropPos = hit.point + Vector3.up * 0.3f; // Place 0.3m above the ground (Section 2)
        }
        carriedLoot.transform.position = dropPos;
        carriedLoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Re-enable collision
        var col = carriedLoot.GetComponent<Collider>();
        if (col != null) col.enabled = true;
        foreach (var c in carriedLoot.GetComponentsInChildren<Collider>()) c.enabled = true;

        // 2. Ensure Rigidbody is fully enabled (kinematic = false, useGravity = true)
        var rb = carriedLoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 3. Apply small forward impulse (Section 2)
            rb.AddForce(transform.forward * 1.5f, ForceMode.Impulse);
        }

        // Re-enable rendering
        var mr = carriedLoot.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = true;
        foreach (var filter in carriedLoot.GetComponentsInChildren<MeshFilter>(true))
        {
            var r = filter.GetComponent<Renderer>();
            if (r != null) r.enabled = true;
        }

        // Show world labels again
        Transform worldCanvas = carriedLoot.transform.Find("LootWorldCanvas");
        if (worldCanvas != null) worldCanvas.gameObject.SetActive(true);

        carriedLoot = null;

        // Debounce cooldown
        currentInteractionState = InteractionState.Cooldown;
        interactionCooldownTimer = interactionCooldownDuration;
    }

    public void StartLootDeposit(LootStorageZone zone)
    {
        if (carriedLoot == null || currentInteractionState == InteractionState.Interacting) return;

        // Complete deposit on next frame (no wait — removes buffering feel inside bus)
        currentInteractionState = InteractionState.Interacting;
        interactionTimer = interactionDuration;
        interactionActionName = "Depositing " + carriedLoot.itemName + "...";
        isInteractingWithStorage = true;
        isInteractingWithBus = false;
        targetStorageZone = zone;
    }

    private BusDoorInteractable FindNearbyBusDoorInteractable(Vector3 point)
    {
        bool wantEntrance = true;
        if (BusTransitionController.Instance != null)
        {
            wantEntrance = (BusTransitionController.Instance.CurrentState == BusTransitionController.BusCabinState.OutsideBus);
        }
        else
        {
            wantEntrance = !isInsideBus;
        }

        // First: check colliders within 5m of the hit point
        Collider[] cols = Physics.OverlapSphere(point, 5f);
        foreach (var c in cols)
        {
            var bdi = c.GetComponent<BusDoorInteractable>() ?? c.GetComponentInParent<BusDoorInteractable>();
            if (bdi != null && bdi.isEntrance == wantEntrance) return bdi;
        }
        // Fallback: scene-wide search for any BusDoorInteractable within 8m of player
        var allBdi = Object.FindObjectsByType<BusDoorInteractable>(FindObjectsSortMode.None);
        foreach (var bdi in allBdi)
        {
            if (bdi.isEntrance == wantEntrance && Vector3.Distance(transform.position, bdi.transform.position) < 8f) return bdi;
        }
        return null;
    }

    private bool PerformSafeRaycast(out RaycastHit hit, float maxDistance = 4.5f)
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        
        // Sort hits by distance
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            // Ignore player themselves or any children/camera-mounted objects
            if (h.transform.IsChildOf(this.transform)) continue;
            if (carriedLoot != null && (h.transform == carriedLoot.transform || h.transform.IsChildOf(carriedLoot.transform))) continue;

            hit = h;
            return true;
        }

        hit = new RaycastHit();
        return false;
    }

    private bool ValidateInteractionTarget()
    {
        if (currentInteractTarget == null) return false;
        RaycastHit hit;
        if (PerformSafeRaycast(out hit, 4.5f))
        {
            if (hit.transform.gameObject == currentInteractTarget || 
                hit.transform.parent?.gameObject == currentInteractTarget)
            {
                return true;
            }
        }
        return false;
    }

    private void CancelActiveInteraction()
    {
        currentInteractionState = InteractionState.Idle;
        interactionProgress = 0f;
        interactionActionName = "";
        currentInteractTarget = null;
        isInteractingWithBus = false;
        isInteractingWithStorage = false;
        targetStorageZone = null;
    }

    private void CompleteActiveInteraction()
    {
        if (isInteractingWithStorage)
        {
            if (targetStorageZone != null)
            {
                targetStorageZone.CompleteDeposit(this);
            }
        }
        else if (isInteractingWithBus)
        {
            BusController bus = currentInteractTarget?.GetComponent<BusController>() ?? currentInteractTarget?.GetComponentInParent<BusController>();
            if (bus != null)
            {
                bus.EnterBus(gameObject);
            }
        }
        else if (currentInteractTarget != null)
        {
            // First check if it is a seamless portal transition door
            BusDoorInteractable bdi = currentInteractTarget.GetComponent<BusDoorInteractable>() ?? currentInteractTarget.GetComponentInParent<BusDoorInteractable>();
            if (bdi != null)
            {
                bdi.TriggerDoorTransition(this);
            }
            // Second check if it is a normal door toggling action
            else if (currentInteractTarget.GetComponent<BusDoor>() != null || currentInteractTarget.GetComponentInParent<BusDoor>() != null)
            {
                BusDoor door = currentInteractTarget.GetComponent<BusDoor>() ?? currentInteractTarget.GetComponentInParent<BusDoor>();
                if (door != null)
                {
                    door.ToggleDoor();
                }
            }
            else
            {
                LootPickup loot = currentInteractTarget.GetComponent<LootPickup>() ?? currentInteractTarget.GetComponentInParent<LootPickup>();
                if (loot != null)
                {
                    // Grab item physically
                    carriedLoot = loot.GetComponent<LootItem>();
                    if (carriedLoot != null)
                    {
                        // Parent physically to player camera
                        carriedLoot.transform.SetParent(cam.transform);
                        carriedLoot.transform.localPosition = new Vector3(0.32f, -0.28f, 0.65f);
                        carriedLoot.transform.localRotation = Quaternion.Euler(15f, -15f, 10f);

                        // Disable mesh components / collision layers
                        var col = carriedLoot.GetComponent<Collider>();
                        if (col != null) col.enabled = false;
                        foreach (var c in carriedLoot.GetComponentsInChildren<Collider>()) c.enabled = false;

                        var rb = carriedLoot.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = true;

                        // Disable world-space indicators
                        Transform worldCanvas = carriedLoot.transform.Find("LootWorldCanvas");
                        if (worldCanvas != null) worldCanvas.gameObject.SetActive(false);

                        Debug.Log($"[Pickup] Carry state activated for {carriedLoot.itemName}");
                    }
                }
            }
        }

        // Enter Cooldown state to debounce E presses
        currentInteractionState = InteractionState.Cooldown;
        interactionCooldownTimer = interactionCooldownDuration;
        interactionProgress = 0f;
        interactionActionName = "";
        currentInteractTarget = null;
        isInteractingWithBus = false;
        isInteractingWithStorage = false;
        targetStorageZone = null;
    }

    private void HandleFootstepsAndBreathing()
    {
        // Footsteps
        Vector2 move = PlayerInputController.Instance.MoveInput;
        bool isMoving = controller.isGrounded && move.magnitude > 0.1f;
        if (isMoving)
        {
            bool sprinting = PlayerInputController.Instance.IsSprinting && stamina > 0f;
            float currentStepInterval = sprinting ? 0.32f : 0.55f;
            footstepTimer += Time.deltaTime;
            if (footstepTimer >= currentStepInterval)
            {
                footstepTimer = 0f;
                if (footstepSource != null && stepClip != null)
                {
                    footstepSource.PlayOneShot(stepClip);
                }
            }
        }
        else
        {
            footstepTimer = 0f;
        }

        // Low stamina breathing
        if (stamina < 30f)
        {
            float lowStamRatio = 1f - (stamina / 30f);
            if (breathingSource != null)
                breathingSource.volume = Mathf.Lerp(breathingSource.volume, lowStamRatio * 0.45f, Time.deltaTime * 3f);
        }
        else
        {
            if (breathingSource != null)
                breathingSource.volume = Mathf.Lerp(breathingSource.volume, 0f, Time.deltaTime * 5f);
        }
    }

    private void HandleRotation()
    {
        if (cam == null) return;
        Vector2 look = PlayerInputController.Instance.LookInput;
        verticalRotation -= look.y * mouseSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cam.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        transform.Rotate(Vector3.up * look.x * mouseSensitivity);
    }

    private void HandleMovement()
    {
        bool grounded = controller.isGrounded;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += Gravity * Time.deltaTime;

        // Jump — Space, only when grounded
        if (grounded && PlayerInputController.Instance.JumpPressed)
        {
            // Gravity planet = low gravity → higher jump
            bool lowGrav = PlanetManager.Instance != null &&
                           PlanetManager.Instance.activeTheme == HorrorVisualPreset.PlanetTheme.Gravity;
            float effectiveHeight = lowGrav ? jumpHeight * 2.8f : jumpHeight;
            verticalVelocity = Mathf.Sqrt(effectiveHeight * -2f * Gravity);
        }

        Vector2 move = PlayerInputController.Instance.MoveInput;
        bool sprinting = PlayerInputController.Instance.IsSprinting && stamina > 0f;
        float currentSpeed = sprinting ? speed * 2f : speed;

        // Reduce movement speed slightly when carrying heavy cargo
        if (carriedLoot != null)
        {
            currentSpeed *= 0.72f;
        }

        Vector3 horizontal = (transform.right * move.x + transform.forward * move.y) * currentSpeed;
        controller.Move(new Vector3(horizontal.x, verticalVelocity, horizontal.z) * Time.deltaTime);
    }

    private void HandleStats()
    {
        bool moving = PlayerInputController.Instance.MoveInput.magnitude > 0.1f;
        bool sprinting = PlayerInputController.Instance.IsSprinting && moving;

        float currentStaminaRegen = staminaRegenRate;
        if (isInsideBus && BusTransitionController.Instance != null)
        {
            currentStaminaRegen *= BusTransitionController.Instance.staminaRegenMultiplier;
        }

        stamina = sprinting
            ? Mathf.Max(0f, stamina - staminaDrainRate * Time.deltaTime)
            : Mathf.Min(maxStamina, stamina + currentStaminaRegen * Time.deltaTime);

        // Inside Bus Safe Zone: Regenerate oxygen and prevent depletion
        if (isInsideBus)
        {
            float regenRate = (BusTransitionController.Instance != null) ? BusTransitionController.Instance.oxygenRegenRate : 15f;
            oxygen = Mathf.Min(maxOxygen, oxygen + regenRate * Time.deltaTime);
        }
        else
        {
            oxygen = Mathf.Max(0f, oxygen - oxygenDrainRate * Time.deltaTime);
            if (oxygen <= 0f)
                TakeDamage(oxygenDamageRate * Time.deltaTime);
        }
    }

    private void HandleHeadBob()
    {
        if (cam == null) return;
        Vector2 move = PlayerInputController.Instance.MoveInput;
        bool sprinting = PlayerInputController.Instance.IsSprinting;

        if (move.magnitude > 0.1f)
        {
            bobTimer += Time.deltaTime * (sprinting ? bobSpeed * 1.5f : bobSpeed);
            cam.transform.localPosition = new Vector3(
                cam.transform.localPosition.x,
                defaultCamY + Mathf.Sin(bobTimer) * bobAmount,
                cam.transform.localPosition.z);
        }
        else
        {
            bobTimer = 0f;
            cam.transform.localPosition = new Vector3(
                cam.transform.localPosition.x,
                Mathf.Lerp(cam.transform.localPosition.y, defaultCamY, Time.deltaTime * bobSpeed),
                cam.transform.localPosition.z);
        }
    }

    private void HandleFOV()
    {
        if (cam == null) return;
        float target = PlayerInputController.Instance.IsSprinting ? 80f : 60f;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, target, Time.deltaTime * 5f);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        
        // Absolute damage immunity inside the bus safe zone
        if (isInsideBus) return;

        health = Mathf.Max(0f, health - amount);
        HUDManager.Instance?.TriggerDamageFlash();
        if (IsDead) OnDeath();
    }

    public void Heal(float amount)
    {
        health = Mathf.Min(maxHealth, health + amount);
    }

    public void RefillOxygen(float amount)
    {
        oxygen = Mathf.Min(maxOxygen, oxygen + amount);
    }

    private void OnDeath()
    {
        Debug.Log("[Player] Death triggered.");
        enabled = false;
        Cursor.lockState = CursorLockMode.None;

        // Brief slow-motion hit-stop for cinematic feel
        Time.timeScale = 0.15f;

        // Full screen red flash via HUD
        HUDManager.Instance?.TriggerDeathFlash();

        // Restore timescale and show GAME OVER after a short delay
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        Time.timeScale = 1f;
        GameLoopManager.Instance?.EndGame();
    }
}
