using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CardSystem.UI
{
    /// <summary>
    /// UI component for individual card display and interaction
    /// Handles card display, hover effects, and play/discard interactions
    /// </summary>
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Card Display")]
        public Image backgroundImage;
        public Text cardNameText;
        public Text manaCostText;
        public Text cardDescriptionText;
        public Image cardArtworkImage;
        public Image rarityBorderImage;
        
        [Header("Hover Effects")]
        public float hoverScale = 1.1f;
        public float hoverAnimationSpeed = 5f;
        public Vector3 hoverOffset = new Vector3(0, 20f, 0);
        
        [Header("Playability Indicators")]
        public Color playableColor = Color.white;
        public Color unplayableColor = Color.gray;
        public float unplayableAlpha = 0.6f;
        
        private ShipPartCard cardData;
        private HandUI handUI;
        private Button cardButton;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool isHovered = false;
        private bool isPlayable = true;
        
        private void Awake()
        {
            // Get or add required components
            cardButton = GetComponent<Button>();
            if (cardButton == null)
                cardButton = gameObject.AddComponent<Button>();
                
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
                
            rectTransform = GetComponent<RectTransform>();
            
            // Store original values
            originalScale = transform.localScale;
            originalPosition = transform.localPosition;
            
            // Setup button click event
            cardButton.onClick.AddListener(OnCardClicked);
        }
        
        /// <summary>
        /// Initializes the card UI with data
        /// </summary>
        public void Initialize(ShipPartCard card, HandUI parentHandUI)
        {
            cardData = card;
            handUI = parentHandUI;
            
            UpdateCardDisplay();
            UpdatePlayabilityState();
        }
        
        /// <summary>
        /// Updates the card display with current data
        /// </summary>
        private void UpdateCardDisplay()
        {
            if (cardData == null) return;
            
            // Update card name
            if (cardNameText != null)
            {
                cardNameText.text = cardData.cardName;
            }
            
            // Update mana cost
            if (manaCostText != null)
            {
                manaCostText.text = cardData.manaCost.ToString();
            }
            
            // Update description
            if (cardDescriptionText != null)
            {
                cardDescriptionText.text = cardData.cardDescription;
            }
            
            // Update artwork
            if (cardArtworkImage != null && cardData.cardArtwork != null)
            {
                cardArtworkImage.sprite = cardData.cardArtwork;
            }
            else if (cardArtworkImage != null && cardData.partIcon != null)
            {
                // Fallback to part icon
                cardArtworkImage.sprite = cardData.partIcon;
            }
            
            // Update background color based on rarity
            if (backgroundImage != null)
            {
                backgroundImage.color = GetRarityColor(cardData.rarity);
            }
            
            // Update rarity border
            if (rarityBorderImage != null)
            {
                rarityBorderImage.color = GetRarityBorderColor(cardData.rarity);
            }
        }
        
        /// <summary>
        /// Updates playability state based on current mana
        /// </summary>
        public void UpdatePlayabilityState()
        {
            if (cardData == null || handUI == null) return;
            
            // Get deck manager to check current mana
            var deckManager = FindObjectOfType<DeckManager>();
            if (deckManager != null)
            {
                isPlayable = deckManager.CurrentMana >= cardData.manaCost;
            }
            
            // Update visual state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isPlayable ? 1f : unplayableAlpha;
            }
            
            // Update color tint
            if (backgroundImage != null)
            {
                Color baseColor = GetRarityColor(cardData.rarity);
                backgroundImage.color = isPlayable ? baseColor : Color.Lerp(baseColor, unplayableColor, 0.5f);
            }
            
            // Update button interactability
            if (cardButton != null)
            {
                cardButton.interactable = isPlayable;
            }
        }
        
        /// <summary>
        /// Gets color based on card rarity
        /// </summary>
        private Color GetRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.8f, 0.8f, 0.8f, 1f),
                CardRarity.Uncommon => new Color(0.4f, 1f, 0.4f, 1f),
                CardRarity.Rare => new Color(0.4f, 0.6f, 1f, 1f),
                CardRarity.Epic => new Color(0.8f, 0.4f, 1f, 1f),
                CardRarity.Legendary => new Color(1f, 0.8f, 0.2f, 1f),
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Gets border color based on card rarity
        /// </summary>
        private Color GetRarityBorderColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => Color.gray,
                CardRarity.Uncommon => Color.green,
                CardRarity.Rare => Color.blue,
                CardRarity.Epic => Color.magenta,
                CardRarity.Legendary => Color.yellow,
                _ => Color.black
            };
        }
        
        /// <summary>
        /// Handles pointer enter for hover effects
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isHovered && isPlayable)
            {
                isHovered = true;
                StartHoverEffect();
            }
        }
        
        /// <summary>
        /// Handles pointer exit for hover effects
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isHovered)
            {
                isHovered = false;
                EndHoverEffect();
            }
        }
        
        /// <summary>
        /// Handles pointer click for card interaction
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isPlayable) return;
            
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Left click to play card
                PlayCard();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Right click to discard card
                DiscardCard();
            }
        }
        
        /// <summary>
        /// Starts hover effect animation
        /// </summary>
        private void StartHoverEffect()
        {
            if (rectTransform != null)
            {
                // Simple immediate scale up and move up
                transform.localScale = originalScale * hoverScale;
                transform.localPosition = originalPosition + hoverOffset;
            }
        }
        
        /// <summary>
        /// Ends hover effect animation
        /// </summary>
        private void EndHoverEffect()
        {
            if (rectTransform != null)
            {
                // Return to original scale and position
                transform.localScale = originalScale;
                transform.localPosition = originalPosition;
            }
        }
        
        /// <summary>
        /// Button click handler (for left click)
        /// </summary>
        private void OnCardClicked()
        {
            if (isPlayable)
            {
                PlayCard();
            }
        }
        
        /// <summary>
        /// Plays the card
        /// </summary>
        private void PlayCard()
        {
            if (handUI != null && cardData != null)
            {
                handUI.PlayCard(cardData);
                
                // Play card animation
                PlayCardAnimation();
            }
        }
        
        /// <summary>
        /// Discards the card
        /// </summary>
        private void DiscardCard()
        {
            if (handUI != null && cardData != null)
            {
                handUI.DiscardCard(cardData);
                
                // Play discard animation
                PlayDiscardAnimation();
            }
        }
        
        /// <summary>
        /// Plays card play animation
        /// </summary>
        private void PlayCardAnimation()
        {
            // Simple immediate scale down and fade out
            transform.localScale = Vector3.zero;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        /// <summary>
        /// Plays card discard animation
        /// </summary>
        private void PlayDiscardAnimation()
        {
            // Move to the side and fade out immediately
            Vector3 discardPosition = originalPosition + Vector3.right * 200f;
            transform.localPosition = discardPosition;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        /// <summary>
        /// Shows tooltip with card information
        /// </summary>
        public void ShowTooltip()
        {
            if (cardData != null)
            {
                string tooltipText = $"{cardData.cardName}\n" +
                                   $"Cost: {cardData.manaCost}\n" +
                                   $"Rarity: {cardData.rarity}\n" +
                                   $"{cardData.cardDescription}";
                
                // TODO: Implement tooltip system
                Debug.Log($"Card Tooltip: {tooltipText}");
            }
        }
        
        /// <summary>
        /// Hides the tooltip
        /// </summary>
        public void HideTooltip()
        {
            // TODO: Implement tooltip hiding
        }
        
        private void Update()
        {
            // Update playability state each frame (could be optimized to event-driven)
            UpdatePlayabilityState();
        }
        
        private void OnDestroy()
        {
            // No cleanup needed for simple animations
        }
    }
}