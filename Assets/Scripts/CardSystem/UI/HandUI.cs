using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardSystem.UI
{
    /// <summary>
    /// UI system for displaying and managing the player's hand of cards
    /// Provides card display, play/discard functionality, and mana management
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform cardContainer;
        public GameObject cardUIPrefab;
        public Text manaText;
        public Text handCountText;
        public Button drawCardButton;
        
        [Header("Layout Configuration")]
        public float cardSpacing = 10f;
        public float cardScale = 1f;
        public int maxVisibleCards = 10;
        
        [Header("Animation Settings")]
        public float cardAnimationDuration = 0.3f;
        public AnimationCurve cardAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        private DeckManager deckManager;
        private CardSpawner cardSpawner;
        private List<CardUI> cardUIInstances = new List<CardUI>();
        private Canvas handCanvas;
        
        private void Start()
        {
            InitializeUI();
            FindDependencies();
            SubscribeToEvents();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        /// <summary>
        /// Initializes the UI components
        /// </summary>
        private void InitializeUI()
        {
            // Create canvas if not exists
            if (handCanvas == null)
            {
                handCanvas = GetComponent<Canvas>();
                if (handCanvas == null)
                {
                    handCanvas = gameObject.AddComponent<Canvas>();
                    handCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    handCanvas.sortingOrder = 100;
                }
            }
            
            // Setup card container if not assigned
            if (cardContainer == null)
            {
                GameObject containerGO = new GameObject("CardContainer");
                containerGO.transform.SetParent(transform);
                cardContainer = containerGO.transform;
                
                // Position at bottom of screen
                RectTransform containerRect = containerGO.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0f, 0f);
                containerRect.anchorMax = new Vector2(1f, 0.2f);
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;
                
                // Add horizontal layout group
                HorizontalLayoutGroup layoutGroup = containerGO.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.spacing = cardSpacing;
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.childControlWidth = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = false;
                layoutGroup.childForceExpandHeight = false;
            }
            
            // Setup mana text if not assigned
            if (manaText == null)
            {
                GameObject manaGO = new GameObject("ManaText");
                manaGO.transform.SetParent(transform);
                manaText = manaGO.AddComponent<Text>();
                manaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                manaText.fontSize = 24;
                manaText.color = Color.cyan;
                manaText.text = "Mana: 0/0";
                
                RectTransform manaRect = manaGO.GetComponent<RectTransform>();
                manaRect.anchorMin = new Vector2(0f, 0.8f);
                manaRect.anchorMax = new Vector2(0.3f, 1f);
                manaRect.offsetMin = Vector2.zero;
                manaRect.offsetMax = Vector2.zero;
            }
            
            // Setup hand count text
            if (handCountText == null)
            {
                GameObject handCountGO = new GameObject("HandCountText");
                handCountGO.transform.SetParent(transform);
                handCountText = handCountGO.AddComponent<Text>();
                handCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                handCountText.fontSize = 18;
                handCountText.color = Color.white;
                handCountText.text = "Hand: 0/7";
                
                RectTransform handCountRect = handCountGO.GetComponent<RectTransform>();
                handCountRect.anchorMin = new Vector2(0.3f, 0.8f);
                handCountRect.anchorMax = new Vector2(0.6f, 1f);
                handCountRect.offsetMin = Vector2.zero;
                handCountRect.offsetMax = Vector2.zero;
            }
            
            // Setup draw card button
            if (drawCardButton == null)
            {
                GameObject buttonGO = new GameObject("DrawCardButton");
                buttonGO.transform.SetParent(transform);
                drawCardButton = buttonGO.AddComponent<Button>();
                
                // Add button background
                Image buttonImage = buttonGO.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.4f, 0.8f, 0.8f);
                
                // Add button text
                GameObject buttonTextGO = new GameObject("Text");
                buttonTextGO.transform.SetParent(buttonGO.transform);
                Text buttonText = buttonTextGO.AddComponent<Text>();
                buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                buttonText.fontSize = 16;
                buttonText.color = Color.white;
                buttonText.text = "Draw Card";
                buttonText.alignment = TextAnchor.MiddleCenter;
                
                RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
                buttonTextRect.anchorMin = Vector2.zero;
                buttonTextRect.anchorMax = Vector2.one;
                buttonTextRect.offsetMin = Vector2.zero;
                buttonTextRect.offsetMax = Vector2.zero;
                
                RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.7f, 0.8f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                
                drawCardButton.onClick.AddListener(OnDrawCardButtonClicked);
            }
        }
        
        /// <summary>
        /// Finds required dependencies
        /// </summary>
        private void FindDependencies()
        {
            deckManager = FindObjectOfType<DeckManager>();
            cardSpawner = FindObjectOfType<CardSpawner>();
            
            if (deckManager == null)
            {
                Debug.LogError("DeckManager not found! HandUI requires a DeckManager to function.");
            }
            
            if (cardSpawner == null)
            {
                Debug.LogWarning("CardSpawner not found! Card playing functionality will be limited.");
            }
        }
        
        /// <summary>
        /// Subscribes to deck manager events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (deckManager != null)
            {
                deckManager.OnHandChanged += UpdateHandDisplay;
                deckManager.OnManaChanged += UpdateManaDisplay;
                deckManager.OnCardDrawn += OnCardDrawn;
                deckManager.OnCardPlayed += OnCardPlayed;
                deckManager.OnCardDiscarded += OnCardDiscarded;
                
                // Initial update
                UpdateHandDisplay(deckManager.Hand);
                UpdateManaDisplay(deckManager.CurrentMana);
            }
        }
        
        /// <summary>
        /// Unsubscribes from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (deckManager != null)
            {
                deckManager.OnHandChanged -= UpdateHandDisplay;
                deckManager.OnManaChanged -= UpdateManaDisplay;
                deckManager.OnCardDrawn -= OnCardDrawn;
                deckManager.OnCardPlayed -= OnCardPlayed;
                deckManager.OnCardDiscarded -= OnCardDiscarded;
            }
        }
        
        /// <summary>
        /// Updates the hand display with current cards
        /// </summary>
        private void UpdateHandDisplay(List<ShipPartCard> hand)
        {
            // Clear existing card UIs
            foreach (var cardUI in cardUIInstances)
            {
                if (cardUI != null && cardUI.gameObject != null)
                    DestroyImmediate(cardUI.gameObject);
            }
            cardUIInstances.Clear();
            
            // Create new card UIs
            for (int i = 0; i < hand.Count && i < maxVisibleCards; i++)
            {
                CreateCardUI(hand[i], i);
            }
            
            // Update hand count text
            if (handCountText != null)
            {
                handCountText.text = $"Hand: {hand.Count}/{deckManager.maxHandSize}";
            }
        }
        
        /// <summary>
        /// Creates a card UI instance
        /// </summary>
        private void CreateCardUI(ShipPartCard card, int index)
        {
            GameObject cardGO;
            CardUI cardUI;
            
            // Use prefab if available, otherwise create basic card UI
            if (cardUIPrefab != null)
            {
                cardGO = Instantiate(cardUIPrefab, cardContainer);
                cardUI = cardGO.GetComponent<CardUI>();
                if (cardUI == null)
                    cardUI = cardGO.AddComponent<CardUI>();
            }
            else
            {
                cardGO = CreateBasicCardUI(card);
                cardUI = cardGO.GetComponent<CardUI>();
            }
            
            cardUI.Initialize(card, this);
            cardUIInstances.Add(cardUI);
            
            // Set scale
            cardGO.transform.localScale = Vector3.one * cardScale;
        }
        
        /// <summary>
        /// Creates a basic card UI when no prefab is available
        /// </summary>
        private GameObject CreateBasicCardUI(ShipPartCard card)
        {
            GameObject cardGO = new GameObject($"Card_{card.cardName}");
            cardGO.transform.SetParent(cardContainer);
            
            // Add card UI component
            CardUI cardUI = cardGO.AddComponent<CardUI>();
            
            // Add background image
            Image backgroundImage = cardGO.AddComponent<Image>();
            backgroundImage.color = GetCardRarityColor(card.rarity);
            
            // Add button for interaction
            Button cardButton = cardGO.AddComponent<Button>();
            cardButton.targetGraphic = backgroundImage;
            
            // Set size
            RectTransform cardRect = cardGO.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(100, 140);
            
            // Add card name text
            GameObject nameGO = new GameObject("CardName");
            nameGO.transform.SetParent(cardGO.transform);
            Text nameText = nameGO.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 12;
            nameText.color = Color.white;
            nameText.text = card.cardName;
            nameText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.7f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            
            // Add mana cost text
            GameObject costGO = new GameObject("ManaCost");
            costGO.transform.SetParent(cardGO.transform);
            Text costText = costGO.AddComponent<Text>();
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 16;
            costText.color = Color.cyan;
            costText.text = card.manaCost.ToString();
            costText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform costRect = costGO.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0f, 0f);
            costRect.anchorMax = new Vector2(0.3f, 0.3f);
            costRect.offsetMin = Vector2.zero;
            costRect.offsetMax = Vector2.zero;
            
            return cardGO;
        }
        
        /// <summary>
        /// Gets color based on card rarity
        /// </summary>
        private Color GetCardRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                CardRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f, 0.8f),
                CardRarity.Rare => new Color(0.2f, 0.4f, 0.8f, 0.8f),
                CardRarity.Epic => new Color(0.6f, 0.2f, 0.8f, 0.8f),
                CardRarity.Legendary => new Color(1f, 0.6f, 0f, 0.8f),
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// Updates mana display
        /// </summary>
        private void UpdateManaDisplay(int currentMana)
        {
            if (manaText != null && deckManager != null)
            {
                manaText.text = $"Mana: {currentMana}/{deckManager.maxMana}";
            }
        }
        
        /// <summary>
        /// Handles card drawn event
        /// </summary>
        private void OnCardDrawn(ShipPartCard card)
        {
            Debug.Log($"Card drawn: {card.cardName}");
            // Animation could be added here
        }
        
        /// <summary>
        /// Handles card played event
        /// </summary>
        private void OnCardPlayed(ShipPartCard card)
        {
            Debug.Log($"Card played: {card.cardName}");
            // Animation could be added here
        }
        
        /// <summary>
        /// Handles card discarded event
        /// </summary>
        private void OnCardDiscarded(ShipPartCard card)
        {
            Debug.Log($"Card discarded: {card.cardName}");
            // Animation could be added here
        }
        
        /// <summary>
        /// Handles draw card button click
        /// </summary>
        private void OnDrawCardButtonClicked()
        {
            if (deckManager != null)
            {
                deckManager.DrawCard();
            }
        }
        
        /// <summary>
        /// Called by CardUI when a card is clicked to play
        /// </summary>
        public void PlayCard(ShipPartCard card)
        {
            if (deckManager != null && deckManager.PlayCard(card))
            {
                Debug.Log($"Successfully played card: {card.cardName}");
            }
        }
        
        /// <summary>
        /// Called by CardUI when a card is right-clicked to discard
        /// </summary>
        public void DiscardCard(ShipPartCard card)
        {
            if (deckManager != null && deckManager.DiscardCard(card))
            {
                Debug.Log($"Successfully discarded card: {card.cardName}");
            }
        }
    }
}