using System.Collections.Generic;
using Commonwealth.Script.Ship;
using Commonwealth.Script.Ship.EngineMod;
using UnityEditor;
using UnityEngine;

namespace Commonwealth.Editor
{
    public static class Tools
    {
        [MenuItem("Tools/Center Pivot")]
        private static void CenterPivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                Vector3 pos = Vector3.zero;
                Transform[] children = t.GetComponentsInChildren<Transform>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();
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
                        childParentMap.Add(child, child.parent);
                        child.parent = null;
                    }
                }

                t.position = pos;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.parent = childParentMap[child];
                    }
                }
            }
        }
        
        [MenuItem("Tools/Freeze Pivot")]
        private static void FreezePivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                Vector3 pos = Vector3.zero;
                Transform[] children = t.GetComponentsInChildren<Transform>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();
                float count = children.Length - 1;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        pos += child.transform.position;
                    }
                }

                pos = new Vector3(0.0f, 0.0f, 0.0f);

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        childParentMap.Add(child, child.parent);
                        child.parent = null;
                    }
                }

                t.localPosition = pos;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.parent = childParentMap[child];
                    }
                }
            }
        }
        
        [MenuItem("Tools/MinX Pivot")]
        private static void MinXPivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                float minX = float.MaxValue;
                SpriteRenderer[] children = t.GetComponentsInChildren<SpriteRenderer>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();

                foreach (var child in children)
                {
                    if (child != t && child.bounds.min.x < minX)
                    {
                        minX = child.bounds.min.x;
                    }
                }

                Vector3 pos = t.position;
                pos = new Vector3(minX, pos.y, pos.z);

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        childParentMap.Add(child.transform, child.transform.parent);
                        child.transform.parent = null;
                    }
                }

                t.position = pos;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.transform.parent = childParentMap[child.transform];
                    }
                }
            }
        }
        
        [MenuItem("Tools/Center On Frame")]
        private static void CenterSpritePivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                Transform[] children = t.GetComponentsInChildren<Transform>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();

                Vector3 pos = t.position;
                foreach (var child in children)
                {
                    if (child.gameObject.name == "Framer")
                    {
                        pos = child.transform.position;
                    }
                }

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        childParentMap.Add(child, child.parent);
                        child.parent = null;
                    }
                }

                t.position = pos;

                foreach (var child in children)
                {
                    if (child != t)
                    {
                        child.parent = childParentMap[child];
                    }
                }
            }
        }

        [MenuItem("Tools/Fit Fitter")]
        private static void FitFitter()
        {
            Transform t = Selection.activeTransform;
            CameraFit fit = t.GetComponentInChildren<CameraFit>();
            if (fit)
            {
                fit.FitFitter();
            }
        }
        
        [MenuItem("Tools/Collider Fitter")]
        private static void ColliderFitter()
        {
            Side s = Selection.activeTransform.GetComponent<Side>();
            s.Build();
        }

        [MenuItem("Tools/Index Engine Slots")]
        private static void IndexEngineSlots()
        {
            EngineSlot[] slots = Selection.activeTransform.GetComponentsInChildren<EngineSlot>();
            List<EngineSlot> slotList = new List<EngineSlot>(slots);

            foreach (EngineSlot slot in slotList)
            {
                slot.Index = new Vector2Int();
            }
            
            
        }
        
        public static List<Vector2> SortVerticies(List<Vector2> points) {
            // get centroid
            Vector2 center = FindCentroid(points);
            points.Sort(new Comp(center));
            return points;
        }
        
        public static Vector3 FindCentroid(List<Vector2> points) {
            float x = 0;
            float y = 0;
            foreach (Vector2 p in points) {
                x += p.x;
                y += p.y;
            }

            Vector3 center = Vector3.zero;
            center.x = x / points.Count;
            center.y = y / points.Count;
            return center;
        }
        
        public class Comp : IComparer<Vector2>
        {
            private readonly Vector2 _center;

            public Comp(Vector2 center)
            {
                _center = center;
            }
            
            public int Compare (Vector2 a, Vector2 b)
            {
                double a1 = (Mathf.Rad2Deg * (Mathf.Atan2(a.x - _center.x, a.y - _center.y)) + 360) % 360;
                double a2 = (Mathf.Rad2Deg * (Mathf.Atan2(b.x - _center.x, b.y - _center.y)) + 360) % 360;
                return (int) (a1 - a2);
            }
        }
    }
}