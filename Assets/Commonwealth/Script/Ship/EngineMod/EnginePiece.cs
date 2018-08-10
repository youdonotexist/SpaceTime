using System;
using System.Collections.Generic;
using Commonwealth.Script.EditorUtils;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class EnginePiece : MonoBehaviour
    {
        
        [Flags] public enum SlotAttributes
        {
            None = 0,
            Fuel = 1 << 0,
            Tile = 1 << 1,
            Pipe = 1 << 2,
            Damaged = 1 << 3
        }
        
        [Flags]
        public enum Direction
        {
            None = 0,
            Up = 1 << 0,
            Down = 1 << 1,
            Left = 1 << 2,
            Right = 1 << 3
                    
        }

        [EnumFlags]
        [SerializeField] private Direction _directions;
        [FormerlySerializedAs("_pieceType")] [SerializeField] private SlotAttributes _slotAttributes;

        public SlotAttributes Type => _slotAttributes;
        public Vector3 OriginalPos => _originalPos;

        private Collider _collider;
        private SpriteRenderer _renderer;
        private Vector3 _originalPos;

        public float EfficiencyMultiplier { get; set; } = 1.0f;

        void Awake()
        {
            _collider = GetComponent<Collider>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            _originalPos = transform.position;
        }

        public bool CanMoveFrom(Direction direction)
        {
            return (_directions & direction) == direction;
        }

        public Direction GetDirectionFlag()
        {
            return _directions;
        }
        
        public Direction[] GetDirections(Direction ignore = Direction.None)
        {
            List<Direction> directions = new List<Direction>();
            
            if (Direction.Down != ignore && CanMoveFrom(Direction.Down)) directions.Add(Direction.Down);
            if (Direction.Up != ignore && CanMoveFrom(Direction.Up)) directions.Add(Direction.Up);
            if (Direction.Left != ignore && CanMoveFrom(Direction.Left)) directions.Add(Direction.Left);
            if (Direction.Right != ignore && CanMoveFrom(Direction.Right)) directions.Add(Direction.Right);

            return directions.ToArray();
        }


        public static Vector2 VectorForDirection(Direction direction)
        {
            if (direction == Direction.Up) return Vector2.up;
            if (direction == Direction.Down) return Vector2.down;
            if (direction == Direction.Left) return Vector2.left;
            if (direction == Direction.Right) return Vector2.right;
            
            return Vector2.zero;
        }

        public void SetSprite(Sprite sprite)
        {
            _renderer.sprite = sprite;
        }

        public string GetPartName()
        {
            return _renderer.sprite.name;
        }

        public void SetDirections(Direction pieceModelDirection)
        {
            _directions = pieceModelDirection;
        }

        public void SetAttributes(SlotAttributes slotAttributes)
        {
            _slotAttributes = slotAttributes;
        }
    }
}