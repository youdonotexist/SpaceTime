using ProceduralToolkit.Examples;
using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class InputManager : MonoBehaviour
    {
        public static Vector3 MouseWorld;
        public static Camera ActiveCamera;

        void Update()
        {
            if (ActiveCamera != null)
            {
                Vector3 origin = ActiveCamera.transform.position;
                Vector3 direction = MouseWorld - ActiveCamera.transform.position;
                Debug.DrawRay(origin, direction);
            }
        }
    }
}