using Commonwealth.Script.UI.Common;

namespace Commonwealth.Script.UI.Engine
{
    public class EngineOverlay : Overlay
    {
        
        public override bool Show(bool show)
        {
            gameObject.SetActive(show);
            return gameObject.activeSelf;
        }
    }
}