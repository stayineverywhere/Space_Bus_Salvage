using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages all player input via the New Input System.
/// If no InputActionAsset is assigned, creates default WASD/mouse bindings at runtime.
/// </summary>
public class PlayerInputController : MonoBehaviour
{
    private static PlayerInputController _instance;
    public static PlayerInputController Instance
    {
        get
        {
            if (_instance == null) _instance = Object.FindAnyObjectByType<PlayerInputController>();
            return _instance;
        }
    }

    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction interactAction;
    private InputAction doorOpenCloseAction;
    private InputAction exitBusAction;
    private InputAction flashlightAction;
    private InputAction sprintAction;
    private InputAction returnAction;
    private InputAction jumpAction;
    private InputAction pilotSeatAction;

    // ── Public Properties ────────────────────────────────────────────────────

    public Vector2 MoveInput    => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public Vector2 LookInput    => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
    public bool InteractPressed => interactAction?.WasPressedThisFrame() ?? false;
    public bool DoorOpenClosePressed => doorOpenCloseAction?.WasPressedThisFrame() ?? false;
    public bool ExitBusPressed  => exitBusAction?.WasPressedThisFrame() ?? false;
    public bool FlashlightPressed => flashlightAction?.WasPressedThisFrame() ?? false;
    public bool IsSprinting     => sprintAction?.IsPressed() ?? false;
    public bool ReturnToBasePressed => returnAction?.WasPressedThisFrame() ?? false;
    public bool JumpPressed     => jumpAction?.WasPressedThisFrame() ?? false;
    public bool PilotSeatPressed => pilotSeatAction?.WasPressedThisFrame() ?? false;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (inputActions != null)
            BindFromAsset(inputActions);
        else
            CreateDefaultBindings();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        interactAction?.Enable();
        doorOpenCloseAction?.Enable();
        exitBusAction?.Enable();
        flashlightAction?.Enable();
        sprintAction?.Enable();
        returnAction?.Enable();
        jumpAction?.Enable();
        pilotSeatAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        interactAction?.Disable();
        doorOpenCloseAction?.Disable();
        exitBusAction?.Disable();
        flashlightAction?.Disable();
        sprintAction?.Disable();
        returnAction?.Disable();
        jumpAction?.Disable();
        pilotSeatAction?.Disable();
    }

    // ── Binding Helpers ──────────────────────────────────────────────────────

    private void BindFromAsset(InputActionAsset asset)
    {
        var map = asset.FindActionMap("Player");
        if (map == null)
        {
            Debug.LogWarning("[Input] 'Player' action map not found in asset — creating defaults.");
            CreateDefaultBindings();
            return;
        }
        moveAction      = map.FindAction("Move");
        lookAction      = map.FindAction("Look");
        interactAction  = map.FindAction("Interact");
        doorOpenCloseAction = map.FindAction("DoorOpenClose");
        exitBusAction   = map.FindAction("ExitBus");
        flashlightAction= map.FindAction("Flashlight");
        sprintAction    = map.FindAction("Sprint");
        returnAction    = map.FindAction("Return");

        if (doorOpenCloseAction == null)
        {
            doorOpenCloseAction = new InputAction("DoorOpenClose", InputActionType.Button, "<Keyboard>/q");
        }

        jumpAction = map.FindAction("Jump");
        if (jumpAction == null)
        {
            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        }

        Debug.Log("[Input] Bound from InputActionAsset.");
    }

    private void CreateDefaultBindings()
    {
        Debug.Log("[Input] No InputActionAsset — creating default bindings (WASD / Mouse / E / Q / F / LShift / Tab / R).");

        // Move — WASD composite
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        // Arrow keys as alternative
        moveAction.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");

        // Look — mouse delta
        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta",
            expectedControlType: "Vector2");

        // Interact — E
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");

        // Door Open/Close — Q
        doorOpenCloseAction = new InputAction("DoorOpenClose", InputActionType.Button, "<Keyboard>/q");

        // Exit Bus — Tab
        exitBusAction = new InputAction("ExitBus", InputActionType.Button, "<Keyboard>/tab");

        // Flashlight — F
        flashlightAction = new InputAction("Flashlight", InputActionType.Button, "<Keyboard>/f");

        // Sprint — Left Shift
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");

        // Return to base — R
        returnAction = new InputAction("Return", InputActionType.Button, "<Keyboard>/r");

        // Jump — Space
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");

        // Pilot Seat — T
        pilotSeatAction = new InputAction("PilotSeat", InputActionType.Button, "<Keyboard>/t");
    }
}
