# RuntimeGraph UI Toolbar Instructions

## Problem
You're not seeing the Unity runtime UI toolbar for switching between Selection, Create Node, and Create Connection modes.

## Solution Options

### Option 1: Use the Demo Scene (Recommended)
1. In Unity, go to **Tools → Runtime Graph → Open Demo Scene**
2. If the scene doesn't exist, select "Create" when prompted
3. Press **Play** in Unity
4. You should see the toolbar with "Select", "Node", and "Connect" buttons in the top-left corner

### Option 2: Add to Your Current Scene
1. In your current scene, create an empty GameObject
2. Add the **RuntimeGraphBootstrap** component to it
3. Press **Play** in Unity
4. The system will automatically create a RuntimeGraph GameObject with RuntimeGraphUI
5. You should see the toolbar in the top-left corner

### Option 3: Manual Setup
1. In your current scene, create an empty GameObject named "RuntimeGraph"
2. Add the **RuntimeGraphUI** component directly to it
3. Press **Play** in Unity
4. The toolbar should appear in the top-left corner

## Technical Details
- Uses Unity's UI Toolkit (supported in Unity 6.2)
- No UI Camera required - renders directly via UIDocument
- Toolbar appears at top-left with dark background
- Buttons: "Select" (default), "Node", "Connect"

## Troubleshooting
- Make sure you're in **Play Mode** - the UI only appears at runtime
- Check that your scene has either RuntimeGraphUI or RuntimeGraphBootstrap component
- The toolbar has a semi-transparent dark background - it should be visible against most scenes