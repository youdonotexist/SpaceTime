namespace Commonwealth.Script.Ship.EngineMod.Model
{
    [System.Serializable]
    public class EnginePieceModel
    {
        public float EfficiencyMultiplier;
        public EnginePiece.Direction Direction;
        public string VisualType;
        public EnginePiece.SlotAttributes SlotAttributes;
    }
}