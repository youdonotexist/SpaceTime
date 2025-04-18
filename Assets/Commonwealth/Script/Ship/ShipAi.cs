using System;
using Commonwealth.Script.Generation;
using Commonwealth.Script.Life;
using Commonwealth.Script.Model;
using Commonwealth.Script.Proc;
using Commonwealth.Script.Ship.EngineMod;
using Commonwealth.Script.Ship.Hardware;
using Commonwealth.Script.Ship.Monitors;
using Commonwealth.Script.Utility;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Ship
{
    public class ShipAi : MonoBehaviour, IShipAi
    {
        //User Controls
        private ShipControls _shipControls;
        private EngineModUi _engineMod;
        
        //Ship Components
        private Engine _engine;
        private EngineModManager _engineModManager;
        
        //State tracking
        private World _world;
        private Sector _currentSector;
        private DistanceTracker _distanceTracker;
        
        //Monitoring
        private LifeformMonitor _lifeformMonitor;
        private SpatialMonitor _spatialMonitor;

        
        //private
        private bool _requestedStop;
        private float _shipMass;
        private bool _isInstalled;
        private ReactiveCollection<IDisposable> _disposeBag = new ReactiveCollection<IDisposable>();
        
        //TODO: DELETE
        GameObject sphere;
        private Vector3 tempPos;
        [SerializeField] private Transform _cubeSpace;

        void Awake()
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Collider>().enabled = false;
            sphere.SetActive(true);
        }

        public float _circleRadius = 300.0f;
        public float _squareSize = 600.0f;

        public void OnInstall(Ship ship)
        {
            //UnityEngine.iOS.NotificationServices.RegisterForNotifications(NotificationType.Alert);
            
            _isInstalled = true;
            _shipMass = ship.CalculateMass();

            //Start acquiring components
            _shipControls = GetComponentInChildren<ShipControls>();
            _engine = GetComponentInChildren<Engine>();
            _distanceTracker = GetComponentInChildren<DistanceTracker>();
            _engineModManager = GetComponentInChildren<EngineModManager>();
            _engineMod = GetComponentInChildren<EngineModUi>();
            _lifeformMonitor = GetComponentInChildren<LifeformMonitor>();
            _spatialMonitor = GetComponentInChildren<SpatialMonitor>();

            //Hook up controls to systems
            _shipControls.ThrustStream
                .Do(_ => _requestedStop = false)
                .Subscribe(_engine.OnThrustChange).AddTo(_disposeBag);

            _shipControls.DirectionStream
                .Do(speed => _requestedStop = false)
                .Subscribe(_engine.OnDirectionChange).AddTo(_disposeBag);

            _shipControls.StopThrustersStream
                .Do(speed => _requestedStop = false)
                .Do(unit => _engine.OnThrustChange(0.0f))
                .Subscribe(unit => _shipControls.ResetThrusterControls()).AddTo(_disposeBag);

            _shipControls.SlowToStopStream
                .Subscribe(unit => _requestedStop = true).AddTo(_disposeBag);

            _shipControls.PickSectorStream
                .Subscribe(unit => _shipControls.ShowSectorPicker())
                .AddTo(_disposeBag);

            _shipControls.NewSectorStream
                .Do(_ => _shipControls.HideSectorPicker())
                .Do(sector => _distanceTracker.NewSector(sector))
                .Subscribe(SetSector).AddTo(_disposeBag);

            _engine.GetEngineMetrics()
                .Do(CheckMetrics)
                .Subscribe(_distanceTracker.OnEngineMetrics).AddTo(_disposeBag);
            
            
            //TODO Refactor
            Observable.EveryFixedUpdate()
                .Select(_ =>
                {
                    Lifeform lifeformPos = _lifeformMonitor.GetLifeform();
                    return _spatialMonitor.GetLifeformPositionInCube(lifeformPos);
                })
                .Subscribe(pos =>
                {
                    tempPos = pos;
                    //TryOrbitTwo();
                    
                });
            //_engineMod.GetSimulateStream()
            //  .Subscribe(_ => { _engineModManager.Simulate(); }).AddTo(_disposeBag);
        }

        private void TryOrbitOne()
        {
            Vector3 localPt = tempPos;//_cubeSpace.InverseTransformPoint(tempPos);

            sphere.transform.position = localPt;//new Vector3(ellipsePoint.x, transform.position.y, ellipsePoint.y);
                    
            Vector2 point = new Vector2(localPt.x, localPt.z);
                    
            Vector2 ellipsePoint = CameraUtils.Ellipse(point, _circleRadius, _circleRadius);
            Vector3 realPt = new Vector3(ellipsePoint.x, localPt.y, ellipsePoint.y);

            Camera.main.transform.position = realPt;//sphere.transform.position;
            Camera.main.transform.LookAt(tempPos);
        }

        public Transform targetBox; // The 3D box the 2D box is on
        public Transform movingBox; // The 2D box moving on the 3D box
        public float orbitRadiusX = 5f; // Radius of the orbit along the X-axis (oblong width)
        public float orbitRadiusZ = 3f; // Radius of the orbit along the Z-axis (oblong height)
        public float orbitHeight = 2f; // Height offset above the moving box.
        public float orbitSpeed = 5f; // Speed of orbit
        public bool useSurfaceNormal = true;
        private float currentAngle = 0f;
        private void TryOrbitTwo()
        {
            if (targetBox == null || movingBox == null)
            {
                Debug.LogError("Target Box or Moving Box not assigned!");
                return;
            }

            Bounds boxBounds = targetBox.CalculateBounds();
            orbitRadiusX = boxBounds.size.x * 1.5f;
            orbitRadiusZ = boxBounds.size.z * 2f;

            Vector3 movingBoxPosition = _spatialMonitor.GetLifeformPositionInCube(movingBox.GetComponent<Lifeform>());
            Vector3 cameraPosition = GetRoundedRectIntersection((movingBoxPosition - transform.position).normalized, orbitRadiusX, orbitRadiusZ, 5.0f);
            
    
            // Make the camera look at the center of the target box
            cameraPosition.y = movingBoxPosition.y;
            Camera.main.transform.position = cameraPosition;
            Camera.main.transform.forward = movingBoxPosition - cameraPosition;
        }
        

        
        
        public Vector3 GetEllipseIntersection(Vector3 direction, float a, float b)
        {
            direction.Normalize();

            float dx = direction.x;
            float dy = direction.z;

            float denom = (dx * dx) / (a * a) + (dy * dy) / (b * b);
            float t = Mathf.Sqrt(1f / denom);

            return direction * t;
        }
        
        /// <summary>
        /// Calculate the intersection point of a direction vector with a rounded rectangle.
        /// </summary>
        /// <param name="direction">Normalized direction vector</param>
        /// <param name="width">Width of the rectangle</param>
        /// <param name="height">Height of the rectangle</param>
        /// <param name="cornerRadius">Radius of the corner arcs</param>
        /// <returns>Intersection point</returns>
        public Vector3 GetRoundedRectIntersection(Vector3 direction, float width, float height, float cornerRadius)
        {
            direction.Normalize();
            
            // Get 2D components of the direction
            float dx = direction.x;
            float dy = direction.z;
            
            // Half-dimensions of the rectangle
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            
            // Inner rectangle (excluding rounded corners) dimensions
            float innerWidth = halfWidth - cornerRadius;
            float innerHeight = halfHeight - cornerRadius;
            
            // Check if we're pointing toward a corner region or a straight edge
            bool isXBeyondInner = Mathf.Abs(dx * innerHeight) > Mathf.Abs(dy * innerWidth);
            bool isYBeyondInner = Mathf.Abs(dy * innerWidth) > Mathf.Abs(dx * innerHeight);
            
            float t;
            
            if (isXBeyondInner && isYBeyondInner)
            {
                // We're hitting a corner region
                // Find which corner we're hitting
                float cornerX = Mathf.Sign(dx) * innerWidth;
                float cornerY = Mathf.Sign(dy) * innerHeight;
                
                // Calculate intersection with the circle at that corner
                Vector3 cornerCenter = new Vector3(cornerX, 0, cornerY);
                Vector3 toCorner = cornerCenter - Vector3.zero; // From origin to corner center
                
                // Project the direction onto the vector to the corner
                float proj = Vector3.Dot(direction, toCorner.normalized);
                
                // Distance from origin to the circle center
                float distToCornerCenter = toCorner.magnitude;
                
                // Use the quadratic formula to find intersection with the circle
                float a = 1; // direction is normalized
                float b = -2 * proj;
                float c = distToCornerCenter * distToCornerCenter - cornerRadius * cornerRadius;
                
                float discriminant = b * b - 4 * a * c;
                if (discriminant < 0)
                {
                    // No intersection (shouldn't happen in our case)
                    t = 0;
                }
                else
                {
                    // We want the smaller positive solution
                    float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
                    float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
                    
                    if (t1 > 0 && t2 > 0)
                        t = Mathf.Min(t1, t2);
                    else
                        t = Mathf.Max(t1, t2);
                }
            }
            else if (isXBeyondInner)
            {
                // We're hitting a vertical edge
                t = halfWidth / Mathf.Abs(dx);
            }
            else
            {
                // We're hitting a horizontal edge
                t = halfHeight / Mathf.Abs(dy);
            }
            
            return direction * t;
        }

        public void OnUninstall(Ship ship)
        {
            _engine = null;
            _shipControls = null;

            _isInstalled = false;

            _disposeBag.Dispose();
        }

        private void CheckMetrics(Engine.EngineMetrics metrics)
        {
            bool atMaxSpeed = Mathf.Abs(metrics.CurrentSpeed) >= _engine.MaxSpeed;
            bool noThrust = Mathf.Approximately(metrics.CurrentThrust, 0.0f);

            if (atMaxSpeed && !noThrust) // We can coast, now
            {
                _shipControls.ResetThrusterControls();
            }

            if (_requestedStop)
            {
                if (Mathf.Abs(metrics.CurrentSpeed) < 0.02f)
                {
                    Debug.Log("Stopped..");
                    _engine.OnThrustChange(0.0f);
                    _engine.__Freeze();
                    _shipControls.ResetThrusterControls();
                    _requestedStop = false;
                    _currentSector = null;
                }
                else
                {
                    _engine.InvertThrust(true);
                    _shipControls.MoveThrusterControl(_engine.ThrustRatio, false);
                }
            }
            
            if (_currentSector != null)
            {
                //Debug.Log("Travel Distance: " + _currentSector.Distance + ", Current Distance: " +
                  //        _distanceTracker.CurrentTripDistance);

                float distanceFromSector =  _currentSector.Distance - _distanceTracker.CurrentTripDistance;
                float vSquared = metrics.CurrentSpeed * metrics.CurrentSpeed; 
                float forceToSlowDown = (-vSquared * _shipMass) / (2.0f * distanceFromSector);

                if (_distanceTracker.CurrentTripDistance >= _currentSector.Distance)
                {
                    _requestedStop = true;
                }
                else if (Mathf.Abs(forceToSlowDown) >= _engine.MaxThrust)
                {
                    float slowDownRatio = Mathf.Clamp(forceToSlowDown / _engine.MaxThrust, -1.0f, 1.0f);
                    _shipControls.MoveThrusterControl(slowDownRatio, false);
                    _engine.OnThrustChange(slowDownRatio);
                }
            }
        }

        private void SetSector(Sector sector)
        {
            _currentSector = sector;

            _engine.OnDirectionChange(sector.Direction);
            Debug.Log("Selected sector: " + sector.Name);
        }

        public void ShowOverlay(string overlayId)
        {
            _shipControls.ShowOverlay(overlayId);
        }
    }
}