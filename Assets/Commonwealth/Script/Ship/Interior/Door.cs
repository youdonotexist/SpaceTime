using System.Collections.Generic;
using Commonwealth.Script.Life;
using UnityEngine;

namespace Commonwealth.Script.Ship.Interior
{
    public class Door : MonoBehaviour
    {
        [SerializeField] private Sprite _closeSprite;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Door _connectedDoor;

        private SpriteRenderer _spriteRenderer;

        private List<Collider2D> _ignoreList = new List<Collider2D>(); 

        void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        private void OnTriggerEnter2D(Collider2D that)
        {
            _spriteRenderer.sprite = _openSprite;
            
            Human human = that.GetComponent<Human>();

            if (!_ignoreList.Contains(that) && human != null)
            {
                _connectedDoor.UseDoor(human);
            }
        }
        
        private void OnTriggerExit2D(Collider2D that)
        {
            _spriteRenderer.sprite = _closeSprite;

            Human human = that.GetComponent<Human>();
            if (human != null)
            {
                _ignoreList.Remove(that);    
            }
            
        }

        public void UseDoor(Human human)
        {
            _ignoreList.Add(human.GetComponent<Collider2D>());
            human.transform.position = transform.position;
        }
    }
}