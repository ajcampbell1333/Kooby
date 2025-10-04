using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private float orbitSpeed = 30f;
    [SerializeField] private float orbitRadius = 10f;
    [SerializeField] private float orbitHeight = 5f;
    
    [Header("Input")]
    [SerializeField] private InputManagerUIToolkit inputManager;
    
    private Vector3 currentOrbitPosition;
    private float currentOrbitAngle = 0f;
    private bool isOrbiting = false;
    private float currentOrbitSpeed = 0f;
    
    private void Start()
    {
        // Find the input manager if not assigned
        if (inputManager == null)
        {
            inputManager = FindObjectOfType<InputManagerUIToolkit>();
            if (inputManager == null)
            {
                KoobyLogManager.LogError(LogCategory.UI_Output, "CameraOrbit: InputManagerUIToolkit not found!");
                return;
            }
        }
        
        // Subscribe to joystick events
        inputManager.OnThumbstickActive.AddListener(OnThumbstickInput);
        inputManager.OnThumbstickReleased.AddListener(OnThumbstickReleased);
        
        // Initialize camera position
        UpdateCameraPosition();
        
        KoobyLogManager.Log(LogCategory.UI_Output, "CameraOrbit initialized");
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
        // Only use X value for horizontal orbiting
        float horizontalInput = input.x;
        
        if (Mathf.Abs(horizontalInput) > 0.1f) // Dead zone
        {
            isOrbiting = true;
            
            // Calculate and store the orbit speed based on input
            // Use the magnitude of input to control speed (0.1 to 1.0 range)
            float speedMultiplier = Mathf.Abs(horizontalInput);
            currentOrbitSpeed = Mathf.Sign(horizontalInput) * orbitSpeed * speedMultiplier;
            
            KoobyLogManager.Log(LogCategory.UI_Output, $"Camera orbit speed set: {currentOrbitSpeed:F1}°/sec");
        }
        else
        {
            isOrbiting = false;
            currentOrbitSpeed = 0f;
        }
    }
    
    private void OnThumbstickReleased()
    {
        isOrbiting = false;
        currentOrbitSpeed = 0f;
        KoobyLogManager.Log(LogCategory.UI_Output, "Camera orbit stopped");
    }
    
    private void Update()
    {
        // Continuously orbit while active
        if (isOrbiting && Mathf.Abs(currentOrbitSpeed) > 0f)
        {
            // Apply the stored orbit speed
            float angleChange = currentOrbitSpeed * Time.deltaTime;
            currentOrbitAngle += angleChange;
            
            // Update camera position
            UpdateCameraPosition();
        }
    }
    
    private void UpdateCameraPosition()
    {
        if (orbitTarget == null) return;
        
        // Calculate orbit position around the target
        float radians = currentOrbitAngle * Mathf.Deg2Rad;
        currentOrbitPosition = orbitTarget.position + new Vector3(
            Mathf.Sin(radians) * orbitRadius,
            orbitHeight,
            Mathf.Cos(radians) * orbitRadius
        );
        
        // Set camera position and look at target
        transform.position = currentOrbitPosition;
        transform.LookAt(orbitTarget.position);
    }
    
    public void SetOrbitTarget(Transform target)
    {
        orbitTarget = target;
        KoobyLogManager.Log(LogCategory.UI_Output, $"CameraOrbit target set to: {target.name}");
        
        // Update camera position with new target
        UpdateCameraPosition();
    }
}
