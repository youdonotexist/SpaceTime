using UnityEngine;
using RuntimeGraph.Sprite;

namespace CardSystem
{
    /// <summary>
    /// Represents a card that can spawn ship components
    /// Based on existing NodeTypeData but extended for deck building mechanics
    /// </summary>
    [CreateAssetMenu(fileName = "ShipPartCard", menuName = "Card System/Ship Part Card")]
    public class ShipPartCard : ScriptableObject
    {
        [Header("Card Identity")]
        public string cardName;
        public string cardDescription;
        public Sprite cardArtwork;
        
        [Header("Rarity and Cost")]
        public CardRarity rarity = CardRarity.Common;
        public int manaCost = 1; // Cost to play the card
        public int collectionPriority = 1; // Higher priority = appears more often in packs
        
        [Header("Ship Part Data")]
        public string partName;
        public string partCategory;
        public Color partColor = Color.white;
        public string partDescription;
        public Sprite partIcon;
        
        [Header("MIDI Integration (for existing system compatibility)")]
        public int note = 60;
        public int velocity = 80;
        public int channel = 1;
        public float duration = 0.08f;
        
        [Header("Card Effects")]
        public bool isInstant = false; // If true, spawns part immediately when played
        public float cooldownTime = 0f; // Time before card can be played again
        public int maxCopiesInDeck = 3; // Maximum copies allowed in deck
        
        /// <summary>
        /// Converts this card to NodeTypeData for compatibility with existing ship building system
        /// </summary>
        public SpriteNodePalette.NodeTypeData ToNodeTypeData()
        {
            return new SpriteNodePalette.NodeTypeData
            {
                name = partName,
                category = partCategory,
                color = partColor,
                description = partDescription,
                icon = partIcon,
                note = note,
                velocity = velocity,
                channel = channel,
                duration = duration
            };
        }
        
        /// <summary>
        /// Creates a card from existing NodeTypeData (for converting existing parts to cards)
        /// </summary>
        public static ShipPartCard FromNodeTypeData(SpriteNodePalette.NodeTypeData nodeData)
        {
            var card = CreateInstance<ShipPartCard>();
            card.cardName = nodeData.name + " Card";
            card.cardDescription = nodeData.description;
            card.partName = nodeData.name;
            card.partCategory = nodeData.category;
            card.partColor = nodeData.color;
            card.partDescription = nodeData.description;
            card.partIcon = nodeData.icon;
            card.note = nodeData.note;
            card.velocity = nodeData.velocity;
            card.channel = nodeData.channel;
            card.duration = nodeData.duration;
            return card;
        }
    }
    
    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}