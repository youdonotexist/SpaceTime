using UnityEngine;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Test
{
    /// <summary>
    /// Test script to verify dot visibility and scaling behavior at different zoom levels
    /// </summary>
    public class DotVisibilityTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public SpriteRuntimeGraph targetGraph;
        public float[] testZoomLevels = { 0.5f, 1f, 2f, 5f, 10f };
        public float testDuration = 2f; // seconds per zoom level
        
        [Header("Debug")]
        public bool showDebugInfo = true;
        public bool autoTest = false;
        
        private SpriteGraphGrid gridRenderer;
        private int currentZoomIndex = 0;
        private float testTimer = 0f;
        private bool testRunning = false;
        
        private void Start()
        {
            // Find components if not assigned
            if (targetGraph == null)
                targetGraph = FindObjectOfType<SpriteRuntimeGraph>();
            
            if (targetGraph != null)
            {
                gridRenderer = targetGraph.GetComponentInChildren<SpriteGraphGrid>();
            }
            
            if (autoTest)
            {
                StartTest();
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (testRunning)
                    StopTest();
                else
                    StartTest();
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
            if (targetGraph == null || gridRenderer == null)
            {
                Debug.LogError("[DotVisibilityTest] Missing required components for test");
                return;
            }
            
            testRunning = true;
            currentZoomIndex = 0;
            testTimer = 0f;
            
            Debug.Log("[DotVisibilityTest] Starting dot visibility test");
            SetZoomLevel(testZoomLevels[currentZoomIndex]);
        }
        
        public void StopTest()
        {
            testRunning = false;
            Debug.Log("[DotVisibilityTest] Test stopped");
        }
        
        private void RunAutomaticTest()
        {
            testTimer += Time.deltaTime;
            
            if (testTimer >= testDuration)
            {
                testTimer = 0f;
                
                // Log current zoom level results
                float currentZoom = targetGraph.GraphCamera.orthographicSize;
                float zoomLevel = 1f / currentZoom;
                int activeDots = CountActiveDots();
                
                Debug.Log($"[DEBUG_LOG] Zoom Level: {zoomLevel:F2}, Orthographic Size: {currentZoom:F2}, Active Dots: {activeDots}");
                
                // Move to next zoom level
                currentZoomIndex++;
                if (currentZoomIndex >= testZoomLevels.Length)
                {
                    StopTest();
                    Debug.Log("[DotVisibilityTest] Test completed - dots should be visible at all zoom levels");
                    return;
                }
                
                SetZoomLevel(testZoomLevels[currentZoomIndex]);
            }
        }
        
        private void SetZoomLevel(float orthographicSize)
        {
            if (targetGraph?.GraphCamera != null)
            {
                targetGraph.GraphCamera.orthographicSize = orthographicSize;
                gridRenderer?.UpdateGrid();
                
                float zoomLevel = 1f / orthographicSize;
                Debug.Log($"[DotVisibilityTest] Set zoom to orthographic size {orthographicSize} (zoom level {zoomLevel:F2})");
            }
        }
        
        private int CountActiveDots()
        {
            if (gridRenderer == null) return 0;
            
            // Use reflection to access private dotGridDots field
            var field = typeof(SpriteGraphGrid).GetField("dotGridDots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var dots = field.GetValue(gridRenderer) as SpriteRenderer[];
                if (dots != null)
                {
                    int count = 0;
                    foreach (var dot in dots)
                    {
                        if (dot != null && dot.enabled)
                            count++;
                    }
                    return count;
                }
            }
            
            return 0;
        }
        
        private void ShowDebugInfo()
        {
            if (targetGraph?.GraphCamera == null) return;
            
            float orthographicSize = targetGraph.GraphCamera.orthographicSize;
            float zoomLevel = 1f / orthographicSize;
            int activeDots = CountActiveDots();
            
            if (Application.isPlaying)
            {
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                
                string debugText = $"Orthographic Size: {orthographicSize:F2}\n" +
                                 $"Zoom Level: {zoomLevel:F2}\n" +
                                 $"Active Dots: {activeDots}\n" +
                                 $"Test Running: {testRunning}";
                
                GUI.Label(new Rect(10, 10, 300, 80), debugText, style);
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 100, 300, 150));
            
            if (GUILayout.Button(testRunning ? "Stop Dot Test (D)" : "Start Dot Test (D)"))
            {
                if (testRunning)
                    StopTest();
                else
                    StartTest();
            }
            
            if (GUILayout.Button("Test Zoom 0.5x"))
                SetZoomLevel(2f); // orthographic size 2 = zoom level 0.5
            
            if (GUILayout.Button("Test Zoom 1x"))
                SetZoomLevel(1f); // orthographic size 1 = zoom level 1
            
            if (GUILayout.Button("Test Zoom 2x"))
                SetZoomLevel(0.5f); // orthographic size 0.5 = zoom level 2
            
            if (GUILayout.Button("Test Zoom 5x"))
                SetZoomLevel(0.2f); // orthographic size 0.2 = zoom level 5
            
            GUILayout.EndArea();
        }
    }
}