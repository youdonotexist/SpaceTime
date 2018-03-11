using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.UI.Sector
{
    public class SectorPicker : MonoBehaviour
    {
        public SectorContainer[] Sectors;

        void Awake()
        {
            
        }

        public void Show(List<Model.Sector> sectorList)
        {
            gameObject.SetActive(true);

            for (int i = 0; i < Sectors.Length; i++)
            {
                SectorContainer container = Sectors[i];
                if (i < sectorList.Count)
                {
                    Model.Sector sector = sectorList[i];
                    container.SetSector(sector);
                }
                else
                {
                    container.SetSector(null);
                }
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public List<IObservable<Model.Sector>> GetSectorPickerStreams()
        {
            List<IObservable<Model.Sector>> sectorStreams = new List<IObservable<Model.Sector>>();

            for (int i = 0; i < Sectors.Length; i++)
            {
                SectorContainer container = Sectors[i];
                sectorStreams.Add(container.GetClickStream());
            }

            return sectorStreams;
        }
    }
}