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
        
        private Rigidbody2D _rigidbody2D;
        private Collider2D _collider2D;
        private Transform _transform;

        private IShipAi _shipAi;

        // Use this for initialization
        void Awake()
        {
            Application.runInBackground = true;
        
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _transform = GetComponent<Transform>();

            //There's already an Ai existing in the system, let's install it
            IShipAi existing = GetComponentInChildren<IShipAi>();
            if (existing != null)
            {
                InstallAi(existing);
            }
        }

        public void InstallAi(IShipAi shipAi)
        {
            _shipAi = shipAi;
            _shipAi.OnInstall(this);
        }

        public Rigidbody2D GetRigidbody2D()
        {
            return _rigidbody2D;
        }

        public Collider2D GetCollider2D()
        {
            return _collider2D;
        }

        public Transform GetTransform()
        {
            return _transform;
        }
        
        public float CalculateMass()
        {
            return _mass;
        }
    }
    
    
}