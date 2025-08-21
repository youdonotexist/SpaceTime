using UnityEngine;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Test
{
    /// <summary>
    /// Test script to verify zoom-independent dot scaling behavior
    /// </summary>
    public class ZoomIndependentDotTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public SpriteRuntimeGraph targetGraph;
        public float[] testZoomLevels = { 0.5f, 1f, 2f, 5f, 10f, 20f };
        public float testDuration = 1.5f; // seconds per zoom level
        
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
            if (Input.GetKeyDown(KeyCode.Z))
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
                Debug.LogError("[ZoomIndependentDotTest] Missing required components for test");
                return;
            }
            
            testRunning = true;
            currentZoomIndex = 0;
            testTimer = 0f;
            
            Debug.Log("[ZoomIndependentDotTest] Starting zoom-independent dot test");
            SetZoomLevel(testZoomLevels[currentZoomIndex]);
        }
        
        public void StopTest()
        {
            testRunning = false;
            Debug.Log("[ZoomIndependentDotTest] Test stopped");
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
                float dotScale = GetDotScale();
                int activeDots = CountActiveDots();
                
                Debug.Log($"[DEBUG_LOG] Zoom Independent Test - Orthographic Size: {currentZoom:F2}, Dot Scale: {dotScale:F2}, Active Dots: {activeDots}");
                
                // Move to next zoom level
                currentZoomIndex++;
                if (currentZoomIndex >= testZoomLevels.Length)
                {
                    StopTest();
                    Debug.Log("[ZoomIndependentDotTest] Test completed - dots should maintain 1px appearance at all zoom levels");
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
                Debug.Log($"[ZoomIndependentDotTest] Set zoom to orthographic size {orthographicSize} (zoom level {zoomLevel:F2})");
            }
        }
        
        private float GetDotScale()
        {
            if (gridRenderer == null) return 0f;
            
            // Use reflection to access private dotGridDots field
            var field = typeof(SpriteGraphGrid).GetField("dotGridDots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var dots = field.GetValue(gridRenderer) as SpriteRenderer[];
                if (dots != null)
                {
                    foreach (var dot in dots)
                    {
                        if (dot != null && dot.enabled)
                        {
                            return dot.transform.localScale.x;
                        }
                    }
                }
            }
            
            return 0f;
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
            float dotScale = GetDotScale();
            int activeDots = CountActiveDots();
            
            if (Application.isPlaying)
            {
                var style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 12;
                
                string debugText = $"Orthographic Size: {orthographicSize:F2}\n" +
                                 $"Zoom Level: {zoomLevel:F2}\n" +
                                 $"Dot Scale: {dotScale:F2}\n" +
                                 $"Expected Scale: {orthographicSize:F2}\n" +
                                 $"Active Dots: {activeDots}\n" +
                                 $"Test Running: {testRunning}";
                
                GUI.Label(new Rect(320, 10, 300, 120), debugText, style);
            }
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(320, 140, 300, 200));
            
            if (GUILayout.Button(testRunning ? "Stop Zoom Test (Z)" : "Start Zoom Test (Z)"))
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
            
            if (GUILayout.Button("Test Zoom 10x"))
                SetZoomLevel(0.1f); // orthographic size 0.1 = zoom level 10
            
            GUILayout.EndArea();
        }
    }
}