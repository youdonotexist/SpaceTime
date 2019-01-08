using System.Collections.Generic;
using Commonwealth.Script.Life;
using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    public class SpatialMonitor : MonoBehaviour
    {
        [SerializeField] private ShipAi _shipAi;

        private Side[] _sideList;

        void Awake()
        {
            _sideList = _shipAi.GetComponentsInChildren<Side>();
        }

        public Vector3 GetSideNormal(Lifeform lifeform)
        {
            foreach (Side side in _sideList)
            {
                if (side.HasLifeform(lifeform))
                {
                    return side.transform.forward;
                }
            }

            return Vector3.zero;
        }

        public Vector3 GetLifeformPositionInCube(Lifeform lifeform)
        {
            foreach (Side side in _sideList)
            {
                if (side.HasLifeform(lifeform))
                {
                    //Debug.Log("lifeform in side: " + side.gameObject.name);
                    return side.FlatToCube(lifeform);
                }
            }

            return Vector3.zero;
        }
    }
}