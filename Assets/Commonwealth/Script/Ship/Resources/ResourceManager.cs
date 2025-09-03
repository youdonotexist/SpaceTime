using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Commonwealth.Script.Ship.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        [Header("Resource Configuration")]
        [SerializeField] private List<Resource> resources = new List<Resource>();
        
        [Header("Update Settings")]
        [SerializeField] private float updateInterval = 1.0f;
        [SerializeField] private bool enableAutoUpdate = true;
        
        private Dictionary<ResourceType, Resource> resourceLookup = new Dictionary<ResourceType, Resource>();
        private float lastUpdateTime;
        
        // Events
        public event Action<Resource> OnResourceChanged;
        public event Action<Resource, ResourceState> OnResourceStateChanged;
        public event Action OnResourcesUpdated;
        
        // Properties
        public List<Resource> AllResources => resources;
        public List<Resource> CriticalResources => resources.Where(r => r.IsCritical).ToList();
        public List<Resource> LowResources => resources.Where(r => r.IsLow).ToList();
        public int TotalResourceTypes => resources.Count;
        
        private void Awake()
        {
            InitializeResources();
            BuildResourceLookup();
        }
        
        private void Start()
        {
            lastUpdateTime = Time.time;
        }
        
        private void Update()
        {
            if (enableAutoUpdate && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateAllResources();
                lastUpdateTime = Time.time;
            }
        }
        
        private void InitializeResources()
        {
            // Initialize all 6 primary resources if not already set up
            if (resources.Count == 0)
            {
                resources.Add(new Resource(ResourceType.MetalPlates, 5000f, 2500f));
                resources.Add(new Resource(ResourceType.PolymerGlue, 1000f, 500f));
                resources.Add(new Resource(ResourceType.ConductiveWiring, 2000f, 1000f));
                resources.Add(new Resource(ResourceType.CoolantsFluids, 3000f, 1500f));
                resources.Add(new Resource(ResourceType.FuelCells, 500f, 250f));
                resources.Add(new Resource(ResourceType.PlasmaCartridges, 100f, 50f));
                
                Debug.Log($"Initialized {resources.Count} primary resources");
            }
            
            // Ensure we have all 6 primary resources
            var existingTypes = resources.Select(r => r.resourceType).ToHashSet();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!existingTypes.Contains(type))
                {
                    var newResource = CreateDefaultResource(type);
                    resources.Add(newResource);
                    Debug.Log($"Added missing resource: {newResource.DisplayName}");
                }
            }
        }
        
        private Resource CreateDefaultResource(ResourceType type)
        {
            return type switch
            {
                ResourceType.MetalPlates => new Resource(type, 5000f, 2500f),
                ResourceType.PolymerGlue => new Resource(type, 1000f, 500f),
                ResourceType.ConductiveWiring => new Resource(type, 2000f, 1000f),
                ResourceType.CoolantsFluids => new Resource(type, 3000f, 1500f),
                ResourceType.FuelCells => new Resource(type, 500f, 250f),
                ResourceType.PlasmaCartridges => new Resource(type, 100f, 50f),
                _ => new Resource(type, 1000f, 500f)
            };
        }
        
        private void BuildResourceLookup()
        {
            resourceLookup.Clear();
            foreach (var resource in resources)
            {
                resourceLookup[resource.resourceType] = resource;
            }
        }
        
        private void UpdateAllResources()
        {
            foreach (var resource in resources)
            {
                var previousState = resource.CurrentState;
                resource.UpdateOverTime(updateInterval);
                
                // Trigger events for changes
                OnResourceChanged?.Invoke(resource);
                
                if (resource.CurrentState != previousState)
                {
                    OnResourceStateChanged?.Invoke(resource, resource.CurrentState);
                }
            }
            
            OnResourcesUpdated?.Invoke();
        }
        
        // Public API
        public Resource GetResource(ResourceType type)
        {
            return resourceLookup.TryGetValue(type, out var resource) ? resource : null;
        }
        
        public bool HasResource(ResourceType type, float amount)
        {
            var resource = GetResource(type);
            return resource != null && resource.CanConsume(amount);
        }
        
        public bool ConsumeResource(ResourceType type, float amount)
        {
            var resource = GetResource(type);
            if (resource != null && resource.CanConsume(amount))
            {
                var previousState = resource.CurrentState;
                resource.Consume(amount);
                
                OnResourceChanged?.Invoke(resource);
                if (resource.CurrentState != previousState)
                {
                    OnResourceStateChanged?.Invoke(resource, resource.CurrentState);
                }
                return true;
            }
            return false;
        }
        
        public bool ProduceResource(ResourceType type, float amount)
        {
            var resource = GetResource(type);
            if (resource != null && resource.CanProduce(amount))
            {
                var previousState = resource.CurrentState;
                resource.Produce(amount);
                
                OnResourceChanged?.Invoke(resource);
                if (resource.CurrentState != previousState)
                {
                    OnResourceStateChanged?.Invoke(resource, resource.CurrentState);
                }
                return true;
            }
            return false;
        }
        
        public void SetResourceAmount(ResourceType type, float amount)
        {
            var resource = GetResource(type);
            if (resource != null)
            {
                var previousState = resource.CurrentState;
                resource.SetAmount(amount);
                
                OnResourceChanged?.Invoke(resource);
                if (resource.CurrentState != previousState)
                {
                    OnResourceStateChanged?.Invoke(resource, resource.CurrentState);
                }
            }
        }
        
        public void SetResourceProductionRate(ResourceType type, float rate)
        {
            var resource = GetResource(type);
            if (resource != null)
            {
                resource.productionRate = rate;
            }
        }
        
        public void SetResourceConsumptionRate(ResourceType type, float rate)
        {
            var resource = GetResource(type);
            if (resource != null)
            {
                resource.consumptionRate = rate;
            }
        }
        
        public List<Resource> GetResourcesByState(ResourceState state)
        {
            return resources.Where(r => r.CurrentState == state).ToList();
        }
        
        public void LogResourceStatus()
        {
            Debug.Log("=== Resource Status ===");
            foreach (var resource in resources.OrderBy(r => r.resourceType))
            {
                Debug.Log($"{resource} - State: {resource.CurrentState}");
            }
        }
        
        // Integration with ship parts system
        public void ApplyShipPartEffects(Dictionary<ResourceType, float> productionRates, Dictionary<ResourceType, float> consumptionRates)
        {
            foreach (var kvp in productionRates)
            {
                SetResourceProductionRate(kvp.Key, kvp.Value);
            }
            
            foreach (var kvp in consumptionRates)
            {
                SetResourceConsumptionRate(kvp.Key, kvp.Value);
            }
        }
    }
}