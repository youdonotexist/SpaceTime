using UnityEngine;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Test
{
    /// <summary>
    /// Test script to reproduce and verify grid alignment issues during zoom operations
    /// </summary>
    public class GridAlignmentTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public SpriteRuntimeGraph targetGraph;
        public float testZoomMin = 0.5f;
        public float testZoomMax = 5f;
        public int testIterations = 10;
        public float zoomSpeed = 0.1f;
        
        [Header("Debug Info")]
        public bool showDebugInfo = true;
        public bool autoTest = false;
        
        private SpriteGraphGrid gridRenderer;
        private SpriteNode testNode;
        private Vector3 originalNodePosition;
        private bool testRunning = false;
        private int currentIteration = 0;
        private float testTimer = 0f;
        private bool zoomingIn = true;
        
        private void Start()
        {
            // Find components if not assigned
            if (targetGraph == null)
                targetGraph = FindObjectOfType<SpriteRuntimeGraph>();
            
            if (targetGraph != null)
            {
                gridRenderer = targetGraph.GetComponentInChildren<SpriteGraphGrid>();
                
                // Create a test node at grid intersection
                if (gridRenderer != null)
                {
                    Vector3 testPos = Vector3.zero;
                    Vector3 snappedPos = gridRenderer.SnapToGrid(testPos);
                    var nodeData = targetGraph.CreateNode(snappedPos);
                    testNode = targetGraph.GetNode(nodeData.id);
                    originalNodePosition = snappedPos;
                    
                    Debug.Log($"[GridAlignmentTest] Created test node at {snappedPos}");
                }
            }
            
            if (autoTest)
            {
                StartTest();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (testRunning)
                    StopTest();
                else
                    StartTest();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetTestNode();
            }
            
            if (testRunning)
            {
                RunAutomaticTest();
            }
            
            if (showDebugInfo)
            {
                ShowDebugInfo();
            }
        }
        
        public void StartTest()
        {
            if (targetGraph == null || gridRenderer == null || testNode == null)
            {
                Debug.LogError("[GridAlignmentTest] Missing required components for test");
                return;
            }
            
            testRunning = true;
            currentIteration = 0;
            testTimer = 0f;
            zoomingIn = true;
            
            Debug.Log("[GridAlignmentTest] Starting grid alignment test");
            Debug.Log($"[DEBUG_LOG] Initial node position: {testNode.transform.position}");
            Debug.Log($"[DEBUG_LOG] Grid spacing: {gridRenderer.gridSpacing}");
        }
        
        public void StopTest()
        {
            testRunning = false;
            Debug.Log("[GridAlignmentTest] Test stopped");
        }
        
        public void ResetTestNode()
        {
            if (testNode != null && gridRenderer != null)
            {
                Vector3 snappedPos = gridRenderer.SnapToGrid(originalNodePosition);
                testNode.transform.position = snappedPos;
                testNode.NodeDataInstance.worldPosition = snappedPos;
                
                Debug.Log($"[GridAlignmentTest] Reset node to {snappedPos}");
            }
        }
        
        private void RunAutomaticTest()
        {
            testTimer += Time.deltaTime;
            
            if (testTimer >= 1f) // Change zoom every second
            {
                testTimer = 0f;
                
                Camera cam = targetGraph.GraphCamera;
                float currentZoom = cam.orthographicSize;
                float targetZoom;
                
                if (zoomingIn)
                {
                    targetZoom = Mathf.Max(testZoomMin, currentZoom * (1f - zoomSpeed));
                    if (targetZoom <= testZoomMin)
                        zoomingIn = false;
                }
                else
                {
                    targetZoom = Mathf.Min(testZoomMax, currentZoom * (1f + zoomSpeed));
                    if (targetZoom >= testZoomMax)
                        zoomingIn = true;
                }
                
                cam.orthographicSize = targetZoom;
                gridRenderer.UpdateGrid();
                
                // Check alignment after zoom
                CheckNodeGridAlignment();
                
                currentIteration++;
                if (currentIteration >= testIterations)
                {
                    StopTest();
                    Debug.Log("[GridAlignmentTest] Test completed");
                }
            }
        }
        
        private void CheckNodeGridAlignment()
        {
            if (testNode == null || gridRenderer == null) return;
            
            Vector3 nodePos = testNode.transform.position;
            Vector3 expectedGridPos = gridRenderer.SnapToGrid(nodePos);
            
            float alignmentError = Vector3.Distance(nodePos, expectedGridPos);
            float tolerance = 0.001f; // Very small tolerance for floating point precision
            
            string status = alignmentError < tolerance ? "ALIGNED" : "MISALIGNED";
            Debug.Log($"[DEBUG_LOG] Iteration {currentIteration}: Node at {nodePos}, Expected {expectedGridPos}, Error: {alignmentError:F6} - {status}");
            
            if (alignmentError > tolerance)
            {
                Debug.LogWarning($"[GridAlignmentTest] Node misalignment detected! Error: {alignmentError:F6}");
            }
        }
        
        private void ShowDebugInfo()
        {
            if (testNode == null || gridRenderer == null || targetGraph == null) return;
            
            Vector3 nodePos = testNode.transform.position;
            Vector3 snapPos = gridRenderer.SnapToGrid(nodePos);
            float zoomLevel = targetGraph.GraphCamera.orthographicSize;
            
            // Draw debug info in scene view
            Debug.DrawLine(nodePos, snapPos, Color.red);
            
            // Show on-screen debug info
            if (Application.isPlaying)
            {
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                
                string debugText = $"Node Pos: {nodePos}\n" +
                                 $"Snap Pos: {snapPos}\n" +
                                 $"Error: {Vector3.Distance(nodePos, snapPos):F6}\n" +
                                 $"Zoom: {zoomLevel:F2}\n" +
                                 $"Test Running: {testRunning}";
                
                GUI.Label(new Rect(10, 10, 300, 100), debugText, style);
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 120, 300, 200));
            
            if (GUILayout.Button(testRunning ? "Stop Test (T)" : "Start Test (T)"))
            {
                if (testRunning)
                    StopTest();
                else
                    StartTest();
            }
            
            if (GUILayout.Button("Reset Node (R)"))
            {
                ResetTestNode();
            }
            
            if (GUILayout.Button("Manual Alignment Check"))
            {
                CheckNodeGridAlignment();
            }
            
            GUILayout.EndArea();
        }
        
        private void OnDrawGizmos()
        {
            if (testNode != null && gridRenderer != null)
            {
                // Draw node position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(testNode.transform.position, 0.2f);
                
                // Draw expected grid position
                Vector3 expectedPos = gridRenderer.SnapToGrid(testNode.transform.position);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(expectedPos, 0.15f);
                
                // Draw line between them if misaligned
                if (Vector3.Distance(testNode.transform.position, expectedPos) > 0.001f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(testNode.transform.position, expectedPos);
                }
            }
        }
    }
}