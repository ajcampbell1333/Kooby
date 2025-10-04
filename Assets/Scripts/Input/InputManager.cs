using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;
    
    [Header("Events")]
    public UnityEvent<Vector2> OnThumbstickActive;
    public UnityEvent OnThumbstickReleased;
    
    private InputAction touchPressAction;
    private InputAction touchPositionAction;
    private InputAction touchDeltaAction;
    private InputAction mousePressAction;
    private InputAction mousePositionAction;
    private InputAction mouseDeltaAction;
    
    private VirtualJoystick virtualJoystick;
    private bool isUsingTouch = false;
    private bool isUsingMouse = false;
    
    private void Awake()
    {
        // Get the virtual joystick component
        virtualJoystick = FindObjectOfType<VirtualJoystick>();
        if (virtualJoystick == null)
        {
            KoobyLogManager.LogError(LogCategory.Manager, "VirtualJoystick component not found in scene!");
            return;
        }
        
        // Subscribe to virtual joystick events
        virtualJoystick.OnThumbstickActive.AddListener(OnThumbstickActive.Invoke);
        virtualJoystick.OnThumbstickReleased.AddListener(OnThumbstickReleased.Invoke);
    }
    
    private void OnEnable()
    {
        if (inputActions == null)
        {
            KoobyLogManager.LogError(LogCategory.Manager, "Input Actions asset not assigned!");
            return;
        }
        
        // Get actions from the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            KoobyLogManager.LogError(LogCategory.Manager, "Player action map not found in Input Actions asset!");
            return;
        }
        
        // Get individual actions
        touchPressAction = playerActionMap.FindAction("TouchPress");
        touchPositionAction = playerActionMap.FindAction("TouchPosition");
        touchDeltaAction = playerActionMap.FindAction("TouchDelta");
        
        mousePressAction = playerActionMap.FindAction("MousePress");
        mousePositionAction = playerActionMap.FindAction("MousePosition");
        mouseDeltaAction = playerActionMap.FindAction("MouseDelta");
        
        // Enable the action map
        playerActionMap.Enable();
        
        // Subscribe to input events
        if (touchPressAction != null)
        {
            touchPressAction.performed += OnTouchInput;
            touchPressAction.canceled += OnTouchInput;
        }
        
        if (mousePressAction != null)
        {
            mousePressAction.performed += OnMouseInput;
            mousePressAction.canceled += OnMouseInput;
        }
        
        KoobyLogManager.Log(LogCategory.Manager, "Input Manager initialized with touch and mouse support");
    }
    
    private void OnDisable()
    {
        // Unsubscribe from input events
        if (touchPressAction != null)
        {
            touchPressAction.performed -= OnTouchInput;
            touchPressAction.canceled -= OnTouchInput;
        }
        
        if (mousePressAction != null)
        {
            mousePressAction.performed -= OnMouseInput;
            mousePressAction.canceled -= OnMouseInput;
        }
        
        // Disable the action map
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap != null)
                playerActionMap.Disable();
        }
    }
    
    private void OnTouchInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isUsingTouch = true;
            isUsingMouse = false;
            
            // Get touch position and show joystick
            Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();
            virtualJoystick.OnTouchPress(context);
            
            KoobyLogManager.Log(LogCategory.Manager, $"Touch input detected at {touchPosition}");
        }
        else if (context.canceled)
        {
            virtualJoystick.OnTouchPress(context);
        }
    }
    
    private void OnMouseInput(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isUsingMouse = true;
            isUsingTouch = false;
            
            // Get mouse position and show joystick
            Vector2 mousePosition = mousePositionAction.ReadValue<Vector2>();
            virtualJoystick.OnTouchPress(context);
            
            KoobyLogManager.Log(LogCategory.Manager, $"Mouse input detected at {mousePosition}");
        }
        else if (context.canceled)
        {
            virtualJoystick.OnTouchPress(context);
        }
    }
    
    private void Update()
    {
        if (virtualJoystick == null || !virtualJoystick.IsActive()) return;
        
        // Handle continuous input (delta movement)
        if (isUsingTouch && touchDeltaAction != null)
        {
            Vector2 touchDelta = touchDeltaAction.ReadValue<Vector2>();
            if (touchDelta != Vector2.zero)
            {
                virtualJoystick.OnTouchDelta(new InputAction.CallbackContext());
            }
        }
        else if (isUsingMouse && mouseDeltaAction != null)
        {
            Vector2 mouseDelta = mouseDeltaAction.ReadValue<Vector2>();
            if (mouseDelta != Vector2.zero)
            {
                virtualJoystick.OnTouchDelta(new InputAction.CallbackContext());
            }
        }
    }
    
    // Public method to get current input
    public Vector2 GetThumbstickInput()
    {
        return virtualJoystick != null ? virtualJoystick.GetCurrentInput() : Vector2.zero;
    }
    
    // Public method to check if input is active
    public bool IsThumbstickActive()
    {
        return virtualJoystick != null && virtualJoystick.IsActive();
    }
}
