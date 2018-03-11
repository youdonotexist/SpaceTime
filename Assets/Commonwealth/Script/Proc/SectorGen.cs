using System.Collections.Generic;
using Commonwealth.Script.Model;
using UnityEngine;
using ProceduralToolkit;

namespace Commonwealth.Script.Proc
{
    public class SectorGen
    {
        private static string _alphaNumerics;

        public static void Initialize(int seed)
        {
            Random.InitState(seed);

            _alphaNumerics = "ABCDEFGHIJKLMNOPQRSTUVWXYZ01234567890123456789          ";
        }

        public static List<Sector> GenerateSectors(float maxDist, float minDist, int count)
        {
            List<Sector> sectorList = new List<Sector>();


            for (int i = 0; i < count; i++)
            {
                Sector sector = new Sector(RandomSectorName(), Random.Range(minDist, maxDist));
                sector.AddFeature(new FuelFeature(Random.Range(100.0f, 500.0f)));
                sectorList.Add(sector);
            }

            return sectorList;
        }

        private static string RandomSectorName()
        {
            int length = Random.Range(5, 10);
            char[] name = new char[length];

            for (int i = 0; i < length; i++)
            {
                name[i] = _alphaNumerics.GetRandom();
            }

            return new string(name);
        }
    }
}