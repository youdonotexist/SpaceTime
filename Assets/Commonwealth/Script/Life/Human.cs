using Commonwealth.Script.Ship.Interior;
using UnityEngine;

namespace Commonwealth.Script.Life
{
    public class Human : MonoBehaviour
    {

        [SerializeField] private float _speed = 4.0f;

        private Rigidbody2D _rigidbody2D;
        private SpriteRenderer _spriteRenderer;
        private Door _currentDoor;
        
        // Use this for initialization
        void Start()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            Vector3 pos = transform.position;
            if (Input.GetKey(KeyCode.A))
            {
                pos.x -= _speed * Time.deltaTime;
                _spriteRenderer.flipX = true;

            }
            else if (Input.GetKey(KeyCode.D))
            {
                pos.x += _speed * Time.deltaTime;
                _spriteRenderer.flipX = false;
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                _currentDoor.Use(this);
            }

            _rigidbody2D.MovePosition(pos);
        }
        
        private void OnTriggerEnter2D(Collider2D that)
        {   
            Door door = that.GetComponent<Door>();

            if (door != null)
            {
                door.Open();
                _currentDoor = door.GetRandomConnectedDoor();
            }
        }

        private void OnTriggerExit2D(Collider2D that)
        {
            Door door = that.GetComponent<Door>();
            
            if (door != null)
            {
                _currentDoor = null;
            }
            
        }

        public Collider2D GetCollider()
        {
            return GetComponent<Collider2D>();
        }

        public void SetDoor(Door door)
        {
            _currentDoor = door;
        }
    }
}