using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// JSON data structures for ship parts block layouts
    /// </summary>
    [System.Serializable]
    public class ShipPartLayoutData
    {
        public string name;
        public string slug;
        public string category;
        public int[][] coords;
        public int block_count;
        public bool center_is_block;
        public int tile_size;
    }

    [System.Serializable]
    public class ShipPartConventions
    {
        public string origin;
        public int tile_size;
        public string units;
        public string note;
    }

    [System.Serializable]
    public class ShipPartsBlockLayoutsJson
    {
        public ShipPartConventions conventions;
        public ShipPartLayoutData[] parts;
    }

    /// <summary>
    /// Universal composite renderer for ALL ship parts, rendering them as individual engine_block.png sprites
    /// arranged in patterns that reflect the purpose of each ship part category
    /// </summary>
    public class CompositeShipPartRenderer : MonoBehaviour
    {
        [System.Serializable]
        public class PartBlock
        {
            public Vector2Int gridPosition; // Position within the part's grid
            public GameObject blockGameObject;
            public SpriteRenderer blockRenderer;
            public BoxCollider2D blockCollider;
            public List<int> availableAnchorIndices = new List<int>(); // Anchor indices available on this block
        }

        [Header("Ship Part Block Configuration")]
        public UnityEngine.Sprite partBlockSprite; // Reference to engine_block.png
        public float blockSize = 0.5f; // Size of each block in world units
        
        private SpriteNode parentNode;
        private List<PartBlock> partBlocks = new List<PartBlock>();
        private Vector2Int partGridSize;
        
        // Static data for JSON layout loading
        private static Dictionary<string, ShipPartLayoutData> shipPartLayouts = null;
        private static bool layoutsLoaded = false;
        
        public List<PartBlock> PartBlocks => partBlocks;
        public Vector2Int PartGridSize => partGridSize;

        public void Initialize(SpriteNode node)
        {
            parentNode = node;
            LoadShipPartLayouts();
            LoadPartBlockSprite();
            GeneratePartBlocks();
        }

        private static void LoadShipPartLayouts()
        {
            if (layoutsLoaded) return;

            try
            {
                string jsonPath = Path.Combine(Application.dataPath, "ship_parts_block_layouts.json");
                if (File.Exists(jsonPath))
                {
                    string jsonContent = File.ReadAllText(jsonPath);
                    ShipPartsBlockLayoutsJson layoutsJson = JsonConvert.DeserializeObject<ShipPartsBlockLayoutsJson>(jsonContent);
                    
                    shipPartLayouts = new Dictionary<string, ShipPartLayoutData>();
                    if (layoutsJson != null && layoutsJson.parts != null)
                    {
                        foreach (var part in layoutsJson.parts)
                        {
                            if (!string.IsNullOrEmpty(part.name))
                            {
                                shipPartLayouts[part.name] = part;
                            }
                        }
                        Debug.Log($"Loaded {shipPartLayouts.Count} ship part layouts from JSON");
                    }
                    else
                    {
                        Debug.LogWarning("ship_parts_block_layouts.json contains invalid data");
                        shipPartLayouts = new Dictionary<string, ShipPartLayoutData>();
                    }
                }
                else
                {
                    Debug.LogWarning("ship_parts_block_layouts.json file not found, using default patterns");
                    shipPartLayouts = new Dictionary<string, ShipPartLayoutData>();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load ship_parts_block_layouts.json: {ex.Message}");
                shipPartLayouts = new Dictionary<string, ShipPartLayoutData>();
            }

            layoutsLoaded = true;
        }

        private void LoadPartBlockSprite()
        {
            if (partBlockSprite == null)
            {
                // Try to load engine_block.png from Resources folder
                partBlockSprite = Resources.Load<UnityEngine.Sprite>("engine_block");
                
                if (partBlockSprite == null)
                {
                    // Try alternative paths in Resources
                    partBlockSprite = Resources.Load<UnityEngine.Sprite>("Sprites/engine_block");
                }
                
                if (partBlockSprite == null)
                {
                    // Try loading from CommonWealth Art folder via Resources.LoadAll
                    UnityEngine.Sprite[] allSprites = Resources.LoadAll<UnityEngine.Sprite>("");
                    foreach (var sprite in allSprites)
                    {
                        if (sprite.name.Contains("engine_block"))
                        {
                            partBlockSprite = sprite;
                            break;
                        }
                    }
                }
                
                if (partBlockSprite == null)
                {
                    Debug.LogWarning("engine_block.png sprite not found in Resources. Creating placeholder sprite.");
                    partBlockSprite = CreatePlaceholderBlockSprite();
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

        private void GeneratePartBlocks()
        {
            // Clear existing blocks
            ClearExistingBlocks();
            
            // Determine part pattern based on category and type
            var blockPattern = GetPartBlockPattern();
            partGridSize = CalculateGridSize(blockPattern);
            
            // Create blocks based on pattern
            foreach (var blockPos in blockPattern)
            {
                CreatePartBlock(blockPos);
            }
            
            // Update parent node collider to encompass all blocks
            UpdateParentCollider();
        }

        private List<Vector2Int> GetPartBlockPattern()
        {
            var pattern = new List<Vector2Int>();
            string partName = parentNode.NodeDataInstance.title;
            
            // First, try to get pattern from JSON data
            if (shipPartLayouts != null && shipPartLayouts.ContainsKey(partName))
            {
                pattern = GetPatternFromJson(shipPartLayouts[partName]);
                if (pattern.Count > 0)
                {
                    return pattern;
                }
            }
            
            // Fallback to hardcoded patterns
            string category = GetPartCategory();
            string partNameLower = partName.ToLowerInvariant();
            
            // Handle engine parts with existing patterns
            if (parentNode.NodeDataInstance.isEngine)
            {
                return GetEngineBlockPattern(parentNode.NodeDataInstance.engineType);
            }
            
            // Handle all other ship part categories
            switch (category)
            {
                case "Power & Energy":
                    pattern = GetPowerEnergyPattern(partNameLower);
                    break;
                case "Thermal & Coolant":
                    pattern = GetThermalCoolantPattern(partNameLower);
                    break;
                case "Atmosphere & Life Support":
                    pattern = GetLifeSupportPattern(partNameLower);
                    break;
                case "Structural & Hull":
                    pattern = GetStructuralHullPattern(partNameLower);
                    break;
                case "Navigation, Comms & Sensors":
                    pattern = GetNavCommsPattern(partNameLower);
                    break;
                case "Data, Control & Security":
                    pattern = GetDataControlPattern(partNameLower);
                    break;
                case "Manufacturing, Inventory & Logistics":
                    pattern = GetManufacturingPattern(partNameLower);
                    break;
                case "Defense & Shielding":
                    pattern = GetDefensePattern(partNameLower);
                    break;
                default:
                    // Default pattern for unknown categories
                    pattern = GetDefaultPattern();
                    break;
            }
            
            return pattern;
        }

        private List<Vector2Int> GetPatternFromJson(ShipPartLayoutData layoutData)
        {
            var pattern = new List<Vector2Int>();
            
            if (layoutData.coords == null || layoutData.coords.Length == 0)
            {
                return pattern;
            }
            
            // First pass: find the bounds to determine offset needed
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            
            foreach (var coord in layoutData.coords)
            {
                if (coord.Length >= 2)
                {
                    int x = coord[0];
                    int y = coord[1];
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
            
            // Calculate offset to make all coordinates positive (grid system requirement)
            int offsetX = -minX;
            int offsetY = -minY;
            
            // Convert JSON coordinates (center at 0,0) to positive grid positions
            // JSON uses x-right, y-up convention, Unity uses same convention
            foreach (var coord in layoutData.coords)
            {
                if (coord.Length >= 2)
                {
                    // JSON coordinates are relative to center (0,0)
                    // Add offset to make all coordinates positive for grid system
                    int x = coord[0] + offsetX;
                    int y = coord[1] + offsetY;
                    pattern.Add(new Vector2Int(x, y));
                }
            }
            
            return pattern;
        }

        private string GetPartCategory()
        {
            // Extract category from metadata or use a fallback method
            foreach (var metadata in parentNode.NodeDataInstance.metadata)
            {
                if (metadata.key == "Category")
                {
                    return metadata.value;
                }
            }
            
            // Fallback: try to determine category from part name
            string partName = parentNode.NodeDataInstance.title.ToLowerInvariant();
            
            if (partName.Contains("reactor") || partName.Contains("power") || partName.Contains("battery") || 
                partName.Contains("capacitor") || partName.Contains("inverter"))
                return "Power & Energy";
            if (partName.Contains("coolant") || partName.Contains("heat") || partName.Contains("thermal") || 
                partName.Contains("radiator"))
                return "Thermal & Coolant";
            if (partName.Contains("o₂") || partName.Contains("co₂") || partName.Contains("air") || 
                partName.Contains("atmosphere") || partName.Contains("life") || partName.Contains("filter"))
                return "Atmosphere & Life Support";
            if (partName.Contains("hull") || partName.Contains("structural") || partName.Contains("bulkhead") || 
                partName.Contains("armor"))
                return "Structural & Hull";
            if (partName.Contains("navigation") || partName.Contains("sensor") || partName.Contains("antenna") || 
                partName.Contains("comms") || partName.Contains("tracker"))
                return "Navigation, Comms & Sensors";
            if (partName.Contains("control") || partName.Contains("security") || partName.Contains("firewall") || 
                partName.Contains("memory") || partName.Contains("router"))
                return "Data, Control & Security";
            if (partName.Contains("fabricator") || partName.Contains("manufacturing") || partName.Contains("inventory") || 
                partName.Contains("salvage") || partName.Contains("parts"))
                return "Manufacturing, Inventory & Logistics";
            if (partName.Contains("shield") || partName.Contains("defense") || partName.Contains("protection"))
                return "Defense & Shielding";
                
            return "Unknown";
        }

        private List<Vector2Int> GetEngineBlockPattern(SpriteNode.EngineType engineType)
        {
            var pattern = new List<Vector2Int>();
            
            switch (engineType)
            {
                case SpriteNode.EngineType.MainEngine:
                    // Rectangular pattern (3x2)
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
                    // Diamond pattern
                    pattern.Add(new Vector2Int(1, 0)); // Top
                    pattern.Add(new Vector2Int(0, 1)); // Left
                    pattern.Add(new Vector2Int(2, 1)); // Right
                    pattern.Add(new Vector2Int(1, 2)); // Bottom
                    break;
                    
                case SpriteNode.EngineType.StabilityEngine:
                    // Cross pattern
                    pattern.Add(new Vector2Int(1, 0)); // Top
                    pattern.Add(new Vector2Int(0, 1)); // Left
                    pattern.Add(new Vector2Int(1, 1)); // Center
                    pattern.Add(new Vector2Int(2, 1)); // Right
                    pattern.Add(new Vector2Int(1, 2)); // Bottom
                    break;
            }
            
            return pattern;
        }

        private List<Vector2Int> GetPowerEnergyPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("reactor") || partName.Contains("core"))
            {
                // Large central pattern for reactors (3x3)
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("battery rack"))
            {
                // Backwards capital E pattern for battery rack
                // E shape: ███ (top)
                //          █   (middle-top)
                //          ██  (middle)
                //          █   (middle-bottom)
                //          ███ (bottom)
                pattern.Add(new Vector2Int(0, 0)); // Top left
                pattern.Add(new Vector2Int(1, 0)); // Top center
                pattern.Add(new Vector2Int(2, 0)); // Top right
                pattern.Add(new Vector2Int(0, 1)); // Middle-top left
                pattern.Add(new Vector2Int(0, 2)); // Middle left
                pattern.Add(new Vector2Int(1, 2)); // Middle center
                pattern.Add(new Vector2Int(0, 3)); // Middle-bottom left
                pattern.Add(new Vector2Int(0, 4)); // Bottom left
                pattern.Add(new Vector2Int(1, 4)); // Bottom center
                pattern.Add(new Vector2Int(2, 4)); // Bottom right
            }
            else if (partName.Contains("capacitor") || partName.Contains("battery"))
            {
                // Rectangular storage pattern (2x3)
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("inverter") || partName.Contains("controller"))
            {
                // T-shaped pattern for controllers
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 0)); // Top
            }
            else
            {
                // Default 2x2 pattern for other power components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetThermalCoolantPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("radiator") || partName.Contains("panel"))
            {
                // Long linear pattern for radiator panels (1x4)
                for (int y = 0; y < 4; y++)
                {
                    pattern.Add(new Vector2Int(0, y));
                }
            }
            else if (partName.Contains("heat sink") || partName.Contains("exchanger"))
            {
                // Rectangular heat sink pattern (3x2)
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("pump"))
            {
                // Circular-ish pump pattern
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
            }
            else
            {
                // Default 2x2 pattern for other thermal components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetLifeSupportPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("generator") || partName.Contains("electrolyzer"))
            {
                // H-pattern for generators
                pattern.Add(new Vector2Int(0, 0)); // Top left
                pattern.Add(new Vector2Int(0, 1)); // Middle left
                pattern.Add(new Vector2Int(0, 2)); // Bottom left
                pattern.Add(new Vector2Int(1, 1)); // Middle center
                pattern.Add(new Vector2Int(2, 0)); // Top right
                pattern.Add(new Vector2Int(2, 1)); // Middle right
                pattern.Add(new Vector2Int(2, 2)); // Bottom right
            }
            else if (partName.Contains("filter") || partName.Contains("scrubber"))
            {
                // Honeycomb pattern for filters
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(0, 2)); // Bottom left
                pattern.Add(new Vector2Int(2, 2)); // Bottom right
            }
            else if (partName.Contains("blower") || partName.Contains("circulation"))
            {
                // Fan blade pattern
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
                pattern.Add(new Vector2Int(0, 1)); // Left
            }
            else
            {
                // Default 2x2 pattern for other life support components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetStructuralHullPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("hull") || partName.Contains("plate") || partName.Contains("armor"))
            {
                // Large rectangular armor pattern (4x2)
                for (int x = 0; x < 4; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("bulkhead") || partName.Contains("door"))
            {
                // Vertical door pattern (1x3)
                for (int y = 0; y < 3; y++)
                {
                    pattern.Add(new Vector2Int(0, y));
                }
            }
            else if (partName.Contains("shield") || partName.Contains("whipple"))
            {
                // Diamond shield pattern
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
            }
            else
            {
                // Default 2x2 pattern for other structural components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetNavCommsPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("antenna") || partName.Contains("transceiver"))
            {
                // Antenna pattern (cross with extension)
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
                pattern.Add(new Vector2Int(1, 3)); // Extended bottom
            }
            else if (partName.Contains("array") || partName.Contains("sensor"))
            {
                // Array pattern (3x2 grid)
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("computer") || partName.Contains("processor"))
            {
                // Rectangular computer pattern (2x2)
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else
            {
                // Default compact pattern for other nav/comms components
                pattern.Add(new Vector2Int(0, 0));
                pattern.Add(new Vector2Int(1, 0));
                pattern.Add(new Vector2Int(0, 1));
            }
            
            return pattern;
        }

        private List<Vector2Int> GetDataControlPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("router") || partName.Contains("backplane"))
            {
                // Network router pattern (plus shape)
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
            }
            else if (partName.Contains("memory") || partName.Contains("bank"))
            {
                // Memory bank pattern (4x1)
                for (int x = 0; x < 4; x++)
                {
                    pattern.Add(new Vector2Int(x, 0));
                }
            }
            else if (partName.Contains("firewall") || partName.Contains("security"))
            {
                // Security barrier pattern
                for (int x = 0; x < 3; x++)
                {
                    pattern.Add(new Vector2Int(x, 0));
                    if (x % 2 == 0) pattern.Add(new Vector2Int(x, 1));
                }
            }
            else
            {
                // Default 2x2 pattern for other data/control components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetManufacturingPattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("fabricator") || partName.Contains("printer"))
            {
                // Large fabricator pattern (3x3)
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("locker") || partName.Contains("inventory"))
            {
                // Storage locker pattern (2x3)
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else if (partName.Contains("drone") || partName.Contains("salvage"))
            {
                // Drone pattern (T-shape)
                pattern.Add(new Vector2Int(0, 1)); // Left wing
                pattern.Add(new Vector2Int(1, 1)); // Body center
                pattern.Add(new Vector2Int(2, 1)); // Right wing
                pattern.Add(new Vector2Int(1, 0)); // Head
            }
            else
            {
                // Default 2x2 pattern for other manufacturing components
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return pattern;
        }

        private List<Vector2Int> GetDefensePattern(string partName)
        {
            var pattern = new List<Vector2Int>();
            
            if (partName.Contains("shield") && partName.Contains("emitter"))
            {
                // Shield emitter star pattern
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(0, 0)); // Top-left
                pattern.Add(new Vector2Int(2, 0)); // Top-right
                pattern.Add(new Vector2Int(0, 2)); // Bottom-left
                pattern.Add(new Vector2Int(2, 2)); // Bottom-right
            }
            else if (partName.Contains("capacitor"))
            {
                // Shield capacitor pattern (rectangular)
                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        pattern.Add(new Vector2Int(x, y));
                    }
                }
            }
            else
            {
                // Default defensive pattern
                pattern.Add(new Vector2Int(1, 0)); // Top
                pattern.Add(new Vector2Int(0, 1)); // Left
                pattern.Add(new Vector2Int(1, 1)); // Center
                pattern.Add(new Vector2Int(2, 1)); // Right
                pattern.Add(new Vector2Int(1, 2)); // Bottom
            }
            
            return pattern;
        }

        private List<Vector2Int> GetDefaultPattern()
        {
            // Simple 2x2 default pattern
            var pattern = new List<Vector2Int>();
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    pattern.Add(new Vector2Int(x, y));
                }
            }
            return pattern;
        }

        private Vector2Int CalculateGridSize(List<Vector2Int> pattern)
        {
            if (pattern.Count == 0) return new Vector2Int(1, 1);
            
            int maxX = 0, maxY = 0;
            foreach (var pos in pattern)
            {
                maxX = Mathf.Max(maxX, pos.x);
                maxY = Mathf.Max(maxY, pos.y);
            }
            
            return new Vector2Int(maxX + 1, maxY + 1);
        }

        private void CreatePartBlock(Vector2Int gridPosition)
        {
            // Create block GameObject
            var blockGO = new GameObject($"PartBlock_{gridPosition.x}_{gridPosition.y}");
            blockGO.transform.SetParent(transform, false);
            
            // Position block relative to parent
            Vector3 blockWorldPos = new Vector3(
                (gridPosition.x - partGridSize.x * 0.5f + 0.5f) * blockSize,
                (gridPosition.y - partGridSize.y * 0.5f + 0.5f) * blockSize,
                0
            );
            blockGO.transform.localPosition = blockWorldPos;
            blockGO.transform.localScale = new Vector3(0.5f, 0.5f, 1.0f);
            
            // Add sprite renderer
            var spriteRenderer = blockGO.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = partBlockSprite;
            spriteRenderer.color = GetCategoryColor();
            
            // Calculate scale based on tile_size from JSON data
            float scale = GetTileScale();
            spriteRenderer.GetComponent<Transform>().localScale = new Vector3(scale, scale, 1.0f);
            spriteRenderer.sortingOrder = parentNode.GetComponent<SpriteRenderer>().sortingOrder + 1;
            
            // Add collider for individual block interactions
            var collider = blockGO.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * blockSize;
            
            // Create part block data
            var partBlock = new PartBlock
            {
                gridPosition = gridPosition,
                blockGameObject = blockGO,
                blockRenderer = spriteRenderer,
                blockCollider = collider,
                availableAnchorIndices = GenerateBlockAnchorIndices(gridPosition)
            };
            
            partBlocks.Add(partBlock);
        }

        private Color GetCategoryColor()
        {
            string category = GetPartCategory();
            
            // Use colors from EnginePartCatalog
            return category switch
            {
                "Power & Energy" => new Color(1f, 0.8f, 0.2f, 1f), // Golden yellow
                "Thermal & Coolant" => new Color(0.2f, 0.6f, 1f, 1f), // Cool blue
                "Atmosphere & Life Support" => new Color(0.4f, 0.9f, 0.4f, 1f), // Life green
                "Structural & Hull" => new Color(0.7f, 0.7f, 0.7f, 1f), // Steel gray
                "Propulsion & Maneuvering" => new Color(1f, 0.4f, 0.2f, 1f), // Engine orange
                "Navigation, Comms & Sensors" => new Color(0.6f, 0.4f, 1f, 1f), // Tech purple
                "Data, Control & Security" => new Color(0.9f, 0.2f, 0.9f, 1f), // Cyber magenta
                "Manufacturing, Inventory & Logistics" => new Color(0.8f, 0.6f, 0.2f, 1f), // Industrial bronze
                "Defense & Shielding" => new Color(1f, 0.2f, 0.2f, 1f), // Shield red
                _ => parentNode.NodeDataInstance.color // Fallback to node's color
            };
        }

        private float GetTileScale()
        {
            // Try to get tile_size from JSON data for this specific part
           // string partName = parentNode.NodeDataInstance.title;
            //if (shipPartLayouts != null && shipPartLayouts.ContainsKey(partName))
            //{
            //    var layoutData = shipPartLayouts[partName];
                // Scale based on tile_size: 32px = 0.5f scale, 64px = 1.0f scale
            //    return layoutData.tile_size / 64.0f;
            //}
            
            // Fallback to default scale for 32px tiles
            return 0.5f;
        }

        private List<int> GenerateBlockAnchorIndices(Vector2Int gridPosition)
        {
            // Generate unique anchor indices for this block
            // Each block gets 4 anchors (one per side)
            var indices = new List<int>();
            int baseIndex = (gridPosition.y * partGridSize.x + gridPosition.x) * 4;
            
            for (int i = 0; i < 4; i++)
            {
                indices.Add(baseIndex + i);
            }
            
            return indices;
        }

        private void ClearExistingBlocks()
        {
            foreach (var block in partBlocks)
            {
                if (block.blockGameObject != null)
                {
                    DestroyImmediate(block.blockGameObject);
                }
            }
            partBlocks.Clear();
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
                    
                    foreach (var block in partBlocks)
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
            foreach (var block in partBlocks)
            {
                if (block.blockRenderer != null)
                {
                    block.blockRenderer.color = newColor;
                }
            }
        }

        public void ApplyRotation(float rotationDegrees)
        {
            // Rotate all blocks around the center of the part
            transform.rotation = Quaternion.Euler(0, 0, rotationDegrees);
        }

        public PartBlock GetBlockAtPosition(Vector3 worldPosition)
        {
            foreach (var block in partBlocks)
            {
                if (block.blockCollider != null && 
                    block.blockCollider.bounds.Contains(worldPosition))
                {
                    return block;
                }
            }
            return null;
        }

        public Vector3[] GetBlockAnchorPositions(PartBlock block)
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