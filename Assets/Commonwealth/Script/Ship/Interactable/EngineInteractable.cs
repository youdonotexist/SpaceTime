using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace Commonwealth.Script.Ship.Interactable
{
    [ExecuteInEditMode]
    public class EngineInteractable : Interactable
    {
        [SerializeField] private new string name;


        
        void Awake()
        {
            #if UNITY_EDITOR
            name = ObjectNames.GetClassName(this);
            #endif

        }
        public override string InteractableIdentifier()
        {
            return name;
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