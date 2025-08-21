using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Commonwealth.Script.Ship.Monitors
{
    public class ShipStatUIItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statNameText;
        [SerializeField] private TextMeshProUGUI statValueText;
        [SerializeField] private TextMeshProUGUI statUnitText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image statusIndicator;
        [SerializeField] private Image changeIndicator;
        [SerializeField] private Slider valueBar;
        
        [Header("Visual Settings")]
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private Color backgroundNormal = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color backgroundHighlight = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        
        private ShipStat associatedStat;
        private ShipStatAnimator animator;
        private Color lastStateColor;
        private float lastDisplayedValue;
        
        void Awake()
        {
            animator = GetComponent<ShipStatAnimator>();
            if (animator == null)
                animator = gameObject.AddComponent<ShipStatAnimator>();
            
            InitializeUI();
            SubscribeToAnimatorEvents();
        }
        
        void OnDestroy()
        {
            UnsubscribeFromAnimatorEvents();
        }
        
        private void InitializeUI()
        {
            // Set default background
            if (backgroundImage != null)
                backgroundImage.color = backgroundNormal;
            
            // Initialize slider if present
            if (valueBar != null)
            {
                valueBar.minValue = 0f;
                valueBar.maxValue = 1f;
                valueBar.value = 0.5f;
            }
        }
        
        public void Initialize(ShipStat stat)
        {
            associatedStat = stat;
            lastStateColor = stat.GetStateColor();
            lastDisplayedValue = stat.currentValue;
            
            UpdateDisplay();
        }
        
        public void UpdateDisplay()
        {
            if (associatedStat == null) return;
            
            UpdateStatText();
            UpdateValueDisplay();
            UpdateStatusIndicator();
            UpdateValueBar();
            
            // Check for state changes and animate if necessary
            Color newStateColor = associatedStat.GetStateColor();
            if (newStateColor != lastStateColor)
            {
                AnimateColorTransition(lastStateColor, newStateColor);
                lastStateColor = newStateColor;
            }
            
            // Check for value changes and show indicators
            if (Mathf.Abs(associatedStat.currentValue - lastDisplayedValue) > 0.01f)
            {
                ShowValueChangeIndicator();
                AnimateValueChange(lastDisplayedValue, associatedStat.currentValue);
                lastDisplayedValue = associatedStat.currentValue;
            }
            
            // Start risk animations based on state
            if (animator != null)
            {
                animator.StartRiskAnimation(associatedStat.CurrentState, OnRiskAnimationUpdate);
            }
        }
        
        private void UpdateStatText()
        {
            if (statNameText != null)
                statNameText.text = associatedStat.statName;
                
            if (statUnitText != null)
                statUnitText.text = string.IsNullOrEmpty(associatedStat.unit) ? "" : $"({associatedStat.unit})";
        }
        
        private void UpdateValueDisplay()
        {
            if (statValueText != null)
            {
                // Format value based on type
                string formattedValue = FormatStatValue(associatedStat.currentValue);
                
                // Add change indicator
                if (Mathf.Abs(associatedStat.ChangeRate) > 0.01f)
                {
                    string arrow = associatedStat.ChangeRate > 0 ? "↑" : "↓";
                    formattedValue += $" {arrow}";
                }
                
                statValueText.text = formattedValue;
                statValueText.color = associatedStat.GetStateColor();
            }
        }
        
        private void UpdateStatusIndicator()
        {
            if (statusIndicator != null)
            {
                statusIndicator.color = associatedStat.GetStateColor();
                
                // Set indicator shape/symbol based on state
                switch (associatedStat.CurrentState)
                {
                    case ShipStatState.Critical:
                        statusIndicator.gameObject.SetActive(true);
                        break;
                    case ShipStatState.Warning:
                        statusIndicator.gameObject.SetActive(true);
                        break;
                    case ShipStatState.Good:
                        statusIndicator.gameObject.SetActive(false);
                        break;
                }
            }
        }
        
        private void UpdateValueBar()
        {
            if (valueBar != null)
            {
                float normalizedValue = (associatedStat.currentValue - associatedStat.minValue) / 
                                      (associatedStat.maxValue - associatedStat.minValue);
                valueBar.value = Mathf.Clamp01(normalizedValue);
                
                // Color the bar based on state
                var fillImage = valueBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = associatedStat.GetStateColor();
                }
            }
        }
        
        private void AnimateColorTransition(Color fromColor, Color toColor)
        {
            if (animator != null && statusIndicator != null)
            {
                animator.AnimateColorTransition(fromColor, toColor, 
                    color => statusIndicator.color = color);
            }
        }
        
        private void AnimateValueChange(float fromValue, float toValue)
        {
            if (animator != null)
            {
                animator.AnimateValueChange(fromValue, toValue, 
                    value => {
                        if (statValueText != null)
                            statValueText.text = FormatStatValue(value);
                    });
            }
        }
        
        private void ShowValueChangeIndicator()
        {
            if (animator != null && changeIndicator != null)
            {
                animator.ShowValueChangeIndicator(lastDisplayedValue, associatedStat.currentValue,
                    color => changeIndicator.color = color);
            }
        }
        
        private void OnRiskAnimationUpdate(float intensity)
        {
            if (backgroundImage != null)
            {
                Color highlightColor = associatedStat.GetStateColor();
                highlightColor.a = intensity * 0.3f; // Subtle background pulsing
                backgroundImage.color = Color.Lerp(backgroundNormal, highlightColor, intensity);
            }
        }
        
        private string FormatStatValue(float value)
        {
            // Format based on the range and type of value
            if (value >= 1000)
                return $"{value:F0}";
            else if (value >= 100)
                return $"{value:F0}";
            else if (value >= 10)
                return $"{value:F1}";
            else if (value >= 1)
                return $"{value:F1}";
            else
                return $"{value:F2}";
        }
        
        private void SubscribeToAnimatorEvents()
        {
            if (animator != null)
            {
                animator.OnValueAnimationUpdate += OnValueAnimationUpdate;
                animator.OnColorAnimationUpdate += OnColorAnimationUpdate;
                animator.OnRiskAnimationUpdate += OnRiskAnimationUpdate;
            }
        }
        
        private void UnsubscribeFromAnimatorEvents()
        {
            if (animator != null)
            {
                animator.OnValueAnimationUpdate -= OnValueAnimationUpdate;
                animator.OnColorAnimationUpdate -= OnColorAnimationUpdate;
                animator.OnRiskAnimationUpdate -= OnRiskAnimationUpdate;
            }
        }
        
        private void OnValueAnimationUpdate(float value)
        {
            // This can be used for additional value animation effects
        }
        
        private void OnColorAnimationUpdate(Color color)
        {
            // This can be used for additional color animation effects
        }
        
        public void SetHighlighted(bool highlighted)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlighted ? backgroundHighlight : backgroundNormal;
            }
        }
        
        public ShipStat GetAssociatedStat()
        {
            return associatedStat;
        }
    }
}