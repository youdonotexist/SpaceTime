using System.Collections.Generic;
using Commonwealth.Script.UI.Common;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.UI.Sector
{
    public class SectorControl : Overlay
    {
        [SerializeField] private SectorPicker _sectorPicker;

        [SerializeField] private Button _sectorButton;

        [SerializeField] private Text _sectorText;

        private SectorListViewModel _viewModel = new SectorListViewModel();

        void Awake()
        {
            _viewModel.GetSectorListStream().Subscribe(list => _sectorPicker.SetData(list));
        }

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


        public override bool Show(bool show)
        {
            if (show)
            {
                _sectorPicker.Show();
            }
            else
            {
                _sectorPicker.Hide();
            }

            return show;
        }
    }
}