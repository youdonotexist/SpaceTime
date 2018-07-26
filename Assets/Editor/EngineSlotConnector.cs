using Commonwealth.Script.Ship.EngineMod;
using UnityEditor;
using UnityEngine;

namespace CW.Editor
{
    public class EngineSlotConnector
    {
        [MenuItem("Tools/Build Engine Slot Connections")]
        private static void CenterPivot()
        {
            Transform t = Selection.activeTransform;
            EngineModManager engineModManager = t == null ? null : t.GetComponent<EngineModManager>();

            if (engineModManager != null)
            {
                EngineSlot[] slots = engineModManager.GetComponentsInChildren<EngineSlot>();

                foreach (EngineSlot slot in slots)
                {
                    EngineSlot up;
                    EngineSlot down = null;
                    EngineSlot left = null;
                    EngineSlot right = null;

                    up = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Up)));
                    
                    //For fuel types, only look up
                    if (!slot.AcceptsPiece(EnginePiece.SlotAttributes.Fuel))
                    {
                        down = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Down)));
                        left = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Left)));
                        right = ExtractSlot(Physics2D.OverlapPoint(GetPoint(slot, EnginePiece.Direction.Right)));
                    }

                    slot.SetAdjacentSlots(up, down, left, right);

                }
            }
        }


        private static Vector2 GetPoint(EngineSlot slot, EnginePiece.Direction direction)
        {
            Bounds b = slot.GetComponent<Collider2D>().bounds;
            
            if (direction == EnginePiece.Direction.Up)
            {
                return new Vector2(slot.transform.position.x, slot.transform.position.y + b.size.y);
            }

            if (direction == EnginePiece.Direction.Down)
            {
                return new Vector2(slot.transform.position.x, slot.transform.position.y - b.size.y);
            }

            if (direction == EnginePiece.Direction.Left)
            {
                return new Vector2(slot.transform.position.x - b.size.x, slot.transform.position.y);
            }

            return new Vector2(slot.transform.position.x + b.size.x, slot.transform.position.y);
        }

        private static EngineSlot ExtractSlot(Collider2D collider)
        {
            if (collider == null) return null;

            return collider.gameObject.GetComponent<EngineSlot>();
        }
    }
}