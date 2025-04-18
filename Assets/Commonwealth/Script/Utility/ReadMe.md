# Camera Utilities

## SpriteFollowCamera

The `SpriteFollowCamera` is a utility for following 2D sprites in Unity. It ensures that the camera's forward direction is always perpendicular to the player sprite's normal, giving a proper 2D view.

### Features

- Smooth camera following with configurable damping
- Look-ahead functionality that anticipates player movement
- Zoom controls via mouse wheel
- Customizable distance and offset from target
- Ensures camera is always oriented correctly for 2D sprites

### Usage

1. Add the `SpriteFollowCamera` component to your camera GameObject.
2. Set the `targetSprite` to the Transform of the sprite you want to follow.
3. Configure the follow settings to your liking:
   - `followSmoothing`: How quickly the camera catches up to the target
   - `distanceFromTarget`: How far the camera is from the 2D plane
   - `offsetX` and `offsetY`: Position offsets from the target
   - `lookAheadAmount`: How much the camera looks ahead of the target's movement

### Example
