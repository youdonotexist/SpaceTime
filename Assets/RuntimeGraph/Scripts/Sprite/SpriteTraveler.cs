using UnityEngine;
using System.Collections;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Traveler component that moves between nodes and triggers events
    /// </summary>
    public class SpriteTraveler : MonoBehaviour
    {
        [System.Serializable]
        public class TravelerData
        {
            public string id = "";
            public string currentNodeId = "";
            public string targetNodeId = "";
            public string originStartNodeId = ""; // Track which start node spawned this traveler
            public float travelTime = 1f; // in quarter notes
            public float travelProgress = 0f; // 0-1
            public bool isActive = true;
            public Color color = Color.yellow;
            public float size = 0.2f;
        }
        
        [Header("Visual Settings")]
        public Color defaultColor = Color.yellow;
        public float defaultSize = 0.2f;
        public float pulseIntensity = 1.5f;
        public float pulseDuration = 0.3f;
        
        private SpriteRuntimeGraph graph;
        private TravelerData travelerData;
        private SpriteRenderer spriteRenderer;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private Vector3[] pathPoints;
        private bool isMoving = false;
        private bool isPulsing = false;
        
        public TravelerData TravelerDataInstance => travelerData;
        public bool IsMoving => isMoving;
        public bool IsActive => travelerData?.isActive ?? false;
        
        public void Initialize(SpriteRuntimeGraph graph, TravelerData data)
        {
            this.graph = graph;
            this.travelerData = data;
            
            SetupComponents();
            UpdatePosition();
            UpdateVisuals();
        }
        
        private void SetupComponents()
        {
            // Get or create sprite renderer
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            // Create circular sprite for traveler
            spriteRenderer.sprite = CreateTravelerSprite();
            spriteRenderer.sortingOrder = 15; // Above everything else
            
            gameObject.name = $"Traveler ({travelerData.id})";
        }
        
        private UnityEngine.Sprite CreateTravelerSprite()
        {
            // Create a small circular sprite for the traveler
            int size = 32;
            var texture = new Texture2D(size, size);
            var colors = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius)
                    {
                        // Inside the circle - solid color
                        float alpha = 1f - (distance / radius) * 0.3f; // Slight fade
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        // Outside the circle - transparent
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size * 4f);
        }
        
        private void UpdatePosition()
        {
            if (travelerData == null) return;
            
            var currentNode = graph?.GetNode(travelerData.currentNodeId);
            if (currentNode != null)
            {
                transform.position = currentNode.transform.position;
            }
        }
        
        private void UpdateVisuals()
        {
            if (spriteRenderer != null && travelerData != null)
            {
                spriteRenderer.color = travelerData.color;
                transform.localScale = Vector3.one * travelerData.size;
            }
        }
        
        public void StartMovement(string targetNodeId, float travelTime)
        {
            StartMovementWithConnection(targetNodeId, travelTime, null);
        }
        
        public void StartMovementWithConnection(string targetNodeId, float travelTime, SpriteConnection connection)
        {
            if (isMoving) return;
            
            var currentNode = graph?.GetNode(travelerData.currentNodeId);
            var targetNode = graph?.GetNode(targetNodeId);
            
            if (currentNode == null || targetNode == null) return;
            
            travelerData.targetNodeId = targetNodeId;
            travelerData.travelTime = travelTime;
            travelerData.travelProgress = 0f;
            
            // Get the connection path if available
            if (connection != null && connection.ConnectionDataInstance != null)
            {
                // Get the actual path points from the connection's LineRenderer
                var lineRenderer = connection.GetComponent<LineRenderer>();
                if (lineRenderer != null && lineRenderer.positionCount > 0)
                {
                    pathPoints = new Vector3[lineRenderer.positionCount];
                    for (int i = 0; i < lineRenderer.positionCount; i++)
                    {
                        pathPoints[i] = lineRenderer.GetPosition(i);
                    }
                }
                else
                {
                    // Fallback to direct path
                    pathPoints = new Vector3[] { currentNode.transform.position, targetNode.transform.position };
                }
            }
            else
            {
                // Fallback to direct path
                pathPoints = new Vector3[] { currentNode.transform.position, targetNode.transform.position };
            }
            
            isMoving = true;
            StartCoroutine(MovementCoroutine());
        }
        
        private IEnumerator MovementCoroutine()
        {
            while (travelerData.travelProgress < 1f && isMoving)
            {
                // Update progress based on tempo
                float tempoMultiplier = graph?.GetCurrentTempoMultiplier() ?? 1f;
                travelerData.travelProgress += Time.deltaTime * tempoMultiplier / travelerData.travelTime;
                travelerData.travelProgress = Mathf.Clamp01(travelerData.travelProgress);
                
                // Follow path points if available
                Vector3 currentPos;
                if (pathPoints != null && pathPoints.Length > 1)
                {
                    currentPos = GetPositionAlongPath(travelerData.travelProgress);
                }
                else
                {
                    // Fallback to direct movement
                    currentPos = Vector3.Lerp(startPosition, targetPosition, 
                        Mathf.SmoothStep(0f, 1f, travelerData.travelProgress));
                }
                
                transform.position = currentPos;
                
                yield return null;
            }
            
            // Movement complete
            isMoving = false;
            travelerData.currentNodeId = travelerData.targetNodeId;
            travelerData.targetNodeId = "";
            travelerData.travelProgress = 0f;
            
            // Trigger node arrival
            TriggerNodeArrival();
        }
        
        private Vector3 GetPositionAlongPath(float progress)
        {
            if (pathPoints == null || pathPoints.Length < 2)
                return transform.position;
            
            // Calculate total path length
            float totalLength = 0f;
            float[] segmentLengths = new float[pathPoints.Length - 1];
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
                totalLength += segmentLengths[i];
            }
            
            if (totalLength <= 0f)
                return pathPoints[0];
            
            // Find which segment we're on based on progress
            float targetDistance = progress * totalLength;
            float currentDistance = 0f;
            
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                if (currentDistance + segmentLengths[i] >= targetDistance)
                {
                    // We're on this segment
                    float segmentProgress = (targetDistance - currentDistance) / segmentLengths[i];
                    return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], 
                        Mathf.SmoothStep(0f, 1f, segmentProgress));
                }
                currentDistance += segmentLengths[i];
            }
            
            // Fallback to last point
            return pathPoints[pathPoints.Length - 1];
        }
        
        private void TriggerNodeArrival()
        {
            var currentNode = graph?.GetNode(travelerData.currentNodeId);
            if (currentNode == null) return;
            
            // Trigger node pulse
            StartCoroutine(PulseNode(currentNode));
            
            // Notify graph system
            graph?.OnTravelerArrivedAtNode(this, currentNode);
        }
        
        private IEnumerator PulseNode(SpriteNode node)
        {
            isPulsing = true;
            
            // Get the node's sprite renderer
            var nodeSpriteRenderer = node.GetComponent<SpriteRenderer>();
            if (nodeSpriteRenderer == null)
            {
                isPulsing = false;
                yield break;
            }
            
            // Store original scale and color
            Vector3 originalScale = node.transform.localScale;
            Color originalColor = nodeSpriteRenderer.color;
            
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                float t = elapsed / pulseDuration;
                float pulse = Mathf.Sin(t * Mathf.PI) * pulseIntensity;
                
                // Scale pulse
                node.transform.localScale = originalScale * (1f + pulse * 0.1f);
                
                // Color pulse (brightness increase)
                Color pulseColor = originalColor * (1f + pulse * 0.3f);
                nodeSpriteRenderer.color = pulseColor;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Restore original values
            node.transform.localScale = originalScale;
            nodeSpriteRenderer.color = originalColor;
            
            isPulsing = false;
        }
        
        public void StopMovement()
        {
            isMoving = false;
            StopAllCoroutines();
        }
        
        public void SetActive(bool active)
        {
            if (travelerData != null)
            {
                travelerData.isActive = active;
                gameObject.SetActive(active);
            }
        }
        
        public void UpdateTravelerData(TravelerData newData)
        {
            travelerData = newData;
            UpdateVisuals();
        }
        
        private void OnDestroy()
        {
            StopMovement();
        }
        
        private void OnDrawGizmos()
        {
            if (travelerData == null) return;
            
            // Draw traveler position
            Gizmos.color = travelerData.color;
            Gizmos.DrawWireSphere(transform.position, travelerData.size * 0.5f);
            
            // Draw movement path if moving
            if (isMoving)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition, targetPosition);
                
                // Draw progress indicator
                Vector3 progressPos = Vector3.Lerp(startPosition, targetPosition, travelerData.travelProgress);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(progressPos, 0.1f);
            }
        }
    }
}