using System;
using System.Collections.Generic;
using System.Linq;
using MidiPlayerTK;
using UE.Script;
using UE.Script.Utility.ServiceLocatorSample.ServiceLocator;
using UnityEngine;
using UnityEngine.InputSystem;

public class GemstrumentManager : MonoBehaviour, IConductorEventReceiver
{
    private SpaceTimeInput _input;

    private Gemstrument _selectedGemstrument;

    public Gemstrument SelectedGemstrument => _selectedGemstrument;

    private List<Gemstrument> _allGemstruments;

    [SerializeField]
    private SpriteRenderer selectedSprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        _allGemstruments = GetComponentsInChildren<Gemstrument>().ToList();
    }

    void Start()
    {
        ConductorManager manager = ServiceLocator.Current.Get<ConductorManager>();
        manager.mainPlayer.OnEventSynthStarted.AddListener(OnSynthReady);
        
        _selectedGemstrument = _allGemstruments.First();
        selectedSprite.transform.position = _selectedGemstrument.transform.position;
    }

    private void OnEnable()
    {
        if (_input == null)
            _input = new SpaceTimeInput();
        _input.Enable();
        _input.Player.Fire.started += OnFire;
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.Player.Fire.started -= OnFire;
            _input.Disable();
        }
    }

    void OnSynthReady(string arg0)
    {
        ConductorManager manager = ServiceLocator.Current.Get<ConductorManager>();
        for (int i = 0; i < _allGemstruments.Count; i++)
        {
          
                Gemstrument model = _allGemstruments[i];
                manager.mainPlayer.MPTK_Channels[i].PresetNum = model.Instrument.PresetNum; // Nylon Guitar
                manager.mainPlayer.MPTK_Channels[i].BankNum = model.Instrument.BankNum;    // Bank 0
                Debug.Log($"Channel {i}: Preset={manager.mainPlayer.MPTK_Channels[i].PresetNum}, Bank={manager.mainPlayer.MPTK_Channels[i].BankNum}");    
        }
    }

    private void OnFire(InputAction.CallbackContext obj)
    {
        // Get pointer position in screen space
        Vector2 screenPosition = Mouse.current.position.ReadValue();

        if (Camera.main != null)
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z));
                
            RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero, Mathf.Infinity, LayerMask.GetMask("Click"));
            if (!hit) return;
            Gemstrument cell = hit.collider.GetComponentInParent<Gemstrument>();
            if (cell)
            {
                _selectedGemstrument = cell;
                selectedSprite.transform.position = _selectedGemstrument.transform.position;
                
                IMidiPlayer playuer = ServiceLocator.Current.Get<ConductorManager>();
                int channel = _allGemstruments.IndexOf(_selectedGemstrument);
                playuer.PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.NoteOn, // midi command
                    Value = 60, // from 0 to 127, 48 for C4, 60 for C5, ...
                    Channel = channel, // from 0 to 15, 9 reserved for drum
                    Duration = 500, // note duration in millisecond, -1 to play undefinitely, MPTK_StopChord to stop
                    Velocity = 100, // from 0 to 127, sound can vary depending on the velocity
                    Delay = 0, // delay in millisecond before playing the note
                });
            }
        }
    }

    public void OnTick(float loopPositionInBeats, float songPositionInBeats, IMidiPlayer midiPlayer)
    {
        
    }

    public void OnLoopCompleted()
    {
        
    }
}
