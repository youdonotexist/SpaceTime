using Commonwealth.Script.UI.Engine;
using Commonwealth.Script.UI.Sector;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.UI
{
    public class OverlayManager : MonoBehaviour
    {
        [SerializeField] private SectorControl _sectorControls;
        [SerializeField] private EngineOverlay _engineControls;


        public IObservable<Unit> PickSectorStream => _sectorControls.PickSectorButtonStream();
        public IObservable<Model.Sector> NewSectorStream => _sectorControls.NewSectorStream();
        
        public SectorControl GetSectorPicker()
        {
            return _sectorControls;
        }
        
    }
}