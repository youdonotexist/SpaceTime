using UnityEngine;

namespace Commonwealth.Script.Ship.Interior
{
    public class Room : MonoBehaviour
    {
        private Door[] _doors;
        
        void Awake()
        {
            _doors = GetComponentsInChildren<Door>();
        }
    }
}