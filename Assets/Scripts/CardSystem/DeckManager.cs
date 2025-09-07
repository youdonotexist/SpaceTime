using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardSystem
{
    /// <summary>
    /// Manages the player's deck, hand, and card collection
    /// Handles deck building, shuffling, drawing, and card play mechanics
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        [Header("Deck Configuration")]
        public int maxDeckSize = 30;
        public int maxHandSize = 7;
        public int startingHandSize = 5;
        public int startingMana = 3;
        public int maxMana = 10;
        
        [Header("Current Deck")]
        [SerializeField] private List<ShipPartCard> currentDeck = new List<ShipPartCard>();
        
        [Header("Runtime State")]
        [SerializeField] private List<ShipPartCard> hand = new List<ShipPartCard>();
        [SerializeField] private List<ShipPartCard> discardPile = new List<ShipPartCard>();
        [SerializeField] private List<ShipPartCard> deckPile = new List<ShipPartCard>();
        [SerializeField] private int currentMana = 0;
        
        // Events
        public System.Action<ShipPartCard> OnCardDrawn;
        public System.Action<ShipPartCard> OnCardPlayed;
        public System.Action<ShipPartCard> OnCardDiscarded;
        public System.Action<List<ShipPartCard>> OnHandChanged;
        public System.Action<int> OnManaChanged;
        public System.Action OnDeckEmpty;
        
        // Properties
        public List<ShipPartCard> Hand => hand;
        public List<ShipPartCard> CurrentDeck => currentDeck;
        public int CurrentMana => currentMana;
        public int HandCount => hand.Count;
        public int DeckCount => deckPile.Count;
        public int DiscardCount => discardPile.Count;
        
        private void Start()
        {
            InitializeGame();
        }
        
        /// <summary>
        /// Initializes a new game with the current deck
        /// </summary>
        public void InitializeGame()
        {
            // Copy deck to pile for shuffling
            deckPile.Clear();
            deckPile.AddRange(currentDeck);
            
            // Clear hand and discard
            hand.Clear();
            discardPile.Clear();
            
            // Shuffle deck
            ShuffleDeck();
            
            // Set starting mana
            currentMana = startingMana;
            OnManaChanged?.Invoke(currentMana);
            
            // Draw starting hand
            for (int i = 0; i < startingHandSize && deckPile.Count > 0; i++)
            {
                DrawCard();
            }
        }
        
        /// <summary>
        /// Shuffles the deck pile using Fisher-Yates algorithm
        /// </summary>
        public void ShuffleDeck()
        {
            for (int i = deckPile.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                var temp = deckPile[i];
                deckPile[i] = deckPile[randomIndex];
                deckPile[randomIndex] = temp;
            }
        }
        
        /// <summary>
        /// Draws a card from the deck to the hand
        /// </summary>
        public bool DrawCard()
        {
            if (hand.Count >= maxHandSize)
            {
                Debug.LogWarning("Hand is full, cannot draw card");
                return false;
            }
            
            if (deckPile.Count == 0)
            {
                // Try to reshuffle discard pile into deck
                if (discardPile.Count > 0)
                {
                    deckPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDeck();
                    Debug.Log("Reshuffled discard pile into deck");
                }
                else
                {
                    Debug.LogWarning("Deck and discard pile are empty");
                    OnDeckEmpty?.Invoke();
                    return false;
                }
            }
            
            var drawnCard = deckPile[0];
            deckPile.RemoveAt(0);
            hand.Add(drawnCard);
            
            OnCardDrawn?.Invoke(drawnCard);
            OnHandChanged?.Invoke(hand);
            
            return true;
        }
        
        /// <summary>
        /// Plays a card from the hand
        /// </summary>
        public bool PlayCard(ShipPartCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand");
                return false;
            }
            
            if (currentMana < card.manaCost)
            {
                Debug.LogWarning("Not enough mana to play card");
                return false;
            }
            
            // Remove from hand
            hand.Remove(card);
            
            // Add to discard pile
            discardPile.Add(card);
            
            // Spend mana
            currentMana -= card.manaCost;
            
            OnCardPlayed?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            OnManaChanged?.Invoke(currentMana);
            
            return true;
        }
        
        /// <summary>
        /// Discards a card from the hand without playing it
        /// </summary>
        public bool DiscardCard(ShipPartCard card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogWarning("Card not in hand");
                return false;
            }
            
            hand.Remove(card);
            discardPile.Add(card);
            
            OnCardDiscarded?.Invoke(card);
            OnHandChanged?.Invoke(hand);
            
            return true;
        }
        
        /// <summary>
        /// Adds mana (typically done each turn or through game mechanics)
        /// </summary>
        public void AddMana(int amount)
        {
            currentMana = Mathf.Min(currentMana + amount, maxMana);
            OnManaChanged?.Invoke(currentMana);
        }
        
        /// <summary>
        /// Sets the current deck for deck building
        /// </summary>
        public void SetDeck(List<ShipPartCard> newDeck)
        {
            if (newDeck.Count > maxDeckSize)
            {
                Debug.LogWarning($"Deck size ({newDeck.Count}) exceeds maximum ({maxDeckSize})");
                return;
            }
            
            // Validate deck composition
            if (!ValidateDeck(newDeck))
            {
                Debug.LogError("Invalid deck composition");
                return;
            }
            
            currentDeck.Clear();
            currentDeck.AddRange(newDeck);
        }
        
        /// <summary>
        /// Validates deck composition rules
        /// </summary>
        private bool ValidateDeck(List<ShipPartCard> deck)
        {
            var cardCounts = new Dictionary<ShipPartCard, int>();
            
            foreach (var card in deck)
            {
                if (cardCounts.ContainsKey(card))
                    cardCounts[card]++;
                else
                    cardCounts[card] = 1;
                
                // Check max copies rule
                if (cardCounts[card] > card.maxCopiesInDeck)
                {
                    Debug.LogError($"Too many copies of {card.cardName} in deck (max: {card.maxCopiesInDeck})");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Gets all playable cards in hand (enough mana)
        /// </summary>
        public List<ShipPartCard> GetPlayableCards()
        {
            return hand.Where(card => card.manaCost <= currentMana).ToList();
        }
        
        /// <summary>
        /// Clears all piles and resets state
        /// </summary>
        public void Reset()
        {
            hand.Clear();
            deckPile.Clear();
            discardPile.Clear();
            currentMana = startingMana;
            
            OnHandChanged?.Invoke(hand);
            OnManaChanged?.Invoke(currentMana);
        }
    }
}