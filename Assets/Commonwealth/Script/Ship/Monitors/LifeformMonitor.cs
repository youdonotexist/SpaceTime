using System.Collections.Generic;
using System.Xml.Schema;
using Commonwealth.Script.Life;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    public class LifeformMonitor : MonoBehaviour
    {
        private List<Lifeform> _lifesigns = new List<Lifeform>();

        [SerializeField] private ShipAi _shipAi;
        [SerializeField] private Lifeform _trackedLifeform;
        
        private ISubject<Lifeform> _lifeformStream;

        void Awake()
        {
            //_trackedLifeform = _shipAi.GetComponentInChildren<Lifeform>();
            _lifeformStream = new BehaviorSubject<Lifeform>(_trackedLifeform);
        }

        public IObservable<Lifeform> GetLifeformStream()
        {
            return _lifeformStream;
        }

        public Lifeform GetLifeform()
        {
            return _trackedLifeform;
        }
    }
}