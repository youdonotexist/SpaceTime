using UnityEngine;

namespace Commonwealth.Script.Utility
{
    /// <summary>
    /// Camera utility that follows a 2D sprite in Unity, keeping the camera forward
    /// direction perpendicular to the sprite's normal (i.e., the camera looks directly at the 2D plane).
    /// </summary>
    public class SpriteFollowCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The sprite transform to follow")]
        public Transform targetSprite;
        
        [Header("Follow Settings")]
        [Tooltip("How fast the camera follows the target")]
        public float followSmoothing = 0.125f;
        
        [Tooltip("Distance from the camera to the 2D plane")]
        public float distanceFromTarget = 10f;
        
        [Tooltip("Offset from the target's position on the X axis")]
        public float offsetX = 0f;
        
        [Tooltip("Offset from the target's position on the Y axis")]
        public float offsetY = 0f;
        
        [Header("Look Ahead Settings")]
        [Tooltip("How far ahead of the target's movement to look")]
        public float lookAheadAmount = 2f;
        
        [Tooltip("Smoothing applied to look ahead movement")]
        public float lookAheadSmoothing = 0.5f;
        
        [Tooltip("Minimum movement threshold to trigger look ahead")]
        public float lookAheadThreshold = 0.1f;
        
        [Header("Zoom Settings")]
        [Tooltip("Minimum allowed orthographic size")]
        public float minZoom = 3f;
        
        [Tooltip("Maximum allowed orthographic size")]
        public float maxZoom = 100f;
        
        [Tooltip("Mouse wheel zoom speed")]
        public float zoomSpeed = 1f;
        
        // Internal state variables
        private Vector3 _lastTargetPosition;
        private Vector3 _currentVelocity;
        private Vector3 _lookAheadPos;
        private Camera _camera;
        [SerializeField]
        private float _currentZoom = 100.0f;
        
        // Initialize the camera
        private void Start()
        {
            if (targetSprite == null)
            {
                Debug.LogError("No target sprite assigned to SpriteFollowCamera!");
                enabled = false;
                return;
            }
            
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    Debug.LogError("No camera component found!");
                    enabled = false;
                    return;
                }
            }
            
            // Set initial values
            _lastTargetPosition = targetSprite.position;
            _currentZoom = _camera.orthographicSize;
            
            // Ensure the camera is orthographic for 2D following
            if (!_camera.orthographic)
            {
                Debug.LogWarning("Camera is not orthographic! Setting to orthographic for proper 2D following.");
                _camera.orthographic = true;
            }
            
            // Detach the camera from any parent to prevent unexpected movement
            transform.parent = null;
            
            // Set the initial camera position and rotation
            UpdateCameraPosition(true);
        }
        
        private void LateUpdate()
        {
            if (targetSprite == null)
                return;
                
            // Handle zoom input
            HandleZoomInput();
            
            // Update camera position with smoothing
            UpdateCameraPosition(false);
        }
        
        private void UpdateCameraPosition(bool immediate)
        {
            // Calculate movement delta for look-ahead effect
            Vector3 movementDelta = targetSprite.position - _lastTargetPosition;
            float movementDeltaMagnitude = movementDelta.magnitude;
            
            // Calculate look-ahead position
            if (movementDeltaMagnitude > lookAheadThreshold)
            {
                // When moving, look ahead in the direction of movement
                _lookAheadPos = lookAheadAmount * movementDelta.normalized;
            }
            else
            {
                // When relatively stationary, gradually reset look-ahead
                _lookAheadPos = Vector3.Lerp(_lookAheadPos, Vector3.zero, Time.deltaTime * lookAheadSmoothing);
            }
            
            // Calculate target position with offsets
            Vector3 targetPosition = targetSprite.position + new Vector3(offsetX, offsetY, 0f);
            
            // Add look-ahead to target position
            Vector3 aheadTargetPos = targetPosition + _lookAheadPos;
            
            // Set the z-coordinate to maintain proper distance
            aheadTargetPos.z = targetPosition.z - distanceFromTarget;
            
            // Move camera to target position with smoothing
            Vector3 newPosition;
            if (immediate)
            {
                newPosition = aheadTargetPos;
                _currentVelocity = Vector3.zero;
            }
            else
            {
                newPosition = Vector3.SmoothDamp(transform.position, aheadTargetPos, ref _currentVelocity, followSmoothing);
            }
            
            // Update camera position
            transform.position = newPosition;
            
            // Ensure camera is looking perpendicular to the 2D plane
            // For a standard 2D game, this means looking along the negative Z axis
            transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            
            // Update last position for next frame
            _lastTargetPosition = targetSprite.position;
        }
        
        private void HandleZoomInput()
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0)
            {
                // Adjust zoom level based on scroll input
                _currentZoom -= scrollInput * zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
                
                // Apply zoom
                //_camera.orthographicSize = _currentZoom;
            }
        }
        
        // Public methods to adjust camera settings during runtime
        
        /// <summary>
        /// Set the distance from the camera to the 2D plane
        /// </summary>
        public void SetDistance(float distance)
        {
            distanceFromTarget = Mathf.Max(0.1f, distance);
        }
        
        /// <summary>
        /// Adjust the current distance by the specified amount
        /// </summary>
        public void AdjustDistance(float amount)
        {
            distanceFromTarget = Mathf.Max(0.1f, distanceFromTarget + amount);
        }
        
        /// <summary>
        /// Set the horizontal and vertical offset from the target
        /// </summary>
        public void SetOffset(float x, float y)
        {
            offsetX = x;
            offsetY = y;
        }
        
        /// <summary>
        /// Set the zoom level (orthographic size)
        /// </summary>
        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            _camera.orthographicSize = _currentZoom;
        }
        
        /// <summary>
        /// Set a new target to follow
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            if (newTarget != null)
            {
                targetSprite = newTarget;
                _lastTargetPosition = targetSprite.position;
            }
        }
    }
}
