using System;
using Commonwealth.Script.Life;
using Commonwealth.Script.Utility;
using UnityEngine;

namespace Commonwealth.Script.Ship
{
    [ExecuteInEditMode]
    public class Side : MonoBehaviour
    {
        [SerializeField] private Transform _cubeSpace;
        [SerializeField] private Material _renderMaterial;
        [SerializeField] private Camera _camera;
        [SerializeField] private LayerMask _layerMask;

        private RenderTexture _renderTexture;
        private Collider2D _flatSpace;
        private Mesh _cubeSpaceBounds;

        //DELETE
        GameObject sphere;
       

        void Awake()
        {
            InputManager.ActiveCameraFlat = _camera;

            Build();

            _flatSpace = GetComponent<Collider2D>();
            _cubeSpaceBounds = _cubeSpace.GetComponent<MeshFilter>().sharedMesh;

            Transform t = _cubeSpace.Find("Sphere");
            if (t == null)
            {
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.GetComponent<Collider>().enabled = false;
                sphere.transform.parent = _cubeSpace;
                sphere.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);  
               
            }
            else
            {
                sphere = t.gameObject;
         
            }
            
        }

        public void Build()
        {
            Transform sideTransform = transform;
            SpriteRenderer[] renderers = sideTransform.GetComponentsInChildren<SpriteRenderer>();
            Bounds rendererEncapsulatingBounds = new Bounds();
            BoxCollider2D parentBoxCollider = sideTransform.GetComponent<BoxCollider2D>();
            if (parentBoxCollider == null || renderers.Length == 0)
            {
                return;
            }

            float minx = float.MaxValue, miny = float.MaxValue, maxx = float.MinValue, maxy = float.MinValue;

            foreach (SpriteRenderer r in renderers)
            {
                minx = Mathf.Min((minx), r.bounds.min.x);
                miny = Mathf.Min((miny), r.bounds.min.y);
                maxx = Mathf.Max(maxx, r.bounds.max.x);
                maxy = Mathf.Max(maxy, r.bounds.max.y);
            }

            rendererEncapsulatingBounds.SetMinMax(new Vector2(minx, miny), new Vector2(maxx, maxy));
            parentBoxCollider.size = rendererEncapsulatingBounds.size;
            parentBoxCollider.offset = sideTransform.InverseTransformPoint(rendererEncapsulatingBounds.center);

            //Camera c = sideTransform.GetComponentInChildren<Camera>();
            //c.pixelRect = new Rect(Vector2.zero, new Vector2(rendererEncapsulatingBounds.size.x + 1, rendererEncapsulatingBounds.size.y + 1) * 100.0f);
            //c.aspect = rendererEncapsulatingBounds.size.x / rendererEncapsulatingBounds.size.y;
            //CalculateOrthographicSize(c, rendererEncapsulatingBounds);

            //Vector3 pos = c.transform.position;
            //c.transform.position = new Vector3(rendererEncapsulatingBounds.center.x, rendererEncapsulatingBounds.center.y, pos.z);

            //SetRenderScaleSize(rendererEncapsulatingBounds.size);
        }

        void CalculateOrthographicSize(Camera cam, Bounds boundingBox)
        {
            Vector3 topRight = new Vector3(boundingBox.max.x, boundingBox.min.y, 0f);
            Vector3 topRightAsViewport = cam.WorldToViewportPoint(topRight);

            if (topRightAsViewport.x >= topRightAsViewport.y)
                cam.orthographicSize = Mathf.Abs(boundingBox.size.x) / cam.aspect / 2f;
            else
                cam.orthographicSize = Mathf.Abs(boundingBox.size.y) / 2f;
        }

        private void SetRenderScaleSize(Vector2 size)
        {
            _cubeSpace.localScale = new Vector3(size.x, 1.0f, size.y);
            _renderTexture =
                new RenderTexture(Mathf.FloorToInt(size.x + 1) * 100, Mathf.FloorToInt(size.y + 1) * 100, 0);
            _renderTexture.filterMode = FilterMode.Point;

            _renderMaterial.mainTexture = _renderTexture;
            _camera.targetTexture = _renderTexture;
        }

