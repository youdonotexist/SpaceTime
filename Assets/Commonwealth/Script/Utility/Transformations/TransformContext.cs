using Commonwealth.Script.Ship;
using UnityEngine;

namespace Commonwealth.Script.Utility.Transformations
{
    public class TransformContext
    {
        public Camera Camera;
        public Vector3 WorldPointCube;
        
        public TransformContext(Camera cam, Vector3 worldPointCube)
        {
            Camera = cam;
            WorldPointCube = worldPointCube;
        }
        
    }

    public class FlatTransformContext : TransformContext
    {
        public Vector3 WorldPointFlat;
        public static Side CurrentSide;

        public FlatTransformContext(Camera cam, Vector3 worldPointCube) : base(cam, worldPointCube)
        {
        }
    }

    public class UiTransformContext : TransformContext
    {
        public UiTransformContext(Camera cam, Vector3 worldPointCube) : base(cam, worldPointCube)
        {
        }
    }
}