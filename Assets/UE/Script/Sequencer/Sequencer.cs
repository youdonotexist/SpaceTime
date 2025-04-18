using System;
using System.Collections.Generic;
using MidiPlayerTK;
using UE.Script.Models;
using UE.Script.Utility.ServiceLocatorSample.ServiceLocator;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UE.Script.Sequencer
{
    public class Sequencer : MonoBehaviour, IConductorEventReceiver
    {
        [Serializable]
        class SeqChannel
        {
            [SerializeField] 
            public int channel = 9;
            [SerializeField] 
            public int value = 1;
            [SerializeField] 
            public Cell[] buttons;
        } 
        
        public UkuleleEnvelopeInput input;
        
        [SerializeField]
        List<SeqChannel> channels;
        
        [SerializeField] ChannelModel[] channelConfigs;

        [SerializeField] private GemstrumentManager gemstrumentManager;

        public void SetSeqCells(List<Cell> cells, int index)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                channels[index].buttons = cells.ToArray();
            }
        }
        
        private int _lastPlayed = -1;
        private void Start()
        {
            ConductorManager manager = ServiceLocator.Current.Get<ConductorManager>();

            manager.Initialize(40   , 5);
            manager.EventReceiver.Add(this);
            
            input = new UkuleleEnvelopeInput();
            input.Enable();
            input.Player.Fire.started += OnFire;

            EndLoadingSynth("");
        }

        private void Update()
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -Camera.main.transform.position.z));
            
            transform.position = world;
            
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
                Cell cell = hit.collider.GetComponentInParent<Cell>();
                if (cell)
                {
                    cell.ToggleState(gemstrumentManager.SelectedGemstrument);
                }
            }
        }

        public void OnTick(float loopPositionInBeats, float songPositionInBeats, IMidiPlayer midiPlayer)
        {

            int index = Mathf.FloorToInt(loopPositionInBeats * 4); //for 4/4 

           for (int i = 0; i < channels.Count; i++)
           {
               Cell[] buttons = channels[i].buttons;
               if (index <= _lastPlayed || index >= buttons.Length) return;
               
               if (buttons[index].State == Cell.CellState.Enabled)
               {
                   //Debug.Log($"played note: {i}");
                   midiPlayer.PlayEvent(new MPTKEvent()
                   {
                       Command = MPTKCommand.NoteOn,
                       Value = channels[i].value,
                       Duration = 50,
                       Channel = Mathf.Clamp(channels[i].channel, 0, 24)
                   });
               }
           }
           _lastPlayed = index;
        }

        public void OnLoopCompleted()
        {
            Debug.Log("Loop completed");
            _lastPlayed = -1;
            
        }
        
        public void EndLoadingSynth(string name)
        {
            ChannelModel[] channelModels = {
                new() { preset = 81, channel = 0, bank = 15 },
                new() { preset = 118, channel = 1, bank = 1 }
            };
            
            /*ConductorManager manager = ServiceLocator.Current.Get<ConductorManager>();
            foreach (ChannelModel model in channelModels)
            {
                manager.mainPlayer.Channels[model.channel].BankNum = model.bank;
                manager.mainPlayer.Channels[model.channel].PresetNum = model.preset;
                
                manager.mainPlayer.MPTK_PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb,
                    Value = model.bank, Channel = model.channel
                });
                Debug.LogFormat($"   Bank '{model.bank}' defined on channel {model.channel}");
                Debug.LogFormat($"   Preset '{manager.mainPlayer.MPTK_Channels[model.channel].PresetName}' defined on channel {model.channel}");
            }*/
        }

        public void RebindChannels()
        {
            /*ConductorManager manager = ServiceLocator.Current.Get<ConductorManager>();
            foreach (ChannelModel model in channelConfigs)
            {
                manager.mainPlayer.Channels[model.channel].BankNum = model.bank;
                manager.mainPlayer.Channels[model.channel].PresetNum = model.preset;
                
                manager.mainPlayer.MPTK_PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.ControlChange, Controller = MPTKController.BankSelectMsb,
                    Value = model.bank, Channel = model.channel
                });
                Debug.LogFormat($"   Bank '{model.bank}' defined on channel {model.channel}");
                Debug.LogFormat($"   Preset '{manager.mainPlayer.MPTK_Channels[model.channel].PresetName}' defined on channel {model.channel}");
            }*/
        }

        [Serializable]
        public class Instrument
        {
            [SerializeField]
            public int BankNum;
            [SerializeField]
            public int PresetNum;
            [SerializeField]
            public string Name;

            public override string ToString()
            {
                return Name;
            }
            
            public override bool Equals(object other)
            {
                if (other == null) return false; // Handle null case

                // Check if the object is of the same type.  Important!
                Instrument otherInstrument = other as Instrument;
                if (otherInstrument == null) return false;

                // Compare all properties
                return BankNum == otherInstrument.BankNum &&
                       PresetNum == otherInstrument.PresetNum &&
                       Name == otherInstrument.Name;
            }

            // Important:  If you override Equals, you *must* also override GetHashCode!
            public override int GetHashCode()
            {
                // Combine hash codes of all properties to create a unique hash code for the object.
                unchecked // Overflow is fine, just wrap around
                {
                    int hash = 17;
                    hash = hash * 23 + BankNum.GetHashCode();
                    hash = hash * 23 + PresetNum.GetHashCode();
                    hash = hash * 23 + (Name?.GetHashCode() ?? 0); // Handle null Name case
                    return hash;
                }
            }
        }

        [Serializable]
        public class InstrumentList
        {
            [SerializeField]
            public List<Instrument> List = new();
        }

        public void DebugPrintInstruments()
        {
            ImSoundFont sfont = MidiPlayerGlobal.ImSFCurrent;

            InstrumentList list = new InstrumentList();
            

            for (int i = 0; i < sfont.Banks.Length; i++)
            {
                var bank = sfont.Banks[i];
                
                if (bank == null) continue;
                
                Debug.LogFormat($"   Bank '{bank.BankNumber}'");
                for (int j = 0; j < bank.defpresets.Length; j++)
                {
                    var preset = bank.defpresets[j];

                    if (preset == null) continue;

                    Instrument inst = new Instrument();
                    inst.BankNum = i;
                    inst.PresetNum = j;
                    inst.Name = preset.Name;
                    
                    list.List.Add(inst );
                    
                    Debug.LogFormat($"   Bank Desc'{preset?.Name}'");

                    string str = JsonUtility.ToJson(list);
                    //write string to file
                    System.IO.File.WriteAllText($"{Application.dataPath}/instruments.json", str);
                }
            }
        }
    }
    
    
}