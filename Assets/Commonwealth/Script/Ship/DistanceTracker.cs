using System;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.Ship.Hardware;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.UI;

namespace Commonwealth.Script.Ship
{
    public class DistanceTracker : MonoBehaviour
    {
        [SerializeField] private Text _distanceText;
        [SerializeField] private Text _timeText;
        [SerializeField] private Text _fuelText;
        [SerializeField] private Text _speedText;
        [SerializeField] private Text _thrustText;
        
        //private List<Quaternion> _directions = new List<Quaternion>();
        //private List<float> _speed = new List<float>();
        private float _speed;
        private Vector3 _direction;

        private DateTime? _lastTime;
        private float _totalDistance = 0.0f;
        private float _totalTime = 0.0f;
        private float _fuelRemaining = 0.0f;
        private float _currentThrust = 0.0f;
        private float _currentTripDistance = 0.0f;

        public float CurrentTripDistance
        {
            get { return _currentTripDistance; }
        }

        void Start()
        {
            _speed = 0.0f;
        }
        
        public void OnEngineMetrics(Engine.EngineMetrics metrics)
        {
            _speed = Mathf.Abs(metrics.CurrentSpeed);
            _fuelRemaining = metrics.FuelRemaining;
            _currentThrust = metrics.CurrentThrust;
        }

        public void NewTracking(float speed)
        {
            _speed = speed;
        }

        public void NewSector()
        {
            _currentTripDistance = 0.0f;
        }

        private void Update()
        {
            if (_distanceText != null)
            {
                _distanceText.text = "Total: " + _totalDistance;
            }

            if (_timeText != null)
            {
                _timeText.text = _totalTime + " s";
            }

            if (_fuelText != null)
            {
                _fuelText.text = _fuelRemaining + "kg";
            }

            if (_speedText != null)
            {
                _speedText.text = _speed + "m/s";
            }

            if (_thrustText != null)
            {
                _thrustText.text = _currentThrust + "N";
            }
            
            
        }

        private void FixedUpdate()
        {
            float delta = _speed * Time.fixedDeltaTime;
            _totalDistance += delta;
            _currentTripDistance += delta;
            _totalTime += Time.fixedDeltaTime;

            //v = d / t
            //d = v * t
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log("On App Focus: " + hasFocus);
            HandleSimulationRunning(hasFocus);
        }

        void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log("On App Pause: " + pauseStatus);
            HandleSimulationRunning(!pauseStatus);    
        }

        private void HandleSimulationRunning(bool running)
        {
            if (!running)
            {
                _lastTime = DateTime.Now;
            }
            else
            {
                if (_lastTime.HasValue)
                {
                    float secondsSince = (float) (DateTime.Now - _lastTime.Value).TotalSeconds;
                    Debug.Log("Last time: " + _lastTime + " Current Time: " + Time.timeSinceLevelLoad);
                    _totalDistance += (secondsSince * _speed);
                }
            }
        }
    }
}