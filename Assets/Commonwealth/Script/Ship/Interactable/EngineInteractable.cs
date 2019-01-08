using UnityEditor;
using UnityEngine;

namespace Commonwealth.Script.Ship.Interactable
{
    public class EngineInteractable : Interactable
    {
        public override string InteractableIdentifier()
        {
            return ObjectNames.GetClassName(this);
        }

        public override void AssociateShip(Ship ship)
        {
            Ship = ship;
        }

        public override bool HasOverlay()
        {
            return true;
        }

        public void OnUse()
        {
            Ship.ShowOverlay(InteractableIdentifier());
        }
    }
}