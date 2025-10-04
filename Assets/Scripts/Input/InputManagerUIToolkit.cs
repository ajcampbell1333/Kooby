using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class InputManagerUIToolkit : MonoBehaviour
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
    
    private VirtualJoystickUIToolkit virtualJoystick;
    private bool isUsingTouch = false;
    private bool isUsingMouse = false;
    
    private void Awake()
    {
        // Get the virtual joystick component
        virtualJoystick = FindObjectOfType<VirtualJoystickUIToolkit>();
        if (virtualJoystick == null)
        {
            KoobyLogManager.LogError(LogCategory.UI_Input, "VirtualJoystickUIToolkit component not found in scene!");
            return;
        }
        
        // Subscribe to virtual joystick events
        virtualJoystick.OnThumbstickActive.AddListener(OnThumbstickActive.Invoke);
        virtualJoystick.OnThumbstickReleased.AddListener(OnThumbstickReleased.Invoke);
    }
    
    private void OnEnable()
    {
        KoobyLogManager.Log(LogCategory.UI_Input, "InputManagerUIToolkit OnEnable called");
        if (inputActions == null)
        {
            KoobyLogManager.LogError(LogCategory.UI_Input, "Input Actions asset not assigned!");
            return;
        }
        
        // Get actions from the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            KoobyLogManager.LogError(LogCategory.UI_Input, "Player action map not found in Input Actions asset!");
            return;
        }
        
        KoobyLogManager.Log(LogCategory.UI_Input, "Player action map found, enabling it...");
        
        // Get individual actions
        touchPressAction = playerActionMap.FindAction("TouchPress");
        touchPositionAction = playerActionMap.FindAction("TouchPosition");
        touchDeltaAction = playerActionMap.FindAction("TouchDelta");
        
        mousePressAction = playerActionMap.FindAction("MousePress");
        mousePositionAction = playerActionMap.FindAction("MousePosition");
        mouseDeltaAction = playerActionMap.FindAction("MouseDelta");
        
        // Enable the action map
        playerActionMap.Enable();
        KoobyLogManager.Log(LogCategory.UI_Input, "Player action map enabled");
        
        // Subscribe to input events
        if (touchPressAction != null)
        {
            touchPressAction.performed += OnTouchInput;
            touchPressAction.canceled += OnTouchInput;
            KoobyLogManager.Log(LogCategory.UI_Input, "TouchPress action subscribed");
        }
        else
        {
            KoobyLogManager.LogError(LogCategory.UI_Input, "TouchPress action not found!");
        }
        
        if (mousePressAction != null)
        {
            mousePressAction.performed += OnMouseInput;
            mousePressAction.canceled += OnMouseInput;
            KoobyLogManager.Log(LogCategory.UI_Input, "MousePress action subscribed");
        }
        else
        {
            KoobyLogManager.LogError(LogCategory.UI_Input, "MousePress action not found!");
        }
        
        KoobyLogManager.Log(LogCategory.UI_Input, "Input Manager UI Toolkit initialized with touch and mouse support");
        KoobyLogManager.Log(LogCategory.UI_Input, "Input Manager UI Toolkit initialization complete");
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
        KoobyLogManager.Log(LogCategory.UI_Input, $"OnTouchInput called: {context.phase}");
        if (context.performed)
        {
            isUsingTouch = true;
            isUsingMouse = false;
            
            // Get touch position and show joystick
            Vector2 touchPosition = touchPositionAction.ReadValue<Vector2>();
            virtualJoystick.OnTouchPress(context);
            
            KoobyLogManager.Log(LogCategory.UI_Input, $"Touch input detected at {touchPosition}");
        }
        else if (context.canceled)
        {
            virtualJoystick.OnTouchPress(context);
        }
    }
    
    private void OnMouseInput(InputAction.CallbackContext context)
    {
        KoobyLogManager.Log(LogCategory.UI_Input, $"OnMouseInput called: {context.phase}");
        if (context.performed)
        {
            isUsingMouse = true;
            isUsingTouch = false;
            
            // Get mouse position from the separate MousePosition action
            Vector2 mousePosition = mousePositionAction.ReadValue<Vector2>();
            
            // Also get the actual screen position for comparison
            Vector3 screenPos = Input.mousePosition;
            Vector2 actualScreenPos = new Vector2(screenPos.x, screenPos.y);
            
            KoobyLogManager.Log(LogCategory.UI_Input, $"Mouse input detected at {mousePosition}");
            KoobyLogManager.Log(LogCategory.UI_Input, $"Actual screen position: {actualScreenPos}");
            
            // Show joystick at actual screen position (not Input System position)
            virtualJoystick.ShowJoystickAtPosition(actualScreenPos);
        }
        else if (context.canceled)
        {
            virtualJoystick.HideJoystick();
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
                virtualJoystick.UpdateJoystickFromDelta(touchDelta);
            }
        }
        else if (isUsingMouse && mouseDeltaAction != null)
        {
            Vector2 mouseDelta = mouseDeltaAction.ReadValue<Vector2>();
            if (mouseDelta != Vector2.zero)
            {
                virtualJoystick.UpdateJoystickFromDelta(mouseDelta);
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
