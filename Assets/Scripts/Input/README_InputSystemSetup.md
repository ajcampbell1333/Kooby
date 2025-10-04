# Virtual Joystick Input System - Complete Setup Guide

## Overview
This system provides a virtual joystick using Unity's modern UI Toolkit (USS/UXML) that works on both touchscreens and PC with mouse input. It uses Unity's new Input System and provides normalized Vector2 output for other systems to consume.

## Prerequisites
- Unity 2022.3 or later
- Input System package installed (Window → Package Manager → Input System)
- UI Toolkit package (usually included by default)

## Files in This System
- `VirtualJoystickUIToolkit.cs` - Main joystick controller using UI Toolkit
- `InputManagerUIToolkit.cs` - Handles Input Actions and coordinates touch/mouse input
- `VirtualJoystick.uxml` - UI structure definition
- `VirtualJoystick.uss` - UI styling and themes
- `ThumbstickConsumer.cs` - Example script showing how to consume joystick input

## Complete Setup Instructions

### Step 1: Install Required Packages
1. Open Unity Package Manager (Window → Package Manager)
2. Install **Input System** package if not already installed
3. UI Toolkit should be included by default

### Step 2: Create Input Action Asset
1. Right-click in Project window → Create → Input Actions
2. Name it "PlayerInputActions"
3. Double-click to open the Input Actions window
4. Create a new Action Map called "Player"
5. Add these Actions to the Player map:

| Action Name     | Type    | Description              |
|-----------------|---------|--------------------------|
| `TouchPress`    | Button  | Touch press detection    |
| `TouchPosition` | Vector2 | Touch position           |
| `TouchDelta`    | Vector2 | Touch movement delta     |
| `MousePress`    | Button  | Mouse click detection    |
| `MousePosition` | Vector2 | Mouse position           |
| `MouseDelta`    | Vector2 | Mouse movement delta     |

### Step 3: Configure Input Action Bindings
For each action, add these bindings:

**TouchPress:**
- Touch (Press)

**TouchPosition:**
- Touch (Position)

**TouchDelta:**
- Touch (Delta)

**MousePress:**
- Mouse (Left Button)

**MousePosition:**
- Mouse (Position)

**MouseDelta:**
- Mouse (Delta)

**Important:** After configuring bindings, simply close the Input Actions window. Do NOT click "Save Asset" as this generates C# code that could conflict with our custom scripts.

