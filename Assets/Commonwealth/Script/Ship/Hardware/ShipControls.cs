﻿using System;
using System.Collections.Generic;
using Commonwealth.Script.Model;
using Commonwealth.Script.Proc;
using Commonwealth.Script.UI;
using Commonwealth.Script.UI.Sector;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.Ship.Hardware
{
    public interface IShipControlable
    {
        void OnThrustChange(float thrust);
        void OnDirectionChange(Vector3 direction);
    }

    public interface IMapControlable
    {
        void OnDeltaMove(Vector2 delta);
        void OnPinchZoom(float distance);
    }

    public class ShipControls : MonoBehaviour
    {
        [SerializeField] private BetterSlider _speedSlider;
        [SerializeField] private BetterSlider _directionSlider;
        [SerializeField] private Button _stopThrustersButton;
        [SerializeField] private Button _slowToStopButton;
        [SerializeField] private SectorControl _sectorControls;

        private Vector2 _lastPanPosition;
        private int _panFingerId;
        private bool _wasZoomingLastFrame;
        private Vector2[] _lastZoomPositions; // Touch mode only

        private ReactiveCollection<IDisposable> _disposables = new ReactiveCollection<IDisposable>();
        private readonly Subject<Vector2> _panStream = new Subject<Vector2>();
        private readonly Subject<float> _zoomStream = new Subject<float>();

        public IObservable<float> ThrustStream
        {
            get { return _speedSlider.OnValueChangedAsObservable(); }
        }

        public IObservable<Vector3> DirectionStream
        {
            get
            {
                return _directionSlider.OnValueChangedAsObservable()
                    .Select(value => new Vector3(_speedSlider.value, 0.0f, value));
            }
        }

        public IObservable<Unit> StopThrustersStream
        {
            get { return _stopThrustersButton.OnClickAsObservable(); }
        }

        public IObservable<Unit> SlowToStopStream
        {
            get { return _slowToStopButton.OnClickAsObservable(); }
        }

        public IObservable<Unit> PickSectorStream
        {
            get { return _sectorControls.PickSectorButtonStream(); }
        }

        public Subject<Vector2> PanStream
        {
            get { return _panStream; }
        }

        public Subject<float> ZoomStream
        {
            get { return _zoomStream; }
        }

        public IObservable<Model.Sector> NewSectorStream
        {
            get { return _sectorControls.NewSectorStream(); }
        }

        public void ResetThrusterControls()
        {
            _speedSlider.value = Mathf.Lerp(_speedSlider.minValue, _speedSlider.maxValue, 0.5f);
        }

        public void MoveThrusterControl(float val, bool notify)
        {
            _speedSlider.SetValue(val, notify);
        }

        private void Update()
        {
            if (Input.touchSupported && Application.platform != RuntimePlatform.WebGLPlayer)
            {
                HandleTouch();
            }
            else
            {
                HandleMouse();
            }
        }

        void HandleMouse()
        {
            // On mouse down, capture it's position.
            // Otherwise, if the mouse is still down, pan the camera.
            if (Input.GetMouseButtonDown(0))
            {
                _lastPanPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                PanCamera(Input.mousePosition);
            }

            // Check for scrolling to zoom the camera
            _zoomStream.OnNext(Input.GetAxis("Mouse ScrollWheel"));
        }

        void HandleTouch()
        {
            switch (Input.touchCount)
            {
                case 1: // Panning
                    _wasZoomingLastFrame = false;

                    // If the touch began, capture its position and its finger ID.
                    // Otherwise, if the finger ID of the touch doesn't match, skip it.
                    Touch touch = Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began)
                    {
                        _lastPanPosition = touch.position;
                        _panFingerId = touch.fingerId;
                    }
                    else if (touch.fingerId == _panFingerId && touch.phase == TouchPhase.Moved)
                    {
                        PanCamera(touch.position);
                    }

                    break;

                case 2: // Zooming
                    Vector2[] newPositions = new Vector2[] {Input.GetTouch(0).position, Input.GetTouch(1).position};
                    if (!_wasZoomingLastFrame)
                    {
                        _lastZoomPositions = newPositions;
                        _wasZoomingLastFrame = true;
                    }
                    else
                    {
                        // Zoom based on the distance between the new positions compared to the 
                        // distance between the previous positions.
                        float newDistance = Vector2.Distance(newPositions[0], newPositions[1]);
                        float oldDistance = Vector2.Distance(_lastZoomPositions[0], _lastZoomPositions[1]);
                        float offset = newDistance - oldDistance;

                        _zoomStream.OnNext(offset);

                        _lastZoomPositions = newPositions;
                    }

                    break;

                default:
                    _wasZoomingLastFrame = false;
                    break;
            }
        }

        void PanCamera(Vector2 newPanPosition)
        {
            // Determine how much to move the camera
            _panStream.OnNext(_lastPanPosition - newPanPosition);

            // Cache the position
            _lastPanPosition = newPanPosition;
        }

        public void ShowSectorPicker(List<Sector> sectorList)
        {
            _sectorControls.ShowPicker(sectorList);
        }

        public void HideSectorPicker()
        {
            _sectorControls.HidePicker();
        }
    }
}