        void LateUpdate()
        {
            if (_camera.targetTexture == null)
            {
                Build();
            }

            /*Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.DrawLine(ray.origin, ray.origin + (ray.direction * 100000.0f));

            if (!Physics.Raycast(ray, out hit, 10000000.0f, _layerMask) || hit.collider.transform != _cubeSpace) return;

            Vector3 cubeSpaceMouse = hit.point;
            Vector3 flatSpaceMouse = CubeToFlat(cubeSpaceMouse);

            if (!_flatSpace.bounds.Contains(flatSpaceMouse))
            {
                //Debug.Log("rejecting: " + gameObject.name);
                return;
            }

            InputManager.MouseWorldCube = cubeSpaceMouse;
            InputManager.MouseWorldFlat = flatSpaceMouse;
            InputManager.ActiveCameraFlat = _camera;
            InputManager.SetSide(this);*/
        }

        public bool HasLifeform(Lifeform lifeform)
        {
            Vector3 pos = lifeform.transform.position;
            pos.z = _flatSpace.transform.position.z;
            return _flatSpace.bounds.Contains(pos);
        }

        public Vector3 FlatToCube(Lifeform lifeform)
        {
            Bounds flatSpaceBounds = _flatSpace.bounds;
            Bounds cubeSpaceBounds = _cubeSpaceBounds.bounds;
            Vector3 flatWorld = lifeform.GetCenter();

            Vector3 flatLocalPos = _flatSpace.transform.InverseTransformPoint(flatWorld);
            float fixedZExtent = Math.Abs(flatSpaceBounds.size.z) < Mathf.Epsilon ? 1.0f : flatSpaceBounds.size.z;
            Vector3 normalizedFlatLocalPoint = new Vector3(
                flatLocalPos.x / (flatSpaceBounds.size.x),
                flatLocalPos.y / (flatSpaceBounds.size.y),
                flatLocalPos.z / (fixedZExtent)
               );
            
            (normalizedFlatLocalPoint.z, normalizedFlatLocalPoint.y) = (normalizedFlatLocalPoint.y, normalizedFlatLocalPoint.z);

            normalizedFlatLocalPoint.x *= -1.0f;
            normalizedFlatLocalPoint.z *= -1.0f;
            
            Vector3 scaledCubeLocalPoint = Vector3.Scale(normalizedFlatLocalPoint, _cubeSpace.GetComponent<BoxCollider>().size);

            if (HasLifeform(lifeform))
            {
                //Debug.Log(scaledFlatLocalPoint);
                sphere.transform.localPosition = scaledCubeLocalPoint;
            }

            return _cubeSpace.TransformPoint(scaledCubeLocalPoint);
        }

        public Vector3 CubeToFlat(Vector3 cubeWorldPos)
        {
            Bounds cubeSpaceBounds = _cubeSpaceBounds.bounds;
            Bounds flatSpaceBounds = _flatSpace.bounds;
            Vector3 cubeLocalPos = _cubeSpace.InverseTransformPoint(cubeWorldPos);

            Vector3 scaledCubeLocalPoint = new Vector3(
                cubeLocalPos.x / (cubeSpaceBounds.extents.x),
                cubeLocalPos.y / (cubeSpaceBounds.extents.y),
                cubeLocalPos.z / (cubeSpaceBounds.extents.z));

            scaledCubeLocalPoint.x *= -1.0f;
            float tmp = scaledCubeLocalPoint.y;
            scaledCubeLocalPoint.y = -scaledCubeLocalPoint.z;
            scaledCubeLocalPoint.z = tmp;


            Vector3 scaledWorldSide = new Vector3(scaledCubeLocalPoint.x * flatSpaceBounds.extents.x,
                scaledCubeLocalPoint.y * flatSpaceBounds.extents.y,
                scaledCubeLocalPoint.z * flatSpaceBounds.extents.z);

            return scaledWorldSide + flatSpaceBounds.center;
        }

        private void OnDrawGizmos()
        {
            Debug.DrawLine(transform.position, transform.position + (transform.forward * 100.0f));
        }
    }
}