using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class VirtualJoystick : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform joystickContainer;
    [SerializeField] private RectTransform joystickKnob;
    [SerializeField] private CanvasGroup joystickCanvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float joystickRadius = 50f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float deadZone = 0.1f;
    
    [Header("Events")]
    public UnityEvent<Vector2> OnThumbstickActive;
    public UnityEvent OnThumbstickReleased;
    
    private bool isActive = false;
    private Vector2 joystickCenter;
    private Vector2 currentInput;
    private Coroutine fadeCoroutine;
    
    private void Start()
    {
        // Initially hide the joystick
        if (joystickCanvasGroup != null)
        {
            joystickCanvasGroup.alpha = 0f;
            joystickCanvasGroup.interactable = false;
            joystickCanvasGroup.blocksRaycasts = false;
        }
        
        // Hide the joystick container initially
        if (joystickContainer != null)
            joystickContainer.gameObject.SetActive(false);
    }
    
    public void OnTouchPress(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 touchPosition = context.ReadValue<Vector2>();
            ShowJoystick(touchPosition);
        }
        else if (context.canceled)
        {
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
        
        // Convert screen position to canvas position
        Vector2 canvasPosition = ScreenToCanvasPosition(screenPosition);
        
        // Position the joystick container
        joystickContainer.anchoredPosition = canvasPosition;
        joystickContainer.gameObject.SetActive(true);
        
        // Reset knob position to center
        if (joystickKnob != null)
            joystickKnob.anchoredPosition = Vector2.zero;
        
        // Store the center position
        joystickCenter = canvasPosition;
        currentInput = Vector2.zero;
        isActive = true;
        
        // Fade in
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeJoystick(true));
        
        KoobyLogManager.Log(LogCategory.Manager, $"Virtual joystick activated at screen position {screenPosition}");
    }
    
    private void HideJoystick()
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
        
        KoobyLogManager.Log(LogCategory.Manager, "Virtual joystick deactivated");
    }
    
    private void UpdateJoystickInput(Vector2 delta)
    {
        if (!isActive || joystickKnob == null) return;
        
        // Update the knob position based on delta
        Vector2 newKnobPosition = joystickKnob.anchoredPosition + delta;
        
        // Clamp to joystick radius
        if (newKnobPosition.magnitude > joystickRadius)
        {
            newKnobPosition = newKnobPosition.normalized * joystickRadius;
        }
        
        joystickKnob.anchoredPosition = newKnobPosition;
        
        // Calculate normalized input
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
    
    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return screenPosition;
        
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            canvas.worldCamera,
            out canvasPosition
        );
        
        return canvasPosition;
    }
    
    private IEnumerator FadeJoystick(bool fadeIn)
    {
        if (joystickCanvasGroup == null) yield break;
        
        float startAlpha = joystickCanvasGroup.alpha;
        float targetAlpha = fadeIn ? 1f : 0f;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            joystickCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
        
        joystickCanvasGroup.alpha = targetAlpha;
        
        if (!fadeIn)
        {
            // Hide the container after fade out
            if (joystickContainer != null)
                joystickContainer.gameObject.SetActive(false);
        }
        else
        {
            // Enable interactions after fade in
            joystickCanvasGroup.interactable = true;
            joystickCanvasGroup.blocksRaycasts = true;
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
