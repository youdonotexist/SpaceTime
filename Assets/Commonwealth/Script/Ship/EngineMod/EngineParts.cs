using System.Collections.Generic;
using Commonwealth.Script.Ship.EngineMod.Model;
using UnityEngine;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class EngineParts : MonoBehaviour
    {
        [SerializeField] private string _spriteName = "engine_parts";
        [SerializeField] private EnginePiece _enginePiecePrefab;
        [SerializeField] private EnginePiece _fuelContainerPrefab;

        private Dictionary<string, Sprite> _spriteMap = new Dictionary<string, Sprite>();

        void Awake()
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(_spriteName);
            foreach (var sprite in sprites)
            {
                _spriteMap[sprite.name] = sprite;
            }
        } 
        
        public enum EnginePartNames
        {
            Horizontal,
            Verical,
            DownToRight,
            DownToLeft,
            UpToLeft,
            UpToRight
        }

        public EnginePiece EnginePieceWithName(EnginePieceModel enginePieceModel)
        {
            EnginePiece piece = Instantiate(_enginePiecePrefab);
            if (_spriteMap.ContainsKey(enginePieceModel.VisualType))
            {
                Sprite sprite = _spriteMap[enginePieceModel.VisualType];
                piece.SetSprite(sprite);
            }

            piece.EfficiencyMultiplier = enginePieceModel.EfficiencyMultiplier;
            piece.SetAttributes(enginePieceModel.SlotAttributes);
            piece.SetDirections(enginePieceModel.Direction);

            return piece;
        }

        public EnginePiece FuelContainerWithName(FuelContainerModel fuelContainerModel)
        {
            EnginePiece piece = Instantiate(_fuelContainerPrefab);
            FuelContainer fuelCellPiece = piece.GetComponent<FuelContainer>();
            fuelCellPiece.AvailableFuel = fuelContainerModel.AvailableFuel;
            fuelCellPiece.MaxCapacity = fuelContainerModel.MaxCapacity;
            return piece;
        }
        

    }
}