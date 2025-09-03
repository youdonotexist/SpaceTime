using System;
using UnityEngine;

namespace Commonwealth.Script.Ship.Resources
{
    [System.Serializable]
    public class Resource
    {
        [Header("Resource Configuration")]
        public ResourceType resourceType;
        public float currentAmount;
        public float maxCapacity;
        public float minAmount;
        
        [Header("Thresholds")]
        public float warningThreshold = 0.3f; // 30% for warning
        public float criticalThreshold = 0.1f; // 10% for critical
        
        [Header("Consumption/Production")]
        public float consumptionRate; // Amount consumed per second
        public float productionRate; // Amount produced per second
        
        public Resource(ResourceType type, float maxCapacity = 1000f, float startingAmount = 500f)
        {
            this.resourceType = type;
            this.maxCapacity = maxCapacity;
            this.currentAmount = Mathf.Clamp(startingAmount, 0f, maxCapacity);
            this.minAmount = 0f;
            this.consumptionRate = 0f;
            this.productionRate = 0f;
        }
        
        public string DisplayName => resourceType.GetDisplayName();
        public string Unit => resourceType.GetUnit();
        public string Description => resourceType.GetDescription();
        
        public float FillPercentage => maxCapacity > 0 ? currentAmount / maxCapacity : 0f;
        public bool IsLow => FillPercentage <= warningThreshold;
        public bool IsCritical => FillPercentage <= criticalThreshold;
        public bool IsEmpty => currentAmount <= minAmount;
        public bool IsFull => currentAmount >= maxCapacity;
        
        public ResourceState CurrentState
        {
            get
            {
                if (IsCritical) return ResourceState.Critical;
                if (IsLow) return ResourceState.Warning;
                return ResourceState.Good;
            }
        }
        
        public void Consume(float amount)
        {
            currentAmount = Mathf.Max(minAmount, currentAmount - amount);
        }
        
        public void Produce(float amount)
        {
            currentAmount = Mathf.Min(maxCapacity, currentAmount + amount);
        }
        
        public void SetAmount(float amount)
        {
            currentAmount = Mathf.Clamp(amount, minAmount, maxCapacity);
        }
        
        public void UpdateOverTime(float deltaTime)
        {
            float netChange = (productionRate - consumptionRate) * deltaTime;
            if (netChange != 0)
            {
                if (netChange > 0)
                    Produce(netChange);
                else
                    Consume(-netChange);
            }
        }
        
        public bool CanConsume(float amount)
        {
            return currentAmount - amount >= minAmount;
        }
        
        public bool CanProduce(float amount)
        {
            return currentAmount + amount <= maxCapacity;
        }
        
        public override string ToString()
        {
            return $"{DisplayName}: {currentAmount:F1}/{maxCapacity:F1} {Unit} ({FillPercentage:P1})";
        }
    }
    
    public enum ResourceState
    {
        Good,
        Warning,
        Critical
    }
}