using System.Collections;
using System.Collections.Generic;
using Commonwealth.Script.Life;
using ProceduralToolkit;
using UnityEngine;

namespace Commonwealth.Script.Ship.Interior
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private Sprite _closeSprite;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Door[] _connectedDoors;
        [SerializeField] private float _doorCloseTime = 0.5f;
        [SerializeField] private LayerMask _doorSensorLayer;

        private SpriteRenderer _spriteRenderer;
        private float _timeSinceSensor;
        

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void FixedUpdate()
        {
            if (_spriteRenderer.sprite == _openSprite)
            {
                if (_timeSinceSensor > _doorCloseTime)
                {
                    BoxCollider2D c = GetComponent<BoxCollider2D>();
                    if (!Physics2D.OverlapBox(c.transform.position, c.size, 0.0f, _doorSensorLayer))
                    {
                        _spriteRenderer.sprite = _closeSprite;
                        _timeSinceSensor = 0.0f;
                    }
                        
                    _timeSinceSensor = 0.0f;
                        
                }
                else
                {
                    _timeSinceSensor += Time.deltaTime;
                }
            }
            else
            {
                _timeSinceSensor = 0.0f;
            }

            foreach (var door in _connectedDoors)
            {
                Debug.DrawLine(transform.position, door.transform.position);    
            }
            
        }

        public void Open()
        {
            _spriteRenderer.sprite = _openSprite;
        }

        public Door GetRandomConnectedDoor()
        {
            return _connectedDoors.GetRandom();
        }

        public void Use(Human human)
        {
            StartCoroutine(OpenDoor(human));
        }

        private IEnumerator OpenDoor(Human human)
        {
            human.gameObject.SetActive(false);
            
            yield return new WaitForSeconds(1.0f);
            
            human.transform.position = transform.position;
            human.gameObject.SetActive(true);
            human.SetDoor(null);

            yield return null;
        } 
    }
}