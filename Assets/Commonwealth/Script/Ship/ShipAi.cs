using System;
using Commonwealth.Script.Generation;
using Commonwealth.Script.Model;
using Commonwealth.Script.Proc;
using Commonwealth.Script.Ship.Hardware;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Ship
{
    public class ShipAi : MonoBehaviour, IShipAi
    {
        private Engine _engine;
        private ShipControls _shipControls;
        private World _world;

        private Sector _currentSector;

        private DistanceTracker _distanceTracker;

        private bool _requestedStop = false;

        private bool _isInstalled = false;
        private ReactiveCollection<IDisposable> _disposeBag = new ReactiveCollection<IDisposable>();

        public void OnInstall(Ship ship)
        {
            _isInstalled = true;

            //Start acquiring components
            _shipControls = ship.GetComponentInChildren<Hardware.ShipControls>();
            _engine = ship.GetComponentInChildren<Engine>();
            _distanceTracker = ship.GetComponentInChildren<DistanceTracker>();

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
                .Subscribe(unit => _shipControls.ShowSectorPicker(SectorGen.GenerateSectors(100.0f, 1000.0f, 5)))
                .AddTo(_disposeBag);

            _shipControls.NewSectorStream
                .Do(_ => _shipControls.HideSectorPicker())
                .Do(sector => _distanceTracker.NewSector())
                .Subscribe(SetSector).AddTo(_disposeBag);

            _engine.GetEngineMetrics()
                .Do(CheckMetrics)
                .Subscribe(_distanceTracker.OnEngineMetrics).AddTo(_disposeBag);
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
                }
                else
                {
                    _engine.InvertThrust(true);
                    _shipControls.MoveThrusterControl(_engine.ThrustRatio, false);
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
            if (_currentSector != null)
            {
                Debug.Log("Travel Distance: " + _currentSector.Distance + ", Current Distance: " + _distanceTracker.CurrentTripDistance);
            }
            
            if (_currentSector != null && _distanceTracker.CurrentTripDistance >= _currentSector.Distance)
            {
                _requestedStop = true;
            }
        }
    }
}