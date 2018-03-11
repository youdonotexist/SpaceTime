using System.Globalization;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Commonwealth.Script.UI.Sector
{
	public class SectorContainer : MonoBehaviour
	{
		[SerializeField]
		private Text _sectorName;
		
		[SerializeField]
		private Text _sectorDistsance;

		[SerializeField] private Button _selectButton;

		private Model.Sector _sector;

		public void SetSector(Model.Sector sector)
		{
			_sector = sector;
			_sectorName.text = sector != null ? _sector.Name : "";
			_sectorDistsance.text = sector != null ? _sector.Distance.ToString(CultureInfo.InvariantCulture) : "";
		}

		public IObservable<Model.Sector> GetClickStream()
		{
			return _selectButton.OnClickAsObservable()
				.Where(unit => _sector != null)
				.Select(unit => _sector);
		}
	}
}
