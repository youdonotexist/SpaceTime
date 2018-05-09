using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class CameraUtils
    {
        public static float CameraOffset(Camera camera, Vector3 position)
        {
            return position.z - camera.transform.position.z;
        }
    }
}