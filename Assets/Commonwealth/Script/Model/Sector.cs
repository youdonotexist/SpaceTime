using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Commonwealth.Script.Model
{
    [System.Serializable]
    public class Sector
    {
        [SerializeField] private string _name;
        [SerializeField] private float _distance;
        [SerializeField] private List<Feature> _featureList = new List<Feature>();

        [SerializeField] private Vector3 _sectorDirection;

        public Sector(string name, float distance)
        {
            _name = name;
            _sectorDirection = Random.onUnitSphere;
            _distance = distance;
        }

        public string Name
        {
            get { return _name; }
        }

        public float Distance
        {
            get { return _distance; }
        }

        public Vector3 Direction
        {
            get { return _sectorDirection; }
        }

        public List<Feature> FeatureList
        {
            get { return _featureList; }
        }
        
       

        public void AddFeature(Feature feature)
        {
            _featureList.Add(feature);
        }
    }

    [System.Serializable]
    public abstract class Feature
    {
        enum FeatureType
        {
            Fuel
        }
    }

    [System.Serializable]
    public class FuelFeature : Feature
    {
        public FuelFeature(float fuelAmount)
        {
            _fuelAmount = fuelAmount;
        }

        private float _fuelAmount;

        public float FuelAmount
        {
            get { return _fuelAmount; }
        }
    }
}