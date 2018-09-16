using System;
using Commonwealth.Script.Model;
using Commonwealth.Script.Ship.Hardware;
using UnityEngine;
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
        [SerializeField] private Text _destinationText;
        [SerializeField] private Text _fuelEfficiencyText;

        //private List<Quaternion> _directions = new List<Quaternion>();
        //private List<float> _speed = new List<float>();
        private float _speed;
        private Vector3 _direction;
        private Sector _sector;

        private DateTime? _lastTime;
        private float _totalDistance = 0.0f;
        private float _totalTime = 0.0f;
        private float _fuelRemaining = 0.0f;
        private float _currentThrust = 0.0f;
        private float _currentTripDistance;
        private float _totalTripDistance;
        private DateTime? _timeToCurrentDestination;
        
        private DateTime? _firstToCurrentDestination;

        private DateTime? _departureDate = null;

        public float CurrentTripDistance => _currentTripDistance;

        public void OnEngineMetrics(Engine.EngineMetrics metrics)
        {
            //bool recalculateTime = !Mathf.Approximately(_currentThrust, metrics.CurrentThrust);

            _speed = Mathf.Abs(metrics.CurrentSpeed);
            _fuelRemaining = metrics.FuelRemaining;
            _currentThrust = metrics.CurrentThrust;
            _currentTripDistance += metrics.DistanceTraveled;
            
            //Debug.Log("Distance traveled: " + metrics.DistanceTraveled);

            if (_sector == null) return;

            //if (recalculateTime)
            //{
            float accel = metrics.MaxThrust / metrics.ShipMass;
            float decel = -metrics.MaxThrust / metrics.ShipMass;

            float initialVelocity = _speed * _speed ;// * Time.deltaTime;
            float twoaso = -2 * accel * 0.0f;
            float twoabs = 2 * decel * (_totalTripDistance - _currentTripDistance);
            float twoaminus2ab = (2 * accel) - (2 * decel);

           //Debug.Log("Initial v: " + initialVelocity + " accel:" + accel + " decel: " + decel);

            float distanceToApplyOppositeForce = -((initialVelocity + twoaso + twoabs) / twoaminus2ab);

            //Debug.Log("Distance to apply opp force: " + distanceToApplyOppositeForce + " distance left: " +
                      //(_totalTripDistance - _currentTripDistance));

            if (distanceToApplyOppositeForce <= 0.0f) //We're already past
            {
                //Debug.Log("Short dist: " + distanceToApplyOppositeForce);
                float timeToStop = -_speed / decel;
                _timeToCurrentDestination = DateTime.Now.AddSeconds(timeToStop);
            }
            else
            {
                float accelTime = Mathf.Pow((2 * distanceToApplyOppositeForce) / accel, 0.5f);
                float timeToStop = (-accel * accelTime) / decel;
                if (_firstToCurrentDestination == null)
                {
                    _firstToCurrentDestination = DateTime.Now.AddSeconds(accelTime + timeToStop);
                }
                _timeToCurrentDestination = DateTime.Now.AddSeconds(accelTime + timeToStop);

                Debug.Log("Scheduled for: " + _timeToCurrentDestination);
                /*UnityEngine.iOS.LocalNotification not =
                    new UnityEngine.iOS.LocalNotification
                    {
                        alertBody = "Arrived @ " + _sector.Name,
                        fireDate = _timeToCurrentDestination.Value,
                        timeZone = TimeZone.CurrentTimeZone.StandardName
                    };
                UnityEngine.iOS.NotificationServices.CancelAllLocalNotifications();
                UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(not);*/
            }

            //}
        }

        public void NewTracking(float speed)
        {
            _speed = speed;
        }

        public void NewSector(Sector sector)
        {
            _sector = sector;
            _currentTripDistance = 0.0f;
            _totalTripDistance = sector.Distance;
        }

        private void Update()
        {
            if (_distanceText != null)
            {
                _distanceText.text = "Total Distance: " + _totalDistance;
            }

            if (_timeText != null)
            {
                _timeText.text = "Total Time: " + _totalTime + " s";
            }

            if (_fuelText != null)
            {
                _fuelText.text = "Fuel Remaining: " + _fuelRemaining + "kg";
            }

            if (_speedText != null)
            {
                _speedText.text = "Speed: " + _speed + "m/s";
            }

            if (_thrustText != null)
            {
                _thrustText.text = "Current Thrust: " + _currentThrust + "N";
            }

            if (_fuelEfficiencyText != null)
            {
                _fuelEfficiencyText.text = "Fuel Efficiency: " + "1x";
            }

            if (_destinationText != null)
            {
                _destinationText.text = "ETA: " + (_timeToCurrentDestination.HasValue
                                            ? _timeToCurrentDestination + "\n" + _firstToCurrentDestination + "\n" + DateTime.Now
                                            : "[No Destination]");
            }
            
            
        }

        private void FixedUpdate()
        {
            float delta = _speed;
            _totalDistance += delta;
            //_currentTripDistance += delta;
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