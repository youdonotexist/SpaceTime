using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Ship.Hardware
{
    public class Engine : MonoBehaviour, IShipControlable
    {
        [SerializeField] private float _maxThrust = 20.0f;
        [SerializeField] private float _maxSpeed = 1000.0f;
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
        private float _fuelBurnRate = 0.001f; //1kg fuel per 1g thrust
        private EngineMetrics _lastEvent;

        private float _velocity;
        private float _acceleration;

        private readonly Subject<EngineMetrics> _engineMetricsStream = new Subject<EngineMetrics>();

        public struct EngineMetrics
        {
            public float CurrentThrust;
            public float CurrentSpeed;
            public float FuelRemaining;

            public EngineMetrics(float currentThrust, float currentSpeed, float fuelRemaining)
            {
                CurrentThrust = currentThrust;
                CurrentSpeed = currentSpeed;
                FuelRemaining = fuelRemaining;
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

        void Start()
        {
            _ship = GetComponentInParent<Ship>();
            _previousDirection = _ship.GetTransform().position.z;
            _previousPosition = _ship.GetTransform().position;

            _lastEvent = new EngineMetrics(0.0f, 0.0f, GetFuelRemaining());
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

            float mass = 100.0f;
            _acceleration = finalThrust / mass;
            _velocity += _acceleration;

            float speed = _velocity * Time.fixedDeltaTime; 

            trans.position = currentPos + ((speed * _desiredDirection));

            _fuelConverter.SetConverterUtilization((finalThrust / _maxThrust) * _maxThrust);
            _lastEvent = new EngineMetrics(finalThrust, speed, GetFuelRemaining());
            _engineMetricsStream.OnNext(_lastEvent);
            
            _previousPosition = currentPos;
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
                _thrustRatio = -speedDir * 0.5f;
            }
            else if (Mathf.Approximately(speedDir + thrustDir, 0.0f)) //Already opposite
            {
                Debug.Log("Already opposite, just maxxing out");
                _thrustRatio = 0.5f * thrustDir;
            }
            else
            {
                Debug.Log("Reversing Thrust");
                _thrustRatio = -speedDir * 0.5f;
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
    }
}