### Step 4: Setup UI Toolkit Elements
1. Create a GameObject in your scene (name it "VirtualJoystickUI")
2. Add `UIDocument` component to the GameObject
3. Create a Panel Settings asset (if you don't have one):
   - Right-click in Project → Create → UI Toolkit → Panel Settings Asset
4. In the UIDocument component:
   - Assign `VirtualJoystick.uxml` to the **Source Asset** field
   - Assign the Panel Settings asset to the **Panel Settings** field
5. Add `VirtualJoystickUIToolkit` component to the same GameObject
6. In the VirtualJoystickUIToolkit component:
   - Assign the UIDocument component to the **UI Document** field
   - Assign `VirtualJoystick.uss` to the **Style Sheet** field (optional - script will auto-load if not assigned)
7. The script will automatically find the UI elements by name

### Step 5: Setup Input Manager
1. Create another GameObject in scene (name it "InputManager")
2. Add `InputManagerUIToolkit` component
3. In the InputManagerUIToolkit component:
   - Assign the `PlayerInputActions` asset to the **Input Actions** field
4. The InputManager will automatically find the VirtualJoystickUIToolkit

### Step 6: Test the System
1. Create another empty GameObject (name it "InputTester")
2. Add `ThumbstickConsumer` component
3. In the ThumbstickConsumer component:
   - Assign the `InputManagerUIToolkit` component to the **Input Manager** field
   - (You can drag the InputManager GameObject and it will automatically find the InputManagerUIToolkit component)
4. Play the scene and test:
   - Touch/click anywhere to show joystick
   - Drag to move the joystick knob
   - Release to hide joystick
   - Check console for input logs

## System Architecture

### UI Toolkit Structure
The joystick is built using UI Toolkit's UXML/USS system:

```xml
<engine:VisualElement name="joystick-container" class="joystick-container">
    <engine:VisualElement name="joystick-background" class="joystick-background">
        <engine:VisualElement name="joystick-knob" class="joystick-knob" />
    </engine:VisualElement>
</engine:VisualElement>
```

### USS Classes
- `.joystick-container` - Main container (100x100px, positioned absolutely)
- `.joystick-background` - Outer circle (semi-transparent background)
- `.joystick-knob` - Inner circle (draggable knob)

### Component Flow
1. **InputManagerUIToolkit** receives touch/mouse input from Input Actions
2. **VirtualJoystickUIToolkit** handles UI display and input processing
3. **ThumbstickConsumer** (or your custom scripts) receive normalized Vector2 input

## Usage in Other Scripts

### Method 1: Subscribe to Events
```csharp
public class MyScript : MonoBehaviour
{
    private InputManagerUIToolkit inputManager;
    
    private void Start()
    {
        inputManager = FindObjectOfType<InputManagerUIToolkit>();
        inputManager.OnThumbstickActive.AddListener(OnThumbstickInput);
        inputManager.OnThumbstickReleased.AddListener(OnThumbstickReleased);
    }
    
    private void OnThumbstickInput(Vector2 input)
    {
        // Handle normalized Vector2 input (-1 to 1 range)
        Debug.Log($"Input: {input}");
    }
    
    private void OnThumbstickReleased()
    {
        // Handle input release
        Debug.Log("Input released");
    }
}
```

### Method 2: Poll Input
```csharp
public class MyScript : MonoBehaviour
{
    private InputManagerUIToolkit inputManager;
    
    private void Start()
    {
        inputManager = FindObjectOfType<InputManagerUIToolkit>();
    }
    
    private void Update()
    {
        if (inputManager.IsThumbstickActive())
        {
            Vector2 input = inputManager.GetThumbstickInput();
            // Use input for movement, etc.
        }
    }
}
```

## Features
- **Modern UI System**: Uses Unity's latest UI Toolkit (USS/UXML)
- **Cross-platform**: Works on touchscreens and PC
- **Visual feedback**: Joystick appears on touch/click and fades in/out
- **Normalized output**: Vector2 values range from -1 to 1
- **Dead zone**: Configurable dead zone to prevent accidental input
- **Smooth transitions**: Fade in/out animations
- **Event-driven**: Uses UnityEvents for easy integration
- **Theme support**: Multiple visual themes included
- **Accessibility**: High contrast and accessibility support

## Customization & Theming

### Size Variants
Add these classes to the joystick-container in UXML:
- `.small` - 80x80px joystick
- `.large` - 120x120px joystick

### Color Themes
Add these classes to the joystick-background and joystick-knob:
- `.alt-theme` - Dark theme
- `.high-contrast` - High contrast for accessibility

### Active State
Add `.active` class to show glow effects when joystick is in use.

### Script Settings
In the VirtualJoystickUIToolkit component:
- **Joystick Radius** - Size of the joystick area
- **Dead Zone** - Minimum input threshold (0.1 = 10%)
- **Fade In Duration** - Animation time when showing joystick
- **Fade Out Duration** - Animation time when hiding joystick

## Troubleshooting

### Common Issues

**Joystick doesn't appear:**
- Check that UIDocument has both UXML and USS files assigned
- Verify element names match between UXML and script
- Ensure Input Actions asset is assigned to InputManagerUIToolkit

**Input not working:**
- Make sure Input System package is installed
- Check that Input Actions are properly configured and saved
- Verify InputManagerUIToolkit can find VirtualJoystickUIToolkit

**Console errors:**
- Check for missing element errors in console
- Ensure all required components are added to GameObjects
- Verify Input Actions asset is not corrupted

### Debug Tips
- Enable logging in ThumbstickConsumer to see input values
- Check Unity's Input Debugger (Window → Analysis → Input Debugger)
- Use Unity's UI Toolkit Debugger (Window → UI Toolkit → Debugger)

## Integration with Your Game

To integrate this input system with your game's piece movement:

1. **Subscribe to input events** in your game manager or piece controller
2. **Convert Vector2 input** to your game's coordinate system
3. **Apply movement** to your game objects
4. **Handle input state** (active/inactive) for game logic

Example integration:
```csharp
// In your game manager or piece controller
private void Start()
{
    var inputManager = FindObjectOfType<InputManagerUIToolkit>();
    inputManager.OnThumbstickActive.AddListener(OnPlayerInput);
}

private void OnPlayerInput(Vector2 input)
{
    // Convert input to world movement
    Vector3 movement = new Vector3(input.x, 0, input.y);
    // Apply to your game object
    transform.Translate(movement * moveSpeed * Time.deltaTime);
}
```
