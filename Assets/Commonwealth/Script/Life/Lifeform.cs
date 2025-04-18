using UnityEngine;

namespace Commonwealth.Script.Life
{
    public class Lifeform : MonoBehaviour
    {
        public Vector3 GetCenter()
        {
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            return boxCollider.transform.TransformPoint(boxCollider.offset);
        }
    }
}