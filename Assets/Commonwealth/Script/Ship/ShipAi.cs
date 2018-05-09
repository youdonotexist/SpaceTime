using System;
using Commonwealth.Script.Generation;
using Commonwealth.Script.Model;
using Commonwealth.Script.Proc;
using Commonwealth.Script.Ship.EngineMod;
using Commonwealth.Script.Ship.Hardware;
using UniRx;
using UnityEngine;
using UnityEngine.iOS;

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

        private bool _requestedStop;
        private float _shipMass;

        private bool _isInstalled;
        private ReactiveCollection<IDisposable> _disposeBag = new ReactiveCollection<IDisposable>();

        public void OnInstall(Ship ship)
        {
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(NotificationType.Alert);
            
            _isInstalled = true;
            _shipMass = ship.CalculateMass();

            //Start acquiring components
            _shipControls = ship.GetComponentInChildren<ShipControls>();
            _engine = ship.GetComponentInChildren<Engine>();
            _distanceTracker = ship.GetComponentInChildren<DistanceTracker>();
            _engineModManager = ship.GetComponentInChildren<EngineModManager>();
            _engineMod = ship.GetComponentInChildren<EngineModUi>();

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
                .Subscribe(unit => _shipControls.ShowSectorPicker(SectorGen.GenerateSectors(5000.0f, 10000.0f, 5)))
                .AddTo(_disposeBag);

            _shipControls.NewSectorStream
                .Do(_ => _shipControls.HideSectorPicker())
                .Do(sector => _distanceTracker.NewSector(sector))
                .Subscribe(SetSector).AddTo(_disposeBag);

            _engine.GetEngineMetrics()
                .Do(CheckMetrics)
                .Subscribe(_distanceTracker.OnEngineMetrics).AddTo(_disposeBag);

            _engineMod.GetSimulateStream()
                .Subscribe(_ => { _engineModManager.Simulate(); }).AddTo(_disposeBag);
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
        
        private void FixedUpdate()
        {
            
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            
        }
    }
}