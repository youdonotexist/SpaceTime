using System.Collections.Generic;
using Commonwealth.Script.Model;
using UnityEngine;

namespace Commonwealth.Script.Proc
{
    public class SectorManager : MonoBehaviour
    {
        public List<Sector> SectorList = new List<Sector>();

        private void Awake()
        {
            SectorGen.Initialize(1);
            SectorList = SectorGen.GenerateSectors(50.0f, 1000.0f, 10);
        }
    }
}