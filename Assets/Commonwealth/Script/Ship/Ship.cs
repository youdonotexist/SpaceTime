using UnityEngine;

namespace Commonwealth.Script.Ship
{
    public interface IShipAi
    {
        void OnInstall(Ship ship);
        void OnUninstall(Ship ship);
    }

    public class Ship : MonoBehaviour
    {
        [SerializeField] private float _mass;
        [SerializeField] private Transform _space;
        [SerializeField] private ShipAi _shipAi;
        
        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Transform _transform;
        
        

        // Use this for initialization
        void Awake()
        {
            Application.runInBackground = true;
        
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _transform = GetComponent<Transform>();

            //There's already an Ai existing in the system, let's install it
            InstallAi(_shipAi);
        }

        public void InstallAi(ShipAi shipAi)
        {
            _shipAi = shipAi;
            _shipAi.OnInstall(this);
        }

        public Collider2D GetCollider2D()
        {
            return _collider2D;
        }

        public Transform GetTransform()
        {
            return _space;
        }
        
        public float CalculateMass()
        {
            return _mass;
        }

        public void ShowOverlay(string overlayId)
        {
            _shipAi.ShowOverlay(overlayId);
        }
    }
    
    
}