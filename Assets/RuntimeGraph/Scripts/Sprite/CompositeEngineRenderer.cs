using System.Collections.Generic;
using UnityEngine;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Handles rendering engine parts as composite sprites made from individual engine_block.png sprites
    /// Each engine block can have individual connections while moving together as a unit
    /// </summary>
    public class CompositeEngineRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class EngineBlock
        {
            public Vector2Int gridPosition; // Position within the engine's grid
            public GameObject blockGameObject;
            public SpriteRenderer blockRenderer;
            public BoxCollider2D blockCollider;
            public List<int> availableAnchorIndices = new List<int>(); // Anchor indices available on this block
        }

        [Header("Engine Block Configuration")]
        public UnityEngine.Sprite engineBlockSprite; // Reference to engine_block.png
        public float blockSize = 1f; // Size of each block in world units
        
        private SpriteNode parentNode;
        private List<EngineBlock> engineBlocks = new List<EngineBlock>();
        private Vector2Int engineGridSize;
        private LayerMask partBlockLayer;

        public List<EngineBlock> EngineBlocks => engineBlocks;
        public Vector2Int EngineGridSize => engineGridSize;

        public void Initialize(SpriteNode node, LayerMask partBlockLayer)
        {
            this.partBlockLayer = partBlockLayer;
            parentNode = node;
            LoadEngineBlockSprite();
            GenerateEngineBlocks();
        }

        private void LoadEngineBlockSprite()
        {
            if (engineBlockSprite == null)
            {
                // Try to load engine_block.png from Resources folder
                engineBlockSprite = Resources.Load<UnityEngine.Sprite>("engine_block");
                
                if (engineBlockSprite == null)
                {
                    // Try alternative paths in Resources
                    engineBlockSprite = Resources.Load<UnityEngine.Sprite>("Sprites/engine_block");
                }
                
                if (engineBlockSprite == null)
                {
                    // Try loading from CommonWealth Art folder via Resources.LoadAll
                    UnityEngine.Sprite[] allSprites = Resources.LoadAll<UnityEngine.Sprite>("");
                    foreach (var sprite in allSprites)
                    {
                        if (sprite.name.Contains("engine_block"))
                        {
                            engineBlockSprite = sprite;
                            break;
                        }
                    }
                }
                
                if (engineBlockSprite == null)
                {
                    Debug.LogWarning("engine_block.png sprite not found in Resources. Creating placeholder sprite.");
                    engineBlockSprite = CreatePlaceholderBlockSprite();
                }
            }
        }

        private UnityEngine.Sprite CreatePlaceholderBlockSprite()
        {
            // Create a simple placeholder sprite if engine_block.png is not found
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var colors = new Color[32 * 32];
            
            // Create a simple square block with border
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool isBorder = x < 1 || x >= 31 || y < 1 || y >= 31;
                    colors[y * 32 + x] = isBorder ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return UnityEngine.Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        private void GenerateEngineBlocks()
        {
            // Clear existing blocks
            ClearExistingBlocks();
            
            // Determine engine shape based on type
            var blockPattern = GetEngineBlockPattern(parentNode.NodeDataInstance.engineType);
            engineGridSize = new Vector2Int(parentNode.NodeDataInstance.gridWidth, parentNode.NodeDataInstance.gridHeight);
            
            // Create blocks based on pattern
            foreach (var blockPos in blockPattern)
            {
                CreateEngineBlock(blockPos);
            }
            
            // Update parent node collider to encompass all blocks
            UpdateParentCollider();
        }

        private List<Vector2Int> GetEngineBlockPattern(SpriteNode.EngineType engineType)
        {
            var pattern = new List<Vector2Int>();
            
            switch (engineType)
            {
                case SpriteNode.EngineType.MainEngine:
                    // Rectangular pattern (3x2 by default)
                    for (int x = 0; x < 3; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            pattern.Add(new Vector2Int(x, y));
                        }
                    }
                    break;
                    
                case SpriteNode.EngineType.Thruster:
                    // Single block pattern
                    pattern.Add(new Vector2Int(0, 0));
                    break;
                    
                case SpriteNode.EngineType.RetroEngine:
                    // Diamond pattern (2x2)
                    pattern.Add(new Vector2Int(0, 1)); // Top
                    pattern.Add(new Vector2Int(1, 0)); // Left
                    pattern.Add(new Vector2Int(1, 2)); // Right
                    pattern.Add(new Vector2Int(2, 1)); // Bottom
                    break;
                    
                case SpriteNode.EngineType.StabilityEngine:
                    // Cross pattern (1x3 vertical + horizontal center)
                    pattern.Add(new Vector2Int(1, 0)); // Top
                    pattern.Add(new Vector2Int(0, 1)); // Left
                    pattern.Add(new Vector2Int(1, 1)); // Center
                    pattern.Add(new Vector2Int(2, 1)); // Right
                    pattern.Add(new Vector2Int(1, 2)); // Bottom
                    break;
            }
            
            return pattern;
        }

        private void CreateEngineBlock(Vector2Int gridPosition)
        {
            // Create block GameObject
            var blockGO = new GameObject($"EngineBlock_{gridPosition.x}_{gridPosition.y}");
            blockGO.transform.SetParent(transform, false);
            
            // Position block relative to parent
            Vector3 blockWorldPos = new Vector3(
                (gridPosition.x - engineGridSize.x * 0.5f + 0.5f) * blockSize,
                (gridPosition.y - engineGridSize.y * 0.5f + 0.5f) * blockSize,
                0
            );
            blockGO.transform.localPosition = blockWorldPos;
            
            // Add sprite renderer
            var spriteRenderer = blockGO.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = engineBlockSprite;
            spriteRenderer.color = parentNode.NodeDataInstance.color;
            spriteRenderer.sortingOrder = parentNode.GetComponent<SpriteRenderer>().sortingOrder + 1;
            
            // Add collider for individual block interactions
            var collider = blockGO.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * blockSize;
            
            // Create engine block data
            var engineBlock = new EngineBlock
            {
                gridPosition = gridPosition,
                blockGameObject = blockGO,
                blockRenderer = spriteRenderer,
                blockCollider = collider,
                availableAnchorIndices = GenerateBlockAnchorIndices(gridPosition)
            };
            
            engineBlocks.Add(engineBlock);
        }

        private List<int> GenerateBlockAnchorIndices(Vector2Int gridPosition)
        {
            // Generate unique anchor indices for this block
            // Each block gets 4 anchors (one per side)
            var indices = new List<int>();
            int baseIndex = (gridPosition.y * engineGridSize.x + gridPosition.x) * 4;
            
            for (int i = 0; i < 4; i++)
            {
                indices.Add(baseIndex + i);
            }
            
            return indices;
        }

        private void ClearExistingBlocks()
        {
            foreach (var block in engineBlocks)
            {
                if (block.blockGameObject != null)
                {
                    DestroyImmediate(block.blockGameObject);
                }
            }
            engineBlocks.Clear();
        }

        private void UpdateParentCollider()
        {
            // Update parent node's collider to encompass all blocks
            if (parentNode != null)
            {
                var nodeCollider = parentNode.GetComponent<BoxCollider2D>();
                if (nodeCollider != null)
                {
                    // Calculate bounds of all blocks
                    Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
                    bool boundsInitialized = false;
                    
                    foreach (var block in engineBlocks)
                    {
                        var blockBounds = new Bounds(block.blockGameObject.transform.localPosition, Vector3.one * blockSize);
                        
                        if (!boundsInitialized)
                        {
                            totalBounds = blockBounds;
                            boundsInitialized = true;
                        }
                        else
                        {
                            totalBounds.Encapsulate(blockBounds);
                        }
                    }
                    
                    nodeCollider.size = totalBounds.size;
                    nodeCollider.offset = totalBounds.center;
                }
            }
        }

        public void UpdateBlockColors(Color newColor)
        {
            foreach (var block in engineBlocks)
            {
                if (block.blockRenderer != null)
                {
                    block.blockRenderer.color = newColor;
                }
            }
        }

        public void ApplyRotation(float rotationDegrees)
        {
            // Rotate all blocks around the center of the engine
            transform.rotation = Quaternion.Euler(0, 0, rotationDegrees);
        }

        public EngineBlock GetBlockAtPosition(Vector3 worldPosition)
        {
            foreach (var block in engineBlocks)
            {
                if (block.blockCollider != null && 
                    block.blockCollider.bounds.Contains(worldPosition))
                {
                    return block;
                }
            }
            return null;
        }

        public Vector3[] GetBlockAnchorPositions(EngineBlock block)
        {
            if (block == null || block.blockGameObject == null) return new Vector3[0];
            
            // Return anchor positions for the four sides of the block
            var blockPos = block.blockGameObject.transform.position;
            var halfSize = blockSize * 0.5f;
            
            return new Vector3[]
            {
                blockPos + Vector3.right * halfSize,  // Right
                blockPos + Vector3.up * halfSize,     // Top
                blockPos + Vector3.left * halfSize,   // Left
                blockPos + Vector3.down * halfSize    // Bottom
            };
        }

        public void OnDestroy()
        {
            ClearExistingBlocks();
        }
    }
}