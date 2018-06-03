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
        private BetterCamera2DFollow _follow;
        private readonly ReactiveCollection<IDisposable> _disposableBag = new ReactiveCollection<IDisposable>();
        

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _follow = GetComponentInParent<BetterCamera2DFollow>();
            _shipControls.ZoomStream.Subscribe(Zoom).AddTo(_disposableBag);
            _shipControls.RotateStream.Subscribe(Rotate).AddTo(_disposableBag);
        }

        private void Zoom(float value)
        {
            float scroll = value * 0.5f;
            float newZ = _follow.GetOffsetZ() + scroll;

            _follow.ShiftOffsetZBy(scroll);
        }

        private void Rotate(float value)
        {
            _follow.Rotate(value);
        }
    }
}