using UnityEngine;
using UnityEngine.Events;

public class ThumbstickConsumer : MonoBehaviour
{
    [Header("Input Reference")]
    [SerializeField] private InputManagerUIToolkit inputManager;
    
    [Header("Events")]
    public UnityEvent<Vector2> OnInputReceived;
    public UnityEvent OnInputStopped;
    
    [Header("Settings")]
    [SerializeField] private float inputThreshold = 0.1f;
    [SerializeField] private bool logInput = true;
    
    private Vector2 lastInput = Vector2.zero;
    private bool wasInputActive = false;
    
    private void Start()
    {
        // Find InputManagerUIToolkit if not assigned
        if (inputManager == null)
            inputManager = FindObjectOfType<InputManagerUIToolkit>();
        
        if (inputManager == null)
        {
            KoobyLogManager.LogError(LogCategory.UI_Output, "InputManagerUIToolkit not found! ThumbstickConsumer cannot function.");
            return;
        }
        
        // Subscribe to input events
        inputManager.OnThumbstickActive.AddListener(OnThumbstickInput);
        inputManager.OnThumbstickReleased.AddListener(OnThumbstickReleased);
        
        KoobyLogManager.Log(LogCategory.UI_Output, "ThumbstickConsumer initialized and ready to receive input");
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (inputManager != null)
        {
            inputManager.OnThumbstickActive.RemoveListener(OnThumbstickInput);
            inputManager.OnThumbstickReleased.RemoveListener(OnThumbstickReleased);
        }
    }
    
    private void OnThumbstickInput(Vector2 input)
    {
        // Check if input is above threshold
        if (input.magnitude >= inputThreshold)
        {
            lastInput = input;
            wasInputActive = true;
            
            // Invoke the input event
            OnInputReceived?.Invoke(input);
            
            if (logInput)
            {
                KoobyLogManager.Log(LogCategory.UI_Output, $"Thumbstick input received: {input} (magnitude: {input.magnitude:F2})");
            }
        }
    }
    
    private void OnThumbstickReleased()
    {
        if (wasInputActive)
        {
            wasInputActive = false;
            lastInput = Vector2.zero;
            
            // Invoke the input stopped event
            OnInputStopped?.Invoke();
            
            if (logInput)
            {
                KoobyLogManager.Log(LogCategory.UI_Output, "Thumbstick input stopped");
            }
        }
    }
    
    // Public method to get the last input value
    public Vector2 GetLastInput()
    {
        return lastInput;
    }
    
    // Public method to check if input is currently active
    public bool IsInputActive()
    {
        return wasInputActive;
    }
    
    // Public method to get input direction (normalized)
    public Vector2 GetInputDirection()
    {
        return lastInput.normalized;
    }
    
    // Public method to get input magnitude
    public float GetInputMagnitude()
    {
        return lastInput.magnitude;
    }
}
