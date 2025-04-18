using System;
using Commonwealth.Script.Ship.Interior;
using UnityEngine;

namespace Commonwealth.Script.Life
{
    public class Human : Lifeform
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
            // Get current position
            Vector2 position = _rigidbody2D.position;
            
            // Calculate movement for this frame
            if (Input.GetKey(KeyCode.A))
            {
                position.x -= _speed * Time.fixedDeltaTime;
                _spriteRenderer.flipX = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                position.x += _speed * Time.fixedDeltaTime;
                _spriteRenderer.flipX = false;
            }
            
            // Move the rigidbody using MovePosition
            _rigidbody2D.MovePosition(position);
        }

        private void Update()
        {
            Debug.Log("I did it!");
            // Handle door interaction
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (_currentDoor != null)
                {
                    _currentDoor.Use(this);
                }
            }
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