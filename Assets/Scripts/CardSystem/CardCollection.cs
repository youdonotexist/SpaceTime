using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Manages the global collection of available cards
    /// Handles card discovery, rarity distribution, and card pack generation
    /// </summary>
    public class CardCollection : MonoBehaviour
    {
        [Header("Card Collection Configuration")]
        public List<ShipPartCard> allAvailableCards = new List<ShipPartCard>();
        public List<ShipPartCard> playerOwnedCards = new List<ShipPartCard>();
        
        [Header("Rarity Drop Rates")]
        [Range(0f, 1f)] public float commonDropRate = 0.6f;
        [Range(0f, 1f)] public float uncommonDropRate = 0.25f;
        [Range(0f, 1f)] public float rareDropRate = 0.12f;
        [Range(0f, 1f)] public float epicDropRate = 0.025f;
        [Range(0f, 1f)] public float legendaryDropRate = 0.005f;
        
        [Header("Card Pack Configuration")]
        public int cardsPerPack = 5;
        public int guaranteedUncommonOrBetter = 1;
        
        // Events
        public System.Action<ShipPartCard> OnCardDiscovered;
        public System.Action<List<ShipPartCard>> OnCollectionUpdated;
        public System.Action<List<ShipPartCard>> OnCardPackOpened;
        
        // Singleton instance
        private static CardCollection instance;
        public static CardCollection Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CardCollection>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("CardCollection");
                        instance = go.AddComponent<CardCollection>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCollection();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Initializes the card collection by converting existing ship parts to cards
        /// </summary>
        private void InitializeCollection()
        {
            // Auto-generate cards from existing ship parts if collection is empty
            if (allAvailableCards.Count == 0)
            {
                GenerateCardsFromExistingParts();
            }
            
            // Load player's owned cards from save data (placeholder for now)
            LoadPlayerCollection();
        }
        
        /// <summary>
        /// Generates cards from existing ship parts in the system
        /// </summary>
        private void GenerateCardsFromExistingParts()
        {
            Debug.Log("Generating cards from existing ship parts...");
            
            try
            {
                // Get all engine parts from the existing catalog
                var allParts = RuntimeGraph.Sprite.EnginePartCatalog.GetAllEngineParts();
                
                foreach (var part in allParts)
                {
                    // Convert to NodeTypeData first
                    var nodeData = new RuntimeGraph.Sprite.SpriteNodePalette.NodeTypeData
                    {
                        name = part.name,
                        category = part.category,
                        color = part.color,
                        description = part.description,
                        // Set default MIDI values
                        note = Random.Range(36, 84),
                        velocity = Random.Range(60, 100),
                        channel = GetChannelForCategory(part.category),
                        duration = 0.08f
                    };
                    
                    // Create card from node data
                    var card = ShipPartCard.FromNodeTypeData(nodeData);
                    
                    // Assign rarity based on category and name
                    card.rarity = DetermineRarity(part);
                    card.manaCost = CalculateManaCost(card.rarity, part.category);
                    card.cardArtwork = part.icon;
                    
                    allAvailableCards.Add(card);
                }
                
                Debug.Log($"Generated {allAvailableCards.Count} cards from existing ship parts");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate cards from existing parts: {e.Message}");
                // Create some default cards as fallback
                CreateDefaultCards();
            }
        }
        
        /// <summary>
        /// Creates default cards if part generation fails
        /// </summary>
        private void CreateDefaultCards()
        {
            var defaultCard = ScriptableObject.CreateInstance<ShipPartCard>();
            defaultCard.cardName = "Basic Engine";
            defaultCard.partName = "Basic Engine";
            defaultCard.partCategory = "Engine";
            defaultCard.rarity = CardRarity.Common;
            defaultCard.manaCost = 1;
            defaultCard.cardDescription = "A basic engine component";
            
            allAvailableCards.Add(defaultCard);
        }
        
        /// <summary>
        /// Determines card rarity based on part properties
        /// </summary>
        private CardRarity DetermineRarity(object part)
        {
            // Use reflection to get part name safely
            string partName = "";
            try
            {
                System.Reflection.MemberInfo nameMember = (System.Reflection.MemberInfo)part.GetType().GetProperty("name") ?? 
                                                          (System.Reflection.MemberInfo)part.GetType().GetField("name");

                if (nameMember != null)
                {
                    if (nameMember is System.Reflection.PropertyInfo prop)
                        partName = prop.GetValue(part)?.ToString() ?? "";
                    else if (nameMember is System.Reflection.FieldInfo field)
                        partName = field.GetValue(part)?.ToString() ?? "";
                }
            }
            catch
            {
                partName = "";
            }
            
            partName = partName.ToLower();
            
            // Determine rarity based on name patterns
            if (partName.Contains("legendary") || partName.Contains("ultimate"))
                return CardRarity.Legendary;
            if (partName.Contains("epic") || partName.Contains("advanced") || partName.Contains("quantum"))
                return CardRarity.Epic;
            if (partName.Contains("rare") || partName.Contains("enhanced") || partName.Contains("improved"))
                return CardRarity.Rare;
            if (partName.Contains("uncommon") || partName.Contains("upgraded") || partName.Contains("mk2"))
                return CardRarity.Uncommon;
            
            return CardRarity.Common;
        }
        
        /// <summary>
        /// Calculates mana cost based on rarity and category
        /// </summary>
        private int CalculateManaCost(CardRarity rarity, string category)
        {
            int baseCost = 1;
            
            // Category-based cost modifiers
            switch (category.ToLower())
            {
                case "weapon": baseCost = 3; break;
                case "engine": baseCost = 2; break;
                case "shield": baseCost = 2; break;
                case "power": baseCost = 1; break;
                default: baseCost = 1; break;
            }
            
            // Rarity multiplier
            float rarityMultiplier = rarity switch
            {
                CardRarity.Common => 1.0f,
                CardRarity.Uncommon => 1.2f,
                CardRarity.Rare => 1.5f,
                CardRarity.Epic => 2.0f,
                CardRarity.Legendary => 3.0f,
                _ => 1.0f
            };
            
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * rarityMultiplier));
        }
        
        /// <summary>
        /// Gets MIDI channel based on part category
        /// </summary>
        private int GetChannelForCategory(string category)
        {
            return category.ToLower() switch
            {
                "engine" => 1,
                "weapon" => 2,
                "shield" => 3,
                "power" => 4,
                "life support" => 5,
                "navigation" => 6,
                _ => 1
            };
        }
        
        /// <summary>
        /// Adds a card to the player's collection
        /// </summary>
        public void AddCardToCollection(ShipPartCard card)
        {
            if (!playerOwnedCards.Contains(card))
            {
                playerOwnedCards.Add(card);
                OnCardDiscovered?.Invoke(card);
                OnCollectionUpdated?.Invoke(playerOwnedCards);
                
                SavePlayerCollection();
            }
        }
        
        /// <summary>
        /// Generates a random card pack
        /// </summary>
        public List<ShipPartCard> GenerateCardPack()
        {
            var pack = new List<ShipPartCard>();
            var availableByRarity = GroupCardsByRarity();
            
            // Ensure at least one uncommon or better
            for (int i = 0; i < guaranteedUncommonOrBetter; i++)
            {
                var rarity = GetRandomRarity(true); // Exclude commons
                if (availableByRarity.ContainsKey(rarity) && availableByRarity[rarity].Count > 0)
                {
                    var card = availableByRarity[rarity][Random.Range(0, availableByRarity[rarity].Count)];
                    pack.Add(card);
                }
            }
            
            // Fill the rest with random cards
            while (pack.Count < cardsPerPack)
            {
                var rarity = GetRandomRarity(false);
                if (availableByRarity.ContainsKey(rarity) && availableByRarity[rarity].Count > 0)
                {
                    var card = availableByRarity[rarity][Random.Range(0, availableByRarity[rarity].Count)];
                    pack.Add(card);
                }
            }
            
            OnCardPackOpened?.Invoke(pack);
            return pack;
        }
        
        /// <summary>
        /// Groups cards by rarity for pack generation
        /// </summary>
        private Dictionary<CardRarity, List<ShipPartCard>> GroupCardsByRarity()
        {
            return allAvailableCards.GroupBy(card => card.rarity)
                                   .ToDictionary(group => group.Key, group => group.ToList());
        }
        
        /// <summary>
        /// Gets a random rarity based on drop rates
        /// </summary>
        private CardRarity GetRandomRarity(bool excludeCommons = false)
        {
            float roll = Random.value;
            float cumulative = 0f;
            
            if (!excludeCommons)
            {
                cumulative += commonDropRate;
                if (roll <= cumulative) return CardRarity.Common;
            }
            
            cumulative += uncommonDropRate;
            if (roll <= cumulative) return CardRarity.Uncommon;
            
            cumulative += rareDropRate;
            if (roll <= cumulative) return CardRarity.Rare;
            
            cumulative += epicDropRate;
            if (roll <= cumulative) return CardRarity.Epic;
            
            return CardRarity.Legendary;
        }
        
        /// <summary>
        /// Gets cards by rarity
        /// </summary>
        public List<ShipPartCard> GetCardsByRarity(CardRarity rarity)
        {
            return playerOwnedCards.Where(card => card.rarity == rarity).ToList();
        }
        
        /// <summary>
        /// Gets cards by category
        /// </summary>
        public List<ShipPartCard> GetCardsByCategory(string category)
        {
            return playerOwnedCards.Where(card => card.partCategory.Equals(category, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        /// <summary>
        /// Placeholder for loading player collection from save data
        /// </summary>
        private void LoadPlayerCollection()
        {
            // TODO: Implement save/load system
            // For now, give player some starting cards
            if (playerOwnedCards.Count == 0 && allAvailableCards.Count > 0)
            {
                // Give player a few random starting cards
                var startingCards = allAvailableCards.Where(card => card.rarity == CardRarity.Common).Take(10);
                playerOwnedCards.AddRange(startingCards);
                OnCollectionUpdated?.Invoke(playerOwnedCards);
            }
        }
        
        /// <summary>
        /// Placeholder for saving player collection
        /// </summary>
        private void SavePlayerCollection()
        {
            // TODO: Implement save system
            Debug.Log($"Player collection saved: {playerOwnedCards.Count} cards");
        }
    }
}