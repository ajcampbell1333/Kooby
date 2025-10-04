using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class VirtualJoystickUIToolkit : MonoBehaviour
{
    [Header("UI Toolkit References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private string joystickContainerName = "joystick-container";
    [SerializeField] private string joystickKnobName = "joystick-knob";
    [SerializeField] private string styleSheetPath = "Assets/UI/VirtualJoystick.uss";
    
    [Header("Settings")]
    [SerializeField] private float joystickRadius = 50f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("Events")]
    public UnityEvent<Vector2> OnThumbstickActive;
    public UnityEvent OnThumbstickReleased;
    
    private VisualElement joystickContainer;
    private VisualElement joystickKnob;
    private bool isActive = false;
    private Vector2 joystickCenter;
    private Vector2 currentInput;
    private Coroutine fadeCoroutine;
    
    private void Start()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                KoobyLogManager.LogError(LogCategory.Manager, "UIDocument component not found!");
                return;
            }
        }
        
        // Get references to UI elements
        var root = uiDocument.rootVisualElement;
        
        // Load the stylesheet
        if (styleSheet != null)
        {
            root.styleSheets.Add(styleSheet);
        }
        else
        {
            // Fallback: try to load from path using AssetDatabase
            #if UNITY_EDITOR
            var loadedStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
            if (loadedStyleSheet != null)
            {
                root.styleSheets.Add(loadedStyleSheet);
                KoobyLogManager.Log(LogCategory.Manager, "Loaded stylesheet from path: " + styleSheetPath);
            }
            else
            {
                KoobyLogManager.LogWarning(LogCategory.Manager, "Could not load stylesheet from path: " + styleSheetPath);
            }
            #endif
        }
        joystickContainer = root.Q<VisualElement>(joystickContainerName);
        joystickKnob = root.Q<VisualElement>(joystickKnobName);
        
        if (joystickContainer == null)
        {
            KoobyLogManager.LogError(LogCategory.Manager, $"Joystick container '{joystickContainerName}' not found in UXML!");
            return;
        }
        
        if (joystickKnob == null)
        {
            KoobyLogManager.LogError(LogCategory.Manager, $"Joystick knob '{joystickKnobName}' not found in UXML!");
            return;
        }
        
        // Initially hide the joystick
        joystickContainer.style.display = DisplayStyle.None;
        joystickContainer.style.opacity = 0f;
        
        KoobyLogManager.Log(LogCategory.UI_Input, "Virtual Joystick UI Toolkit initialized");
    }
    
    public void OnTouchPress(InputAction.CallbackContext context)
    {
        KoobyLogManager.Log(LogCategory.UI_Input, $"VirtualJoystick OnTouchPress called: {context.phase}");
        if (context.performed)
        {
            Vector2 touchPosition = context.ReadValue<Vector2>();
            KoobyLogManager.Log(LogCategory.UI_Input, $"Showing joystick at position: {touchPosition}");
            ShowJoystick(touchPosition);
        }
        else if (context.canceled)
        {
            KoobyLogManager.Log(LogCategory.UI_Input, "Hiding joystick");
            HideJoystick();
        }
    }
    
    public void OnTouchDelta(InputAction.CallbackContext context)
    {
        if (!isActive) return;
        
        Vector2 delta = context.ReadValue<Vector2>();
        UpdateJoystickInput(delta);
    }
    
    private void ShowJoystick(Vector2 screenPosition)
    {
        if (joystickContainer == null) return;
        
        // Convert screen position to UI Toolkit coordinates
        Vector2 uiPosition = ScreenToUIPosition(screenPosition);
        
        // Position the joystick container (center it on the mouse position)
        // Try percentage-based positioning to handle coordinate system scaling
        joystickContainer.style.position = Position.Absolute;
        joystickContainer.style.left = new StyleLength(new Length((uiPosition.x - joystickRadius) / Screen.width * 100, LengthUnit.Percent));
        joystickContainer.style.top = new StyleLength(new Length((uiPosition.y - joystickRadius) / Screen.height * 100, LengthUnit.Percent));
        
        Rect gameViewRect = Camera.main.pixelRect;
        KoobyLogManager.Log(LogCategory.UI_Input, $"JOYSTICK DEBUG: Screen({screenPosition.x:F1},{screenPosition.y:F1}) -> UI({uiPosition.x:F1},{uiPosition.y:F1}) -> Container({uiPosition.x - joystickRadius:F1},{uiPosition.y - joystickRadius:F1}) -> Actual({joystickContainer.style.left.value.value:F1},{joystickContainer.style.top.value.value:F1}) | Radius:{joystickRadius} | ScreenSize:{Screen.width}x{Screen.height} | GameViewSize:{gameViewRect.width}x{gameViewRect.height} | YOffset:{screenPosition.y - uiPosition.y:F1}");
        
        // Reset knob position to center
        if (joystickKnob != null)
        {
            joystickKnob.style.left = joystickRadius - 25f; // Center the knob (assuming 50px knob size)
            joystickKnob.style.top = joystickRadius - 25f;
        }
        
        // Store the center position
        joystickCenter = uiPosition;
        currentInput = Vector2.zero;
        isActive = true;
        
        // Show and fade in
        joystickContainer.style.display = DisplayStyle.Flex;
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeJoystick(true));
        
        KoobyLogManager.Log(LogCategory.UI_Input, $"Virtual joystick activated at screen position {screenPosition}");
    }
    
    public void ShowJoystickAtPosition(Vector2 screenPosition)
    {
        ShowJoystick(screenPosition);
    }
    
    public void HideJoystick()
    {
        if (!isActive) return;
        
        isActive = false;
        currentInput = Vector2.zero;
        
        // Fade out
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeJoystick(false));
        
        // Notify that joystick was released
        OnThumbstickReleased?.Invoke();
        
        KoobyLogManager.Log(LogCategory.UI_Input, "Virtual joystick deactivated");
    }
    
    public void UpdateJoystickFromDelta(Vector2 delta)
    {
        UpdateJoystickInput(delta);
    }
    
    
    private void UpdateJoystickInput(Vector2 delta)
    {
        if (!isActive || joystickKnob == null) return;
        
        // Get current knob position relative to container center
        Vector2 currentKnobPos = new Vector2(
            joystickKnob.style.left.value.value - joystickRadius + 25f, // Convert to center-relative
            joystickKnob.style.top.value.value - joystickRadius + 25f
        );
        
        // Apply delta to current position (flip Y for correct movement direction)
        Vector2 adjustedDelta = new Vector2(delta.x, -delta.y);
        Vector2 newKnobPosition = currentKnobPos + adjustedDelta;
        
        // Clamp to joystick radius
        if (newKnobPosition.magnitude > joystickRadius)
        {
            newKnobPosition = newKnobPosition.normalized * joystickRadius;
        }
        
        // Apply the new position (convert back to absolute position)
        joystickKnob.style.left = newKnobPosition.x + joystickRadius - 25f;
        joystickKnob.style.top = newKnobPosition.y + joystickRadius - 25f;
        
        // Calculate normalized input (use original delta for input, not adjusted)
        Vector2 normalizedInput = newKnobPosition / joystickRadius;
        
        // Apply dead zone
        if (normalizedInput.magnitude < deadZone)
        {
            normalizedInput = Vector2.zero;
        }
        
        // Update current input
        currentInput = normalizedInput;
        
        // Send the input event
        OnThumbstickActive?.Invoke(currentInput);
    }
    
    private Vector2 ScreenToUIPosition(Vector2 screenPosition)
    {
        // Get the actual Game View size (not full screen)
        Rect gameViewRect = Camera.main.pixelRect;
        float centerY = gameViewRect.height / 2f;
        float flippedY = centerY - (screenPosition.y - centerY);
        
        // Apply fixed offset correction (adjust these values based on testing)
        float offsetX = -10f; // Move left to correct 10px right offset
        float offsetY = -10f; // Move up to correct 10px down offset
        
        return new Vector2(screenPosition.x + offsetX, flippedY + offsetY);
    }
    
    private IEnumerator FadeJoystick(bool fadeIn)
    {
        if (joystickContainer == null) yield break;
        
        float startAlpha = joystickContainer.style.opacity.value;
        float targetAlpha = fadeIn ? 1f : 0f;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            joystickContainer.style.opacity = currentAlpha;
            yield return null;
        }
        
        joystickContainer.style.opacity = targetAlpha;
        
        if (!fadeIn)
        {
            // Hide the container after fade out
            joystickContainer.style.display = DisplayStyle.None;
        }
    }
    
    // Public method to get current input (for other systems)
    public Vector2 GetCurrentInput()
    {
        return currentInput;
    }
    
    // Public method to check if joystick is active
    public bool IsActive()
    {
        return isActive;
    }
}
