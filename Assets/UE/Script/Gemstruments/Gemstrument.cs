using UE.Script.Sequencer;
using UnityEngine;

public class Gemstrument : MonoBehaviour, Cell.ICellConfiguration
{
    [SerializeField] private Sequencer.Instrument instrument;

    [SerializeField] private Sprite gemSprite;
    public Sequencer.Instrument Instrument => instrument;

    public Sprite GemSprite => gemSprite;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Sprite GetSprite()
    {
        return gemSprite;
    }

    public Color GetColor()
    {
        return Color.white;
    }
}
