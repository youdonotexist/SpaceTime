using UnityEngine;

namespace Commonwealth.Script.Life
{
    public class Human : MonoBehaviour
    {

        [SerializeField] private float _speed = 4.0f;
        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = transform.position;
            if (Input.GetKey(KeyCode.A))
            {
                pos.x -= _speed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                pos.x += _speed * Time.deltaTime;
            }

            transform.position = pos;
        }

        public Collider2D GetCollider()
        {
            return GetComponent<Collider2D>();
        }
    }
}