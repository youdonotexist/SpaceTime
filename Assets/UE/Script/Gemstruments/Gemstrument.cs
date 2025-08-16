using UnityEngine;

namespace UE.Script.Gemstruments
{
    public class Gemstrument : MonoBehaviour, Cell.ICellConfiguration
    {
        [SerializeField] private Sequencer.Sequencer.Instrument instrument;

        [SerializeField] private Sprite gemSprite;
        public Sequencer.Sequencer.Instrument Instrument => instrument;

        public Sprite GemSprite => gemSprite;


        public Sprite GetSprite()
        {
            return gemSprite;
        }

        public Color GetColor()
        {
            return Color.white;
        }
    }
}
