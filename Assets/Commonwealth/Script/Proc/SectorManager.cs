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
            SectorList = SectorGen.GenerateSectors(5000.0f, 10000000.0f, 10);
        }
    }
}