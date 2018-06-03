using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.Utility;
using UnityEditor;
using UnityEngine;
using ProBuilder2;
using ProBuilder2.Common;
using ProBuilder2.MeshOperations;

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

        [MenuItem("Tools/Build Mesh From Sprite")]
        private static void BuildMeshFromSprite()
        {
            Transform t = Selection.activeTransform;
            if (t != null)
            {
                PolygonCollider2D collider = t.GetComponent<PolygonCollider2D>();
                if (collider != null)
                {
                    List<Vector2> points = new List<Vector2>();

                    foreach (Vector2 point in collider.points)
                    {
                        points.Add(t.InverseTransformPoint(point));
                    }

                    //points = SortVerticies(points);

                    Triangulator tri = new Triangulator(points.ToArray());
                    int[] indexes = tri.Triangulate();

                    List<Vector3> points3d = new List<Vector3>();
                    foreach (Vector2 pt in points)
                    {
                        points3d.Add(new Vector3(pt.x, pt.y, t.transform.position.z));
                    }
                    
                    points3d.Add(new Vector3(points[0].x, points[0].y, t.transform.position.z));

                    //List<Vector3> output = new List<Vector3>();
                    //foreach (int index in indexes)
                    //{
                    //    output.Add(points[index]);
                    //}

                    //output.Add(points[indexes[0]]);

                    pb_Object obj = pb_Object.CreateInstanceWithPoints(points3d.ToArray());
                    
                    List<pb_Face> trashFaces = obj.faces.ToList();
                    pb_Face baseFace = new pb_Face();
                    obj.CreatePolygon(indexes, false, out baseFace);
                    
                    //obj.DeleteFaces(trashFaces);
           
                    obj.ToMesh();
                    obj.Refresh();
                    
                }
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