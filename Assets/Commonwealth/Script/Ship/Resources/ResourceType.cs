using System;

namespace Commonwealth.Script.Ship.Resources
{
    [Serializable]
    public enum ResourceType
    {
        MetalPlates,
        PolymerGlue,
        ConductiveWiring,
        CoolantsFluids,
        FuelCells,
        PlasmaCartridges
    }
    
    public static class ResourceTypeExtensions
    {
        public static string GetDisplayName(this ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.MetalPlates => "Metal Plates",
                ResourceType.PolymerGlue => "Polymer/Glue",
                ResourceType.ConductiveWiring => "Conductive Wiring",
                ResourceType.CoolantsFluids => "Coolants/Fluids",
                ResourceType.FuelCells => "Fuel Cells",
                ResourceType.PlasmaCartridges => "Plasma Cartridges",
                _ => resourceType.ToString()
            };
        }
        
        public static string GetUnit(this ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.MetalPlates => "kg",
                ResourceType.PolymerGlue => "L",
                ResourceType.ConductiveWiring => "m",
                ResourceType.CoolantsFluids => "L",
                ResourceType.FuelCells => "units",
                ResourceType.PlasmaCartridges => "cartridges",
                _ => "units"
            };
        }
        
        public static string GetDescription(this ResourceType resourceType)
        {
            return resourceType switch
            {
                ResourceType.MetalPlates => "Structural materials for hull repairs and construction",
                ResourceType.PolymerGlue => "Adhesive compounds for sealing and bonding components",
                ResourceType.ConductiveWiring => "Electrical conduits for power and data transmission",
                ResourceType.CoolantsFluids => "Thermal regulation fluids for heat management systems",
                ResourceType.FuelCells => "Energy storage units for power generation",
                ResourceType.PlasmaCartridges => "High-energy plasma for advanced propulsion and weapons",
                _ => "Unknown resource"
            };
        }
    }
}