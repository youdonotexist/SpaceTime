using System.Collections;
using System.Collections.Generic;
using Commonwealth.Script.Utility;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Side : MonoBehaviour
{
    [SerializeField] private Transform _renderSquare;
    [SerializeField] private Material _renderMaterial;
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask _layerMask;

    private RenderTexture _renderTexture;
    private Collider2D _renderBounds;

    void Awake()
    {
        InputManager.ActiveCamera = _camera;
        
        Build();

        _renderBounds = GetComponent<Collider2D>();
    }

    public void Build()
    {
        Transform t = transform;
        SpriteRenderer[] renderers = t.GetComponentsInChildren<SpriteRenderer>();
        Bounds b = new Bounds();
        BoxCollider2D parent = t.GetComponent<BoxCollider2D>();
        if (parent == null || renderers.Length == 0)
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

        b.SetMinMax(new Vector2(minx, miny), new Vector2(maxx, maxy));
        parent.size = b.size;
        parent.offset = t.InverseTransformPoint(b.center);

        Camera c = t.GetComponentInChildren<Camera>();
        c.pixelRect = new Rect(Vector2.zero, new Vector2(b.size.x + 1, b.size.y + 1) * 100.0f);
        c.aspect = b.size.x / b.size.y;
        CalculateOrthographicSize(c, b);

        Vector3 pos = c.transform.position;
        c.transform.position = new Vector3(b.center.x, b.center.y, pos.z);

        SetRenderScaleSize(b.size);
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
        _renderSquare.localScale = new Vector3(size.x, 1.0f, size.y);
        _renderTexture = new RenderTexture(Mathf.FloorToInt(size.x + 1) * 100, Mathf.FloorToInt(size.y + 1) * 100, 0);
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

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
        RaycastHit hit;
        
        Debug.DrawLine(ray.origin, ray.origin+ (ray.direction * 100000.0f));

        if (!Physics.Raycast(ray, out hit, 10000000.0f, _layerMask) || hit.collider.transform != _renderSquare) return;

        Bounds rendererBounds = _renderSquare.GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 worldMainCam = hit.point;
        

        Vector3 localPt = _renderSquare.InverseTransformPoint(worldMainCam);
        
        Vector3 scaledLocalPoint = new Vector3(
            localPt.x / (rendererBounds.extents.x), 
            localPt.y / (rendererBounds.extents.y), 
            localPt.z / (rendererBounds.extents.z));

        scaledLocalPoint.x *= -1.0f;
        float tmp = scaledLocalPoint.y;
        scaledLocalPoint.y = -scaledLocalPoint.z;
        scaledLocalPoint.z = tmp;
        
 
        Vector3 scaledWorldSide = new Vector3(scaledLocalPoint.x * _renderBounds.bounds.extents.x, 
            scaledLocalPoint.y * _renderBounds.bounds.extents.y,
            scaledLocalPoint.z * _renderBounds.bounds.extents.z);
        
        Vector3 worldSide = scaledWorldSide + _renderBounds.bounds.center;

        if (!_renderBounds.bounds.Contains(worldSide))
        {
            Debug.Log("rejecting: " + gameObject.name);
            return;
        } 

        InputManager.MouseWorld = worldSide;
        InputManager.ActiveCamera = _camera;
        InputManager.SetSide(this);
    }
}