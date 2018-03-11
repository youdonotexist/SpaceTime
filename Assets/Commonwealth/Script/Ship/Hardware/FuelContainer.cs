using UnityEngine;

namespace Commonwealth.Script.Ship
{
    public class FuelContainer : MonoBehaviour
    {
        // 100 kg of fuel
        [SerializeField] private float _maxCapacity = 100.0f;

        [SerializeField] private float _availableFuel;

        public float AvailableFuel
        {
            get { return _availableFuel; }
        }


        // Use this for initialization
        void Start()
        {
            _availableFuel = _maxCapacity;
        }

        // Update is called once per frame
        void Update()
        {
        }

        //Returns a number between 1 and 0 signifying how much of the requested they got
        public float AcquireFuel(float requested)
        {
            float possible = Mathf.Min(_availableFuel, requested);
            _availableFuel -= possible;

            return possible / requested;
        }
    }
}