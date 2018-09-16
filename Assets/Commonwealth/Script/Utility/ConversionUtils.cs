using UnityEngine;

namespace Commonwealth.Script.Utility
{
    public class ConversionUtils : MonoBehaviour
    {
        public static Vector3 CubeToFlat(Transform cubeSpace, Bounds cubeSpaceBounds, Bounds flatSpaceBounds, Vector3 position)
        {
            Vector3 worldMainCam = position;
            Vector3 localPt = cubeSpace.InverseTransformPoint(worldMainCam);
        
            Vector3 scaledLocalPoint = new Vector3(
                localPt.x / (cubeSpaceBounds.extents.x), 
                localPt.y / (cubeSpaceBounds.extents.y), 
                localPt.z / (cubeSpaceBounds.extents.z));

            scaledLocalPoint.x *= -1.0f;
            float tmp = scaledLocalPoint.y;
            scaledLocalPoint.y = -scaledLocalPoint.z;
            scaledLocalPoint.z = tmp;
        
 
            Vector3 scaledWorldSide = new Vector3(scaledLocalPoint.x * flatSpaceBounds.extents.x, 
                scaledLocalPoint.y * flatSpaceBounds.extents.y,
                scaledLocalPoint.z * flatSpaceBounds.extents.z);
        
            return scaledWorldSide + flatSpaceBounds.center;
        }
    }
}