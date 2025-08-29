using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// External procedural ship layout generator for creating random ship configurations
    /// Decoupled from SpriteRuntimeGraph to improve separation of concerns
    /// </summary>
    public class ProceduralShipLayoutGenerator
    {
        public enum ShipLayoutType
        {
            Linear,
            Cross,
            Cluster,
            Ring
        }

        /// <summary>
        /// Interface for the generator to interact with the graph system
        /// </summary>
        public interface IShipLayoutTarget
        {
            void ClearAllNodesAndConnections();
            SpriteNode.NodeData CreateShipPartNode(EnginePartNodeData part, Vector3 position);
            SpriteNode.NodeData CreateShipPartNode(EnginePartNodeData part, Vector3 position, bool isStartNode);
            void CreateAutoConnection(string fromNodeId, string toNodeId);
            Vector3 SnapToGrid(Vector3 position);
            int GetCurrentNodeCount();
            bool IsPositionOccupied(Vector3 position, float tolerance = 1f);
        }

        /// <summary>
        /// Configuration settings for the generator
        /// </summary>
        [System.Serializable]
        public class GeneratorSettings
        {
            [Header("General")]
            public int minimumTotalParts = 100;
            public Vector2Int systemCountRange = new Vector2Int(2, 4); // Multiple start nodes (systems)

            [Header("Linear Layout")]
            public float linearSpacing = 3f;
            public Vector2Int linearPartCountRange = new Vector2Int(20, 25);

            [Header("Cross Layout")]
            public float crossArmLength = 4f;
            public float crossEndPieceDistance = 2f;
            public Vector2Int crossPartCountRange = new Vector2Int(20, 28);

            [Header("Cluster Layout")]
            public float clusterRadius = 5f;
            public float clusterHubConnectionChance = 0.7f;
            public Vector2Int clusterPartCountRange = new Vector2Int(20, 30);

            [Header("Ring Layout")]
            public float ringRadius = 6f;
            public Vector2Int ringPartCountRange = new Vector2Int(20, 32);
        }

        private readonly GeneratorSettings settings;

        public ProceduralShipLayoutGenerator(GeneratorSettings settings = null)
        {
            this.settings = settings ?? CreateDefaultSettings();
        }

        /// <summary>
        /// Generate a random ship layout using the specified target interface
        /// </summary>
        public void GenerateRandomShipLayout(IShipLayoutTarget target)
        {
            if (target == null)
            {
                Debug.LogError("ProceduralShipLayoutGenerator: Target cannot be null");
                return;
            }

            // Clear existing nodes and connections
            target.ClearAllNodesAndConnections();

            // Generate a random ship layout
            var layoutType = (ShipLayoutType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(ShipLayoutType)).Length);

            switch (layoutType)
            {
                case ShipLayoutType.Linear:
                    GenerateLinearShipLayout(target);
                    break;
                case ShipLayoutType.Cross:
                    GenerateCrossShipLayout(target);
                    break;
                case ShipLayoutType.Cluster:
                    GenerateClusterShipLayout(target);
                    break;
                case ShipLayoutType.Ring:
                    GenerateRingShipLayout(target);
                    break;
                default:
                    GenerateLinearShipLayout(target);
                    break;
            }

            Debug.Log($"Generated {layoutType} ship layout with {target.GetCurrentNodeCount()} parts");
        }

        /// <summary>
        /// Generate a specific layout type
        /// </summary>
        public void GenerateLayout(IShipLayoutTarget target, ShipLayoutType layoutType)
        {
            if (target == null)
            {
                Debug.LogError("ProceduralShipLayoutGenerator: Target cannot be null");
                return;
            }

            target.ClearAllNodesAndConnections();

            switch (layoutType)
            {
                case ShipLayoutType.Linear:
                    GenerateLinearShipLayout(target);
                    break;
                case ShipLayoutType.Cross:
                    GenerateCrossShipLayout(target);
                    break;
                case ShipLayoutType.Cluster:
                    GenerateClusterShipLayout(target);
                    break;
                case ShipLayoutType.Ring:
                    GenerateRingShipLayout(target);
                    break;
                default:
                    GenerateLinearShipLayout(target);
                    break;
            }

            Debug.Log($"Generated {layoutType} ship layout with {target.GetCurrentNodeCount()} parts");
        }

        private void GenerateLinearShipLayout(IShipLayoutTarget target)
        {
            var engineParts = EnginePartCatalog.GetAllEngineParts();
            var selectedParts = GetRandomShipParts(engineParts, UnityEngine.Random.Range(settings.linearPartCountRange.x, settings.linearPartCountRange.y));
            var systemCount = UnityEngine.Random.Range(settings.systemCountRange.x, settings.systemCountRange.y + 1);

            Vector3 startPos = Vector3.zero;
            SpriteNode.NodeData previousNode = null;

            for (int i = 0; i < selectedParts.Count; i++)
            {
                var part = selectedParts[i];
                Vector3 position = startPos + Vector3.right * (i * settings.linearSpacing);
                position = target.SnapToGrid(position);

                // Ensure position is not occupied
                int maxAttempts = 50; // Safety counter to prevent infinite loops
                int attempts = 0;
                while (target.IsPositionOccupied(position) && attempts < maxAttempts)
                {
                    position += Vector3.up * settings.linearSpacing * 0.5f;
                    position = target.SnapToGrid(position);
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"ProceduralShipLayoutGenerator: Could not find free position for part {i} in LinearShipLayout after {maxAttempts} attempts. Using occupied position.");
                }

                // Mark first few nodes as start nodes (systems) based on system count
                bool isStartNode = i < systemCount && IsSystemPart(part);
                var nodeData = target.CreateShipPartNode(part, position, isStartNode);

                // Connect to previous node
                if (previousNode != null)
                {
                    target.CreateAutoConnection(previousNode.id, nodeData.id);
                }

                previousNode = nodeData;
            }
        }

        private void GenerateCrossShipLayout(IShipLayoutTarget target)
        {
            var engineParts = EnginePartCatalog.GetAllEngineParts();
            var selectedParts = GetRandomShipParts(engineParts, UnityEngine.Random.Range(settings.crossPartCountRange.x, settings.crossPartCountRange.y));
            var systemCount = UnityEngine.Random.Range(settings.systemCountRange.x, settings.systemCountRange.y + 1);

            Vector3 centerPos = Vector3.zero;
            int systemsCreated = 0;

            // Center node (command/core) - mark as start node if it's a system part
            var centerPart = selectedParts[0];
            bool isCenterStart = IsSystemPart(centerPart) && systemsCreated < systemCount;
            if (isCenterStart) systemsCreated++;
            
            var centerNode = target.CreateShipPartNode(centerPart, centerPos, isCenterStart);

            // Create arms
            Vector3[] armDirections = { Vector3.right, Vector3.up, Vector3.left, Vector3.down };

            for (int arm = 0; arm < 4 && (arm + 1) < selectedParts.Count; arm++)
            {
                Vector3 armPos = centerPos + armDirections[arm] * settings.crossArmLength;
                armPos = target.SnapToGrid(armPos);

                // Ensure position is not occupied
                int maxAttempts = 50; // Safety counter to prevent infinite loops
                int attempts = 0;
                while (target.IsPositionOccupied(armPos) && attempts < maxAttempts)
                {
                    armPos += armDirections[arm] * settings.crossArmLength * 0.3f;
                    armPos = target.SnapToGrid(armPos);
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"ProceduralShipLayoutGenerator: Could not find free position for arm {arm} in CrossShipLayout after {maxAttempts} attempts. Using occupied position.");
                }

                var armPart = selectedParts[arm + 1];
                bool isArmStart = IsSystemPart(armPart) && systemsCreated < systemCount;
                if (isArmStart) systemsCreated++;
                
                var armNode = target.CreateShipPartNode(armPart, armPos, isArmStart);
                target.CreateAutoConnection(centerNode.id, armNode.id);

                // Add end pieces to some arms
                if (UnityEngine.Random.value > 0.5f && (selectedParts.Count > arm + 5))
                {
                    Vector3 endPos = armPos + armDirections[arm] * settings.crossEndPieceDistance;
                    endPos = target.SnapToGrid(endPos);

                    // Ensure position is not occupied
                    int maxEndAttempts = 50; // Safety counter to prevent infinite loops
                    int endAttempts = 0;
                    while (target.IsPositionOccupied(endPos) && endAttempts < maxEndAttempts)
                    {
                        endPos += armDirections[arm] * settings.crossEndPieceDistance * 0.5f;
                        endPos = target.SnapToGrid(endPos);
                        endAttempts++;
                    }
                    
                    if (endAttempts >= maxEndAttempts)
                    {
                        Debug.LogWarning($"ProceduralShipLayoutGenerator: Could not find free position for end piece on arm {arm} in CrossShipLayout after {maxEndAttempts} attempts. Using occupied position.");
                    }

                    var endPart = selectedParts[arm + 5];
                    bool isEndStart = IsSystemPart(endPart) && systemsCreated < systemCount;
                    if (isEndStart) systemsCreated++;
                    
                    var endNode = target.CreateShipPartNode(endPart, endPos, isEndStart);
                    target.CreateAutoConnection(armNode.id, endNode.id);
                }
            }
        }

        private void GenerateClusterShipLayout(IShipLayoutTarget target)
        {
            var engineParts = EnginePartCatalog.GetAllEngineParts();
            var selectedParts = GetRandomShipParts(engineParts, UnityEngine.Random.Range(settings.clusterPartCountRange.x, settings.clusterPartCountRange.y));
            var systemCount = UnityEngine.Random.Range(settings.systemCountRange.x, settings.systemCountRange.y + 1);

            Vector3 centerPos = Vector3.zero;
            int systemsCreated = 0;

            // Central hub - mark as start node if it's a system part
            var hubPart = selectedParts[0];
            bool isHubStart = IsSystemPart(hubPart) && systemsCreated < systemCount;
            if (isHubStart) systemsCreated++;
            
            var hubNode = target.CreateShipPartNode(hubPart, centerPos, isHubStart);
            var createdNodes = new List<SpriteNode.NodeData> { hubNode };

            // Surrounding nodes in circular pattern
            for (int i = 1; i < selectedParts.Count; i++)
            {
                float angle = (i - 1) * (360f / (selectedParts.Count - 1)) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * settings.clusterRadius;
                Vector3 position = centerPos + offset;
                position = target.SnapToGrid(position);

                // Ensure position is not occupied
                int maxAttempts = 50; // Safety counter to prevent infinite loops
                int attempts = 0;
                while (target.IsPositionOccupied(position) && attempts < maxAttempts)
                {
                    // Move slightly outward and try again
                    offset *= 1.2f;
                    position = centerPos + offset;
                    position = target.SnapToGrid(position);
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"ProceduralShipLayoutGenerator: Could not find free position for part {i} in ClusterShipLayout after {maxAttempts} attempts. Using occupied position.");
                }

                var part = selectedParts[i];
                bool isPartStart = IsSystemPart(part) && systemsCreated < systemCount;
                if (isPartStart) systemsCreated++;
                
                var nodeData = target.CreateShipPartNode(part, position, isPartStart);
                createdNodes.Add(nodeData);

                // Connect some nodes to hub, others to adjacent nodes
                if (UnityEngine.Random.value < settings.clusterHubConnectionChance)
                {
                    target.CreateAutoConnection(hubNode.id, nodeData.id);
                }
                else if (i > 1)
                {
                    // Connect to previous node in the circle
                    var previousNodeId = createdNodes[i - 1].id;
                    target.CreateAutoConnection(previousNodeId, nodeData.id);
                }
            }
        }

        private void GenerateRingShipLayout(IShipLayoutTarget target)
        {
            var engineParts = EnginePartCatalog.GetAllEngineParts();
            var selectedParts = GetRandomShipParts(engineParts, UnityEngine.Random.Range(settings.ringPartCountRange.x, settings.ringPartCountRange.y));
            var systemCount = UnityEngine.Random.Range(settings.systemCountRange.x, settings.systemCountRange.y + 1);

            Vector3 centerPos = Vector3.zero;
            int systemsCreated = 0;

            SpriteNode.NodeData firstNode = null;
            SpriteNode.NodeData previousNode = null;

            for (int i = 0; i < selectedParts.Count; i++)
            {
                float angle = i * (360f / selectedParts.Count) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * settings.ringRadius;
                Vector3 position = centerPos + offset;
                position = target.SnapToGrid(position);

                // Ensure position is not occupied
                int maxAttempts = 50; // Safety counter to prevent infinite loops
                int attempts = 0;
                while (target.IsPositionOccupied(position) && attempts < maxAttempts)
                {
                    // Move slightly outward and try again
                    offset *= 1.1f;
                    position = centerPos + offset;
                    position = target.SnapToGrid(position);
                    attempts++;
                }
                
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"ProceduralShipLayoutGenerator: Could not find free position for part {i} in RingShipLayout after {maxAttempts} attempts. Using occupied position.");
                }

                var part = selectedParts[i];
                bool isPartStart = IsSystemPart(part) && systemsCreated < systemCount;
                if (isPartStart) systemsCreated++;
                
                var nodeData = target.CreateShipPartNode(part, position, isPartStart);

                if (i == 0)
                {
                    firstNode = nodeData;
                }
                else
                {
                    target.CreateAutoConnection(previousNode.id, nodeData.id);
                }

                previousNode = nodeData;
            }

            // Close the ring
            if (firstNode != null && previousNode != null && selectedParts.Count > 2)
            {
                target.CreateAutoConnection(previousNode.id, firstNode.id);
            }
        }

        private List<EnginePartNodeData> GetRandomShipParts(List<EnginePartNodeData> allParts, int count)
        {
            var result = new List<EnginePartNodeData>();
            var availableParts = new List<EnginePartNodeData>(allParts);

            // Ensure we have multiple start nodes (systems) - command/core/control parts
            var systemCount = UnityEngine.Random.Range(settings.systemCountRange.x, settings.systemCountRange.y + 1);
            var coreParts = availableParts.Where(p => 
                p.category.Contains("Command") || 
                p.category.Contains("Core") || 
                p.category.Contains("Control") ||
                p.category.Contains("Navigation") ||
                p.category.Contains("Power")).ToList();
            
            // Add multiple start nodes (systems)
            int addedSystems = 0;
            for (int i = 0; i < systemCount && coreParts.Count > 0 && count > 0; i++)
            {
                var randomCorePart = coreParts[UnityEngine.Random.Range(0, coreParts.Count)];
                result.Add(randomCorePart);
                coreParts.Remove(randomCorePart); // Avoid duplicate systems
                availableParts.Remove(randomCorePart);
                count--;
                addedSystems++;
            }

            // Ensure we have at least minimum parts
            if (count < (settings.minimumTotalParts - addedSystems))
            {
                count = settings.minimumTotalParts - addedSystems;
            }

            // Fill remaining slots with random parts
            for (int i = 0; i < count && availableParts.Count > 0; i++)
            {
                var randomPart = availableParts[UnityEngine.Random.Range(0, availableParts.Count)];
                result.Add(randomPart);
                availableParts.Remove(randomPart); // Avoid duplicates
            }

            return result;
        }

        private bool IsSystemPart(EnginePartNodeData part)
        {
            return part.category.Contains("Command") || 
                   part.category.Contains("Core") || 
                   part.category.Contains("Control") ||
                   part.category.Contains("Navigation") ||
                   part.category.Contains("Power");
        }

        private GeneratorSettings CreateDefaultSettings()
        {
            return new GeneratorSettings();
        }
    }
}