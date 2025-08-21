using System;
using UnityEngine;

namespace Commonwealth.Script.Ship.Monitors
{
    [Serializable]
    public class ShipStat
    {
        [Header("Basic Info")]
        public string statName;
        public string unit;
        public string category;
        
        [Header("Current Values")]
        public float currentValue;
        public float previousValue;
        
        [Header("Thresholds")]
        public float minValue;
        public float maxValue;
        public float warningThreshold;
        public float criticalThreshold;
        
        [Header("Display Settings")]
        public bool higherIsBetter = true;
        public int displayPriority = 0;
        
        public ShipStatState CurrentState { get; private set; }
        public float ChangeRate { get; private set; }
        
        public ShipStat(string name, string unit, string category, float min, float max, float warning, float critical, bool higherIsBetter = true)
        {
            this.statName = name;
            this.unit = unit;
            this.category = category;
            this.minValue = min;
            this.maxValue = max;
            this.warningThreshold = warning;
            this.criticalThreshold = critical;
            this.higherIsBetter = higherIsBetter;
            this.currentValue = (min + max) / 2f; // Start at middle
        }
        
        public void UpdateValue(float newValue)
        {
            previousValue = currentValue;
            currentValue = Mathf.Clamp(newValue, minValue, maxValue);
            CalculateChangeRate();
            UpdateState();
        }
        
        private void CalculateChangeRate()
        {
            ChangeRate = currentValue - previousValue;
        }
        
        private void UpdateState()
        {
            float normalizedValue = (currentValue - minValue) / (maxValue - minValue);
            
            if (higherIsBetter)
            {
                if (currentValue <= criticalThreshold)
                    CurrentState = ShipStatState.Critical;
                else if (currentValue <= warningThreshold)
                    CurrentState = ShipStatState.Warning;
                else
                    CurrentState = ShipStatState.Good;
            }
            else
            {
                if (currentValue >= criticalThreshold)
                    CurrentState = ShipStatState.Critical;
                else if (currentValue >= warningThreshold)
                    CurrentState = ShipStatState.Warning;
                else
                    CurrentState = ShipStatState.Good;
            }
        }
        
        public Color GetStateColor()
        {
            return CurrentState switch
            {
                ShipStatState.Good => Color.green,
                ShipStatState.Warning => Color.yellow,
                ShipStatState.Critical => Color.red,
                _ => Color.white
            };
        }
        
        public int GetUrgencyScore()
        {
            int baseScore = CurrentState switch
            {
                ShipStatState.Critical => 1000,
                ShipStatState.Warning => 500,
                ShipStatState.Good => 0,
                _ => 0
            };
            
            // Add priority modifier
            return baseScore + displayPriority;
        }
    }
    
    public enum ShipStatState
    {
        Good,
        Warning,
        Critical
    }
}