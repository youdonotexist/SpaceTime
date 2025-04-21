using UE.Script.Sequencer;
using UnityEngine;

public class Gemstrument : MonoBehaviour, Cell.ICellConfiguration
{
    [SerializeField] private Sequencer.Instrument instrument;

    [SerializeField] private Sprite gemSprite;
    public Sequencer.Instrument Instrument => instrument;

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
