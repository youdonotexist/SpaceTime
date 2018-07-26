using System;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.Utility;
using UnityEditor;
using UnityEngine;

namespace CW.Editor
{
    public class Tools
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
        
        [MenuItem("Tools/MinX Pivot")]
        private static void MinXPivot()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                float minX = float.MaxValue;
                SpriteRenderer[] children = t.GetComponentsInChildren<SpriteRenderer>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();
                float count = children.Length - 1;

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
                float minX = float.MaxValue;
                Transform[] children = t.GetComponentsInChildren<Transform>();
                Dictionary<Transform, Transform> childParentMap = new Dictionary<Transform, Transform>();
                float count = children.Length - 1;

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
            public Vector2 center;

            public Comp(Vector2 center)
            {
                this.center = center;
            }
            
            public int Compare (Vector2 a, Vector2 b)
            {
                double a1 = (Mathf.Rad2Deg * (Mathf.Atan2(a.x - center.x, a.y - center.y)) + 360) % 360;
                double a2 = (Mathf.Rad2Deg * (Mathf.Atan2(b.x - center.x, b.y - center.y)) + 360) % 360;
                return (int) (a1 - a2);
            }
        }
    }
}