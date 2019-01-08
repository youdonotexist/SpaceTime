namespace Commonwealth.Script.Ship.Interactable
{
    public class Interactable : UnityEngine.MonoBehaviour
    {
        protected Ship Ship;

        public virtual string InteractableIdentifier()
        {
            return "generic";
            
        }

        public virtual void AssociateShip(Ship ship)
        {
            Ship = ship;
        }

        public virtual bool HasOverlay()
        {
            return false;
        }
    }
}