using System;
using System.Collections;
using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    public class ShipStatAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float valueChangeDuration = 0.5f;
        [SerializeField] private float riskPulseDuration = 1.0f;
        [SerializeField] private float colorTransitionDuration = 0.3f;
        [SerializeField] private AnimationCurve valueChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve riskPulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.7f);
        
        [Header("Visual Effects")]
        [SerializeField] private float criticalBlinkRate = 2.0f;
        [SerializeField] private float warningPulseIntensity = 0.8f;
        [SerializeField] private Color increaseColor = Color.cyan;
        [SerializeField] private Color decreaseColor = Color.magenta;
        
        private Coroutine currentAnimation;
        private Coroutine riskAnimation;
        private Coroutine colorAnimation;
        
        public event Action<float> OnValueAnimationUpdate;
        public event Action<Color> OnColorAnimationUpdate;
        public event Action<float> OnRiskAnimationUpdate;
        
        public void AnimateValueChange(float fromValue, float toValue, Action<float> onUpdate = null, Action onComplete = null)
        {
            if (!gameObject.activeSelf)
                return;
            
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateValueCoroutine(fromValue, toValue, onUpdate, onComplete));
        }
        
        public void AnimateColorTransition(Color fromColor, Color toColor, Action<Color> onUpdate = null, Action onComplete = null)
        {
            if (!gameObject.activeSelf)
                return;
            
            if (colorAnimation != null)
                StopCoroutine(colorAnimation);
                
            colorAnimation = StartCoroutine(AnimateColorCoroutine(fromColor, toColor, onUpdate, onComplete));
        }
        
        public void StartRiskAnimation(ShipStatState state, Action<float> onUpdate = null)
        {
            if (!gameObject.activeSelf)
                return;
            
            if (riskAnimation != null)
                StopCoroutine(riskAnimation);
                
            switch (state)
            {
                case ShipStatState.Critical:
                    riskAnimation = StartCoroutine(CriticalBlinkCoroutine(onUpdate));
                    break;
                case ShipStatState.Warning:
                    riskAnimation = StartCoroutine(WarningPulseCoroutine(onUpdate));
                    break;
                case ShipStatState.Good:
                    StopRiskAnimation();
                    break;
            }
        }
        
        public void StopRiskAnimation()
        {
            if (riskAnimation != null)
            {
                StopCoroutine(riskAnimation);
                riskAnimation = null;
            }
        }
        
        private IEnumerator AnimateValueCoroutine(float fromValue, float toValue, Action<float> onUpdate, Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < valueChangeDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / valueChangeDuration;
                float curvedProgress = valueChangeCurve.Evaluate(progress);
                float currentValue = Mathf.Lerp(fromValue, toValue, curvedProgress);
                
                onUpdate?.Invoke(currentValue);
                OnValueAnimationUpdate?.Invoke(currentValue);
                
                yield return null;
            }
            
            onUpdate?.Invoke(toValue);
            OnValueAnimationUpdate?.Invoke(toValue);
            onComplete?.Invoke();
            
            currentAnimation = null;
        }
        
        private IEnumerator AnimateColorCoroutine(Color fromColor, Color toColor, Action<Color> onUpdate, Action onComplete)
        {
            float elapsed = 0f;
            
            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / colorTransitionDuration;
                Color currentColor = Color.Lerp(fromColor, toColor, progress);
                
                onUpdate?.Invoke(currentColor);
                OnColorAnimationUpdate?.Invoke(currentColor);
                
                yield return null;
            }
            
            onUpdate?.Invoke(toColor);
            OnColorAnimationUpdate?.Invoke(toColor);
            onComplete?.Invoke();
            
            colorAnimation = null;
        }
        
        private IEnumerator CriticalBlinkCoroutine(Action<float> onUpdate)
        {
            while (true)
            {
                float blinkTime = 1f / criticalBlinkRate;
                float halfBlinkTime = blinkTime / 2f;
                
                // Fade to full intensity
                yield return StartCoroutine(BlinkFadeCoroutine(0f, 1f, halfBlinkTime, onUpdate));
                
                // Fade to low intensity
                yield return StartCoroutine(BlinkFadeCoroutine(1f, 0.3f, halfBlinkTime, onUpdate));
            }
        }
        
        private IEnumerator WarningPulseCoroutine(Action<float> onUpdate)
        {
            while (true)
            {
                float elapsed = 0f;
                
                while (elapsed < riskPulseDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / riskPulseDuration;
                    float intensity = riskPulseCurve.Evaluate(progress) * warningPulseIntensity;
                    
                    onUpdate?.Invoke(intensity);
                    OnRiskAnimationUpdate?.Invoke(intensity);
                    
                    yield return null;
                }
            }
        }
        
        private IEnumerator BlinkFadeCoroutine(float fromIntensity, float toIntensity, float duration, Action<float> onUpdate)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float intensity = Mathf.Lerp(fromIntensity, toIntensity, progress);
                
                onUpdate?.Invoke(intensity);
                OnRiskAnimationUpdate?.Invoke(intensity);
                
                yield return null;
            }
        }
        
        public void ShowValueChangeIndicator(float oldValue, float newValue, Action<Color> onColorUpdate)
        {
            if (!gameObject.activeSelf)
                return;
            
            Color indicatorColor = newValue > oldValue ? increaseColor : decreaseColor;
            StartCoroutine(ValueChangeIndicatorCoroutine(indicatorColor, onColorUpdate));
        }
        
        private IEnumerator ValueChangeIndicatorCoroutine(Color indicatorColor, Action<Color> onColorUpdate)
        {
            // Flash the indicator color briefly
            onColorUpdate?.Invoke(indicatorColor);
            yield return new WaitForSeconds(0.2f);
            
            // Fade back to transparent
            float elapsed = 0f;
            float fadeDuration = 0.5f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                Color fadeColor = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, alpha);
                onColorUpdate?.Invoke(fadeColor);
                yield return null;
            }
            
            onColorUpdate?.Invoke(Color.clear);
        }
        
        public void AnimateStatEntry(Transform target, Action onComplete = null)
        {
            StartCoroutine(StatEntryCoroutine(target, onComplete));
        }
        
        private IEnumerator StatEntryCoroutine(Transform target, Action onComplete)
        {
            Vector3 originalScale = target.localScale;
            target.localScale = Vector3.zero;
            
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float curvedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f); // Ease out
                target.localScale = Vector3.Lerp(Vector3.zero, originalScale, curvedProgress);
                yield return null;
            }
            
            target.localScale = originalScale;
            onComplete?.Invoke();
        }
    }
}