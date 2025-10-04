using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UIElements;
using System.Linq;

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
    private bool joystickEnabled = true; // Track if joystick should be enabled
    
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
            // Check if joystick is enabled (not over UI)
            if (!joystickEnabled)
            {
                KoobyLogManager.Log(LogCategory.UI_Input, "Joystick disabled due to UI hover, ignoring activation");
                return;
            }
            
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
        // Continuously check if joystick should be enabled based on UI hover state
        UpdateJoystickEnabledState();
        
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
    
    private void UpdateJoystickEnabledState()
    {
        // Continuously update joystick enabled state based on UI hover
        bool wasEnabled = joystickEnabled;
        joystickEnabled = !IsMouseOverUI();
        
        // Log state changes for debugging
        if (wasEnabled != joystickEnabled)
        {
            KoobyLogManager.Log(LogCategory.UI_Input, $"Joystick enabled state changed: {joystickEnabled}");
        }
    }
    
    private bool IsMouseOverUI()
    {
        // Check if mouse is over any interactive UI Toolkit element by finding all UIDocuments
        var mousePosition = Input.mousePosition;
        var uiDocuments = FindObjectsOfType<UIDocument>();
        
        foreach (var uiDoc in uiDocuments)
        {
            if (uiDoc.rootVisualElement != null)
            {
                // Use the exact same coordinate conversion as the joystick
                var uiPosition = ScreenToUIPosition(mousePosition);
                
                // Debug: Log detailed coordinate conversion AND UI element positions
                Rect gameViewRect = Camera.main.pixelRect;
                Vector2 gameViewPos = new Vector2(mousePosition.x - gameViewRect.x, mousePosition.y - gameViewRect.y);
                float flippedY = gameViewRect.height - gameViewPos.y;
                float percentX = (gameViewPos.x / gameViewRect.width) * 100f;
                float percentY = (flippedY / gameViewRect.height) * 100f;
                
                // Let's also log the actual UI element positions to understand their coordinate system
                var root = uiDoc.rootVisualElement;
                var statusLabel = root.Q<Label>("turnStatusLabel");
                var buttonRow = root.Q<VisualElement>("button-row");
                
                string statusPos = statusLabel != null ? $"Status: ({statusLabel.layout.x:F1}, {statusLabel.layout.y:F1}, {statusLabel.layout.width:F1}x{statusLabel.layout.height:F1})" : "Status: null";
                string buttonPos = buttonRow != null ? $"Buttons: ({buttonRow.layout.x:F1}, {buttonRow.layout.y:F1}, {buttonRow.layout.width:F1}x{buttonRow.layout.height:F1})" : "Buttons: null";
                
                KoobyLogManager.Log(LogCategory.UI_Input, $"COORDINATE DEBUG: Raw mouse: ({mousePosition.x:F1}, {mousePosition.y:F1}) -> GameView: ({gameViewPos.x:F1}, {gameViewPos.y:F1}) -> Flipped: ({gameViewPos.x:F1}, {flippedY:F1}) -> Percent: ({percentX:F1}%, {percentY:F1}%) -> Final: ({uiPosition.x:F1}, {uiPosition.y:F1}) | {statusPos} | {buttonPos}");
                
                var pickedElement = uiDoc.rootVisualElement.panel.Pick(uiPosition);
                if (pickedElement != null)
                {
                    // Debug: Log what element was picked
                    KoobyLogManager.Log(LogCategory.UI_Input, $"Picked element: {pickedElement.GetType().Name}, Name: {pickedElement.name}");
                    
                    // Only consider interactive elements (buttons, etc.)
                    // Check if the element or its parent is a button
                    var currentElement = pickedElement;
                    while (currentElement != null)
                    {
                        // Check if this element is a button
                        if (currentElement is Button)
                        {
                            KoobyLogManager.Log(LogCategory.UI_Input, $"Found button: {currentElement.name}");
                            return true;
                        }
                        currentElement = currentElement.parent;
                    }
                }
            }
        }
        
        return false;
    }
    
    private Vector2 ScreenToUIPosition(Vector2 screenPosition)
    {
        // Get the actual Game View size (not full screen)
        Rect gameViewRect = Camera.main.pixelRect;
        
        // Convert screen position to Game View coordinates
        Vector2 gameViewPos = new Vector2(
            screenPosition.x - gameViewRect.x,
            screenPosition.y - gameViewRect.y
        );
        
        // Flip Y coordinate for UI Toolkit (Game View uses top-left origin, UI Toolkit uses bottom-left)
        float flippedY = gameViewRect.height - gameViewPos.y;
        
        // Fine-tune both axes to 0.80x for precise 10% boundary
        float scaledX = gameViewPos.x * 0.80f;  // Scale X to get 10% boundary
        float scaledY = flippedY * 0.80f;       // Scale Y to get 10% boundary
        
        return new Vector2(scaledX, scaledY);
    }
}
