using System;
using System.Collections.Generic;
using System.Linq;
using Commonwealth.Script.EditorUtils;
using UnityEngine;

namespace Commonwealth.Script.Ship.EngineMod
{
    public class EngineSlot : MonoBehaviour
    {
        private EnginePiece _installedPiece;
        private SpriteRenderer _renderer;
        private Color _originalColor;

        private EngineSlot _up;
        private EngineSlot _down;
        private EngineSlot _left;
        private EngineSlot _right;

        [EnumFlags] [SerializeField] private EnginePiece.PieceType _acceptedPieces;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _originalColor = _renderer.color;
        }

        public void SetPiece(EnginePiece piece)
        {
            if (piece != null)
            {
                piece.transform.parent = transform;
                piece.transform.localPosition = Vector3.zero;
                _installedPiece = piece;
            }
        }

        public void UninstallPiece()
        {
            if (_installedPiece != null)
            {
                _installedPiece.transform.parent = null;
                _installedPiece = null;
            }
        }

        public void SetSelected(bool selected)
        {
            _renderer.color = selected ? Color.red : _originalColor;
        }


        public bool HasPiece()
        {
            return _installedPiece != null;
        }

        public EnginePiece InstalledPiece()
        {
            return _installedPiece;
        }

        public bool AcceptsPiece(EnginePiece.PieceType type)
        {
            return (_acceptedPieces & type) == type;
        }

        public bool CanMoveFrom(EnginePiece.Direction direction)
        {
            bool hasPiece = _installedPiece != null;
            return hasPiece && _installedPiece.CanMoveFrom(direction);;
        }

        public void SetAdjacentSlots(EngineSlot up, EngineSlot down, EngineSlot left, EngineSlot right)
        {
            _up = up;
            _down = down;
            _left = left;
            _right = right;
        }

        public EnginePiece.Direction[] GetValidAdjacentDirections(EngineSlot exclude)
        {
            if (_installedPiece != null)
            {
                EnginePiece.Direction[] possibleDirections = _installedPiece.GetDirections();
                List<EnginePiece.Direction> directions = new List<EnginePiece.Direction>(possibleDirections);
                if (exclude != null)
                {
                    EnginePiece.Direction excludeDirection = DirectionOf(exclude);
                    directions.Remove(excludeDirection);
                }

                return directions.ToArray();
            }

            return new EnginePiece.Direction[0];
        }

        public EngineSlot[] GetValidAdjacentSlots(EngineSlot exclude)
        {
            List<EngineSlot> availableSlots = new List<EngineSlot>();
            if (_up != null && _up.CanMoveFrom(EnginePiece.Direction.Down)) availableSlots.Add(_up);
            if (_down != null && _down.CanMoveFrom(EnginePiece.Direction.Up)) availableSlots.Add(_down);
            if (_left != null && _left.CanMoveFrom(EnginePiece.Direction.Right)) availableSlots.Add(_left);
            if (_right != null && _right.CanMoveFrom(EnginePiece.Direction.Left)) availableSlots.Add(_right);
            
            if (exclude != null)
            {
                availableSlots.Remove(exclude);
            }

            return availableSlots.ToArray();
        }

        public EngineSlot GetFirst(EngineSlot exclude)
        {
            List<EngineSlot> availableSlots = new List<EngineSlot>();
            if (_up != null && _up.CanMoveFrom(EnginePiece.Direction.Down)) availableSlots.Add(_up);
            if (_down != null && _down.CanMoveFrom(EnginePiece.Direction.Up)) availableSlots.Add(_down);
            if (_left != null && _left.CanMoveFrom(EnginePiece.Direction.Right)) availableSlots.Add(_left);
            if (_right != null && _right.CanMoveFrom(EnginePiece.Direction.Left)) availableSlots.Add(_right);

            if (exclude != null)
            {
                availableSlots.Remove(exclude);
            }

            if (availableSlots.Count == 0)
            {
                return null;
            }

            return availableSlots.First();
        }

        public EnginePiece.Direction DirectionOf(EngineSlot slot)
        {
            if (slot == _up) return EnginePiece.Direction.Up;
            if (slot == _down) return EnginePiece.Direction.Down;
            if (slot == _left) return EnginePiece.Direction.Left;
            if (slot == _right) return EnginePiece.Direction.Right;

            return EnginePiece.Direction.None;
        }

        public EngineSlot SlotForDirection(EnginePiece.Direction direction)
        {
            if (direction == EnginePiece.Direction.Up) return _up;
            if (direction == EnginePiece.Direction.Down) return _down;
            if (direction == EnginePiece.Direction.Left) return _left;
            if (direction == EnginePiece.Direction.Right) return _right;

            return null;
        }
        
        public EngineSlot ValidSlotForDirection(EnginePiece.Direction direction)
        {
            if (_installedPiece == null) return null;
            
            if (direction == EnginePiece.Direction.Up && _up != null &&_up.CanMoveFrom(EnginePiece.Direction.Down)) return _up;
            if (direction == EnginePiece.Direction.Down && _down != null && _down.CanMoveFrom(EnginePiece.Direction.Up)) return _down;
            if (direction == EnginePiece.Direction.Left && _left != null && _left.CanMoveFrom(EnginePiece.Direction.Right)) return _left;
            if (direction == EnginePiece.Direction.Right && _right != null && _right.CanMoveFrom(EnginePiece.Direction.Left)) return _right;

            return null;
        }
    }
}