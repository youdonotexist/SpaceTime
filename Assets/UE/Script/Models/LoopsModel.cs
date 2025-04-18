using System.Linq;
using UE.Script;

namespace UE.Script.Models
{
    public class LoopsModel
    {
        public LoopModel[] loops;
        
        
    }
    
    public class LoopModel
    {
        public string name;
        public float loopLength;
        public ChannelModel[] channels;
        public string tool;
        public BeatModel[] beatEvents;

        public bool HasBeatType(BeatRules.BeatType beatType)
        {
            foreach (var beatEvent in beatEvents)
            {
                return BeatRules.BeatTypeForBeat(beatEvent.midiNotes).Contains(beatType);
            }

            return false;
        }
    }
    
    public class BeatModel
    {
        public float beat;
        public int[] midiNotes;
        public float duration;
        public int channel;
    }

    [System.Serializable]
    public class ChannelModel
    {
        public int preset;
        public int channel;
        public int bank;
    }

}