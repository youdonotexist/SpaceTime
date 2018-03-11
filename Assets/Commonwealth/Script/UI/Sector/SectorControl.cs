using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.UI.Sector
{
    public class SectorControl : MonoBehaviour
    {
        [SerializeField] private SectorPicker _sectorPicker;

        [SerializeField] private Button _sectorButton;

        [SerializeField] private Text _sectorText;

        public IObservable<Model.Sector> NewSectorStream()
        {
            return _sectorPicker.GetSectorPickerStreams()
                .Merge()
                .Do(sector => _sectorText.text = "Heading To: " + sector.Name);
        }

        public IObservable<Unit> PickSectorButtonStream()
        {
            return _sectorButton.OnClickAsObservable();
        }

        public void ShowPicker(List<Model.Sector> sectorList)
        {
            _sectorPicker.Show(sectorList);
        }

        public void HidePicker()
        {
            _sectorPicker.Hide();
        }
    }
}