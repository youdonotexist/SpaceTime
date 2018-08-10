using System.Collections.Generic;

namespace Commonwealth.Script.Ship.EngineMod.Model
{
    [System.Serializable]
    public class EngineModel
    {
        public int SizeX;
        public int SizeY;

        public List<EngineSlotModel> Slots;
        public List<FuelSlotModel> FuelSlots;
    }
}