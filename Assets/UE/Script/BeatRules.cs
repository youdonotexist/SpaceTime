using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UE.Script
{
    public class BeatRules : MonoBehaviour
    {
        public enum BeatType
        {
            Red, 
            Blue, 
            Green
        }
        
        public static Color ColorForBeats(int[] notes)
        {
            BeatType[] t = BeatTypeForBeat(notes);
            bool isBlue = t.Contains(BeatType.Blue);
            bool isGreen = t.Contains(BeatType.Green);

            if (isBlue && isGreen)
            {
                return Color.cyan;
            }

            if (isBlue)
            {
                return Color.blue;
            }
            
            if (isGreen)
            {
                return Color.green;
            }

            return Color.red;
        }

        public static BeatType[] BeatTypeForBeat(int[] notes)
        {
            List<BeatType> types = new();
            
            for (int i = 0; i < notes.Length; i++)
            {
                if (notes[i] % 3 == 0)
                {
                    types.Add(BeatType.Blue);
                    continue;
                }

                if (notes[i] % 2 == 0)
                {
                    types.Add(BeatType.Green);
                    continue;
                }
                
                types.Add(BeatType.Red);
            }

            return types.ToArray();
        }
    }
}