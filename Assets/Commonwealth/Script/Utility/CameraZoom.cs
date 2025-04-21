using System;
using Commonwealth.Script.Ship.Hardware;
using Commonwealth.Script.UI;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _boundsCheckMax;
        [SerializeField] private ShipControls _shipControls;

        private Camera _camera;
        private SpriteFollowCamera _spriteFollow;
        private BetterCamera2DFollow _legacyFollow;
        private readonly ReactiveCollection<IDisposable> _disposableBag = new ReactiveCollection<IDisposable>();
        
        // Zoom sensitivity multiplier
        [SerializeField] private float _zoomSensitivity = 1.0f;
        
        // Rotation sensitivity multiplier (only used with legacy camera)
        [SerializeField] private float _rotateSensitivity = 1.0f;
        
        void Awake()
        {
            _camera = GetComponent<Camera>();
            
            // Try to get the new camera controller first
            _spriteFollow = GetComponentInParent<SpriteFollowCamera>();
            
            _shipControls.ZoomStream.Subscribe(Zoom).AddTo(_disposableBag);
            _shipControls.RotateStream.Subscribe(Rotate).AddTo(_disposableBag);
        }

        private void Zoom(float value)
        {
            float zoom = -value * 10.0f * _zoomSensitivity;
            
            if (_spriteFollow != null)
            {
                // Adjust orthographic size for the new camera
                _camera.orthographicSize = Mathf.Clamp(
                    _camera.orthographicSize + zoom * 0.1f,
                    _spriteFollow.minZoom,
                    _spriteFollow.maxZoom
                );
            }
            else if (_legacyFollow != null)
            {
                // Legacy zoom behavior
                _legacyFollow.ShiftOffsetZBy(zoom);
            }
        }

        private void Rotate(float value)
        {
            if (_spriteFollow != null)
            {
                // The new camera doesn't need rotation as it's always perpendicular,
                // but we could adjust the offset based on rotation if needed
                _spriteFollow.SetOffset(_spriteFollow.offsetX + value * 0.1f, _spriteFollow.offsetY);
            }
            else if (_legacyFollow != null)
            {
                // Legacy rotation behavior
                _legacyFollow.Rotate(value * _rotateSensitivity);
            }
        }
    }
}