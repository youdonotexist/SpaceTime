using System;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Ship.Hardware
{
    public class Engine : MonoBehaviour, IShipControlable
    {
        [SerializeField] private float _maxThrust = 20.0f;
        [SerializeField] private float _maxSpeed = 40.0f;
        [SerializeField] private float _fuelBurnRate = 0.001f; //1kg fuel per 1g thrust
        [SerializeField] private Animator _converter;

        [SerializeField] private FuelContainer[] _fuelContainers;
        [SerializeField] private FuelConverter _fuelConverter;

        private Ship _ship;

        private float _thrustRatio;
        private Vector3 _desiredDirection;
        private float _previousDirection;
        private Vector3 _previousVelocity = Vector3.zero;
        private Vector3 _previousPosition;
        private float _previousSpeed;
        
        private EngineMetrics _lastEvent;

        private float _velocity;
        private float _acceleration;

        private readonly Subject<EngineMetrics> _engineMetricsStream = new Subject<EngineMetrics>();

        public struct EngineMetrics
        {
            public float CurrentThrust;
            public float CurrentSpeed;
            public float FuelRemaining;
            public float MaxThrust;
            public float ShipMass;
            public float MaxVelocity;
            public float DistanceTraveled;

            public EngineMetrics(float currentThrust, float currentSpeed, float fuelRemaining, float maxThrustWithFuel, float shipMass, float maxVelocity, float distTraveled)
            {
                CurrentThrust = currentThrust;
                CurrentSpeed = currentSpeed;
                FuelRemaining = fuelRemaining;
                MaxThrust = maxThrustWithFuel;
                ShipMass = shipMass;
                MaxVelocity = maxVelocity;
                DistanceTraveled = distTraveled;
            }

            public override string ToString()
            {
                return "CT: " + CurrentThrust + ", CS: " + CurrentSpeed + ", FR: " + FuelRemaining;
            }
        }

        public float MaxSpeed
        {
            get { return _maxSpeed; }
        }

        public float CurrentThrust
        {
            get { return _lastEvent.CurrentThrust; }
        }

        public float ThrustRatio
        {
            get { return _thrustRatio; }
        }

        public float MaxThrust
        {
            get { return _maxThrust; }
        }

        public float Velocity
        {
            get { return _velocity; }
        }

        public float PreviousSpeed
        {
            get { return _previousSpeed; }
        }

        void Start()
        {
            _ship = GetComponentInParent<Ship>();
            _previousDirection = _ship.GetTransform().position.z;
            _previousPosition = _ship.GetTransform().position;

            _lastEvent = BuildEngineMetrics(0.0f, 0.0f, 0.0f);
            _engineMetricsStream.OnNext(_lastEvent);
        }

        private void FixedUpdate()
        {
            float newThrust = _thrustRatio * _maxThrust;
            float fuelRequired = _fuelBurnRate * newThrust;
            float finalThrust = AcquireFuel(Mathf.Abs(fuelRequired)) * newThrust;

            if (Mathf.Approximately(finalThrust, 0.0f))
            {
                finalThrust = 0.0f;
            }
            
            Transform trans = _ship.GetTransform();
            Vector3 currentPos = trans.position;

            float mass = _ship.CalculateMass();
            _acceleration = finalThrust / mass;
            _velocity += _acceleration;

            float speed = _velocity * Time.fixedDeltaTime; 

            trans.position = currentPos + ((speed * _desiredDirection));

            _fuelConverter.SetConverterUtilization((finalThrust / _maxThrust) * 100.0f);
            _lastEvent = BuildEngineMetrics(finalThrust, speed, Vector3.Distance(trans.position, _previousPosition));
            _engineMetricsStream.OnNext(_lastEvent);
            
            _previousPosition = trans.position;
            _previousSpeed = speed;
        }
        
        public void OnThrustChange(float thrust)
        {
            _thrustRatio = thrust;
        }

        public void InvertThrust(bool full)
        {
            float speedDir = Mathf.Sign(_lastEvent.CurrentSpeed);
            float thrustDir = Mathf.Sign(_lastEvent.CurrentThrust);

            if (Mathf.Approximately(_lastEvent.CurrentThrust, 0.0f)) //no thrust
            {
                Debug.Log("No Thrust, reversing speed: " + _lastEvent.CurrentThrust);
                _thrustRatio = -speedDir * 1.0f;
            }
            else if (Mathf.Approximately(speedDir + thrustDir, 0.0f)) //Already opposite
            {
                Debug.Log("Already opposite, just maxxing out");
                _thrustRatio = 1.0f * thrustDir;
            }
            else
            {
                Debug.Log("Reversing Thrust");
                _thrustRatio = -speedDir * 1.0f;
            }
        }

        public void OnDirectionChange(Vector3 direction)
        {
            _desiredDirection = direction;
            //_desiredZ += (_maxThrust * dir);
        }

        public IObservable<EngineMetrics> GetEngineMetrics()
        {
            return _engineMetricsStream;
        }
        
        private float AuthorizeFuel(float requestedAmount)
        {
            foreach (FuelContainer container in _fuelContainers)
            {
                float receivedPercentage = container.AuthorizeFuel(requestedAmount);
                if (receivedPercentage > 0.0f)
                {
                    return receivedPercentage;
                }
            }

            return 0.0f;
        }

        private float AcquireFuel(float requestedAmount)
        {
            foreach (FuelContainer container in _fuelContainers)
            {
                float receivedPercentage = container.AcquireFuel(requestedAmount);
                if (receivedPercentage > 0.0f)
                {
                    return receivedPercentage;
                }
            }

            return 0.0f;
        }

        private float GetFuelRemaining()
        {
            float remaining = 0.0f;
            foreach (FuelContainer container in _fuelContainers)
            {
                remaining += container.AvailableFuel;
            }

            return remaining;
        }

        public void __Freeze()
        {
            _velocity = 0.0f;
            _acceleration = 0.0f;
        }

        private EngineMetrics BuildEngineMetrics(float finalThrust, float speed, float deltaDistance)
        {
            return new EngineMetrics(finalThrust, speed, GetFuelRemaining(), CalculateMaximumThrustWithFuel(), _ship.CalculateMass(), _maxSpeed, deltaDistance);
        }

        private float CalculateMaximumThrustWithFuel()
        {
            float fuelRequired = _fuelBurnRate * _maxThrust;
            float finalThrust = AuthorizeFuel(Mathf.Abs(fuelRequired)) * _maxThrust;

            return finalThrust;
        }
    }
}