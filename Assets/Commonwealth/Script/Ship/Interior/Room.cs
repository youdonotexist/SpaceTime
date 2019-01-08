using UnityEngine;

namespace Commonwealth.Script.Ship.Interior
{
    public class Room : MonoBehaviour
    {
        public Door[] Doors { get; private set; }

        void Awake()
        {
            Doors = GetComponentsInChildren<Door>();
        }
    }
}