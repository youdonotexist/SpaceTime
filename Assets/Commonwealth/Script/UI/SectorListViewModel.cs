using System.Collections.Generic;
using Commonwealth.Script.Proc;
using UniRx;
using UnityEngine;

namespace Commonwealth.Script.UI
{
    public class SectorListViewModel : Object
    {
        public IObservable<List<Model.Sector>> GetSectorListStream()
        {
            List<Model.Sector> sectorList = SectorGen.GenerateSectors(5000.0f, 10000.0f, 5);
            return Observable.Return(sectorList);

        } 
    }
}