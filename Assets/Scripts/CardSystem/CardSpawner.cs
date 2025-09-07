using UnityEngine;
using RuntimeGraph.Sprite;

namespace CardSystem
{
    /// <summary>
    /// Handles spawning ship components from cards
    /// Integrates card system with existing SpriteRuntimeGraph ship building mechanics
    /// </summary>
    public class CardSpawner : MonoBehaviour
    {
        [Header("Integration References")]
        public SpriteRuntimeGraph runtimeGraph;
        public Camera mainCamera;
        
        [Header("Spawn Configuration")]
        public bool autoPositionParts = true;
        public Vector3 defaultSpawnOffset = new Vector3(0, 0, 0);
        public LayerMask groundLayer = 1;
        
        // Events
        public System.Action<ShipPartCard, SpriteNode> OnPartSpawned;
        public System.Action<ShipPartCard> OnSpawnFailed;
        
        private DeckManager deckManager;
        
        private void Start()
        {
            // Find required components
            if (runtimeGraph == null)
                runtimeGraph = FindObjectOfType<SpriteRuntimeGraph>();
                
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            deckManager = FindObjectOfType<DeckManager>();
            
            // Subscribe to card play events
            if (deckManager != null)
            {
                deckManager.OnCardPlayed += OnCardPlayed;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (deckManager != null)
            {
                deckManager.OnCardPlayed -= OnCardPlayed;
            }
        }
        
        /// <summary>
        /// Handles card played event from DeckManager
        /// </summary>
        private void OnCardPlayed(ShipPartCard card)
        {
            if (card.isInstant)
            {
                // Spawn immediately at default position
                SpawnPartFromCard(card, GetDefaultSpawnPosition());
            }
            else
            {
                // Enter placement mode (like existing ship part placement)
                EnterCardPlacementMode(card);
            }
        }
        
        /// <summary>
        /// Spawns a ship part from a card at the specified position
        /// </summary>
        public bool SpawnPartFromCard(ShipPartCard card, Vector3 position)
        {
            if (runtimeGraph == null)
            {
                Debug.LogError("SpriteRuntimeGraph not found! Cannot spawn ship part.");
                OnSpawnFailed?.Invoke(card);
                return false;
            }
            
            try
            {
                // Create the ship part node using existing system
                var nodeData = CreateNodeFromCard(card, position);
                if (nodeData == null)
                {
                    OnSpawnFailed?.Invoke(card);
                    return false;
                }
                
                // Add to runtime graph using existing CreateNodeInstance method
                var spawnedNodeInstance = runtimeGraph.GetType()
                    .GetMethod("CreateNodeInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(runtimeGraph, new object[] { nodeData });
                    
                if (spawnedNodeInstance != null)
                {
                    Debug.Log($"Successfully spawned {card.cardName} at {position}");
                    OnPartSpawned?.Invoke(card, spawnedNodeInstance as SpriteNode);
                    return true;
                }
                else
                {
                    Debug.LogError($"Failed to create node from card: {card.cardName}");
                    OnSpawnFailed?.Invoke(card);
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning part from card {card.cardName}: {e.Message}");
                OnSpawnFailed?.Invoke(card);
                return false;
            }
        }
        
        /// <summary>
        /// Creates NodeData from card for integration with existing system
        /// </summary>
        private SpriteNode.NodeData CreateNodeFromCard(ShipPartCard card, Vector3 position)
        {
            try
            {
                // Create node data compatible with existing system
                var nodeData = new SpriteNode.NodeData
                {
                    id = System.Guid.NewGuid().ToString(),
                    title = card.partName,
                    worldPosition = position,
                    
                    // MIDI integration for existing system
                    note = card.note,
                    velocity = card.velocity,
                    channel = card.channel,
                    duration = card.duration,
                    
                    // Visual properties
                    color = card.partColor,
                    
                    // Engine properties
                    isEngine = true,
                    engineType = DetermineEngineType(card.partCategory),
                    rotation = 0f,
                    icon = card.partIcon,
                    
                    // Create metadata entry for card info
                    metadata = new System.Collections.Generic.List<SpriteNode.MetadataEntry>
                    {
                        new SpriteNode.MetadataEntry { key = "source", value = "spawned_from_card" },
                        new SpriteNode.MetadataEntry { key = "card_name", value = card.cardName },
                        new SpriteNode.MetadataEntry { key = "card_category", value = card.partCategory }
                    }
                };
                
                return nodeData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create node data from card {card.cardName}: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Determines engine type from card category for compatibility
        /// </summary>
        private SpriteNode.EngineType DetermineEngineType(string category)
        {
            return category.ToLower() switch
            {
                "engine" => SpriteNode.EngineType.MainEngine,
                "weapon" => SpriteNode.EngineType.Thruster,
                "shield" => SpriteNode.EngineType.StabilityEngine,
                "power" => SpriteNode.EngineType.MainEngine,
                "life support" => SpriteNode.EngineType.StabilityEngine,
                "navigation" => SpriteNode.EngineType.Thruster,
                "thermal" => SpriteNode.EngineType.StabilityEngine,
                "data" => SpriteNode.EngineType.StabilityEngine,
                _ => SpriteNode.EngineType.MainEngine
            };
        }
        
        /// <summary>
        /// Enters card placement mode (simplified - spawns at default position)
        /// </summary>
        private void EnterCardPlacementMode(ShipPartCard card)
        {
            if (runtimeGraph == null) return;
            
            // For now, just spawn at default position
            // TODO: Implement interactive placement mode in future iteration
            Debug.Log($"Placing card: {card.cardName} at default position");
            SpawnPartFromCard(card, GetDefaultSpawnPosition());
        }
        
        /// <summary>
        /// Gets default spawn position
        /// </summary>
        private Vector3 GetDefaultSpawnPosition()
        {
            Vector3 position = Vector3.zero;
            
            if (autoPositionParts)
            {
                // Try to find a good position near existing parts
                position = FindOptimalSpawnPosition();
            }
            else
            {
                position = defaultSpawnOffset;
            }
            
            return position;
        }
        
        /// <summary>
        /// Finds optimal spawn position near existing parts
        /// </summary>
        private Vector3 FindOptimalSpawnPosition()
        {
            // Start with center
            Vector3 basePosition = Vector3.zero;
            
            // If there are existing nodes, position near them
            if (runtimeGraph != null)
            {
                try
                {
                    // Use reflection to access node instances
                    var nodeInstancesField = typeof(SpriteRuntimeGraph).GetField("nodeInstances", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (nodeInstancesField?.GetValue(runtimeGraph) is System.Collections.IDictionary nodeInstances && nodeInstances.Count > 0)
                    {
                        // Find center of existing nodes
                        Vector3 centerPos = Vector3.zero;
                        int count = 0;
                        
                        foreach (System.Collections.DictionaryEntry entry in nodeInstances)
                        {
                            if (entry.Value is SpriteNode node)
                            {
                                centerPos += node.transform.position;
                                count++;
                            }
                        }
                        
                        if (count > 0)
                        {
                            centerPos /= count;
                            // Offset slightly to avoid overlap
                            basePosition = centerPos + Vector3.right * 5f;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not find optimal position: {e.Message}");
                }
            }
            
            return basePosition + defaultSpawnOffset;
        }
        
        /// <summary>
        /// Spawns a part at mouse position (for UI integration)
        /// </summary>
        public bool SpawnPartAtMousePosition(ShipPartCard card)
        {
            if (mainCamera == null)
            {
                Debug.LogError("Main camera not found!");
                return false;
            }
            
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, mainCamera.nearClipPlane + 10f));
            
            return SpawnPartFromCard(card, worldPos);
        }
        
        /// <summary>
        /// Checks if a position is valid for spawning (no overlap with existing parts)
        /// </summary>
        public bool IsValidSpawnPosition(Vector3 position, ShipPartCard card)
        {
            if (runtimeGraph == null) return true;
            
            try
            {
                // Use existing overlap checking if available
                var checkOverlapMethod = typeof(SpriteRuntimeGraph).GetMethod("CheckCompositePartOverlap", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (checkOverlapMethod != null)
                {
                    var nodeTypeData = card.ToNodeTypeData();
                    return !(bool)checkOverlapMethod.Invoke(runtimeGraph, new object[] { position, nodeTypeData });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not check spawn position validity: {e.Message}");
            }
            
            return true; // Default to valid if we can't check
        }
    }
}