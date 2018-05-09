using UnityEditor;
using UnityEngine;

namespace CW.Editor
{
    public class PivotManager
    {
        [MenuItem("Tools/Center Pivot")]
        private static void CenterPivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                Vector3 pos = Vector3.zero;
                Transform[] children = t.GetComponentsInChildren<Transform>();
                float count = children.Length - 1;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        pos += child.transform.position;
                    }
                }
                
                pos = new Vector3(pos.x / count, pos.y / count, pos.z / count);

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.parent = null;
                    }
                }

                t.position = pos;
                
                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.parent = t;
                    }
                }

            }
        }
    }
}