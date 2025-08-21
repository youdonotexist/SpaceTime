using System;
using System.Collections.Generic;
using MidiPlayerTK;
using UE.Script;
using UE.Script.Models;
using UE.Script.Utility.ServiceLocatorSample.ServiceLocator;
using UnityEngine;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// MIDI-focused sequencer for SpriteRuntimeGraph that integrates with conductor manager
    /// and handles MIDI note playback based on graph node events
    /// </summary>
    public class SpriteRuntimeGraphSequencer : MonoBehaviour
    {
        [System.Serializable]
        public class MidiChannel
        {
            [SerializeField]
            public int channel = 0;
            [SerializeField]
            public int bankNum = 0;
            [SerializeField]
            public int presetNum = 0;
            [SerializeField]
            public bool mute = false;
            [SerializeField]
            public float volume = 1.0f;
        }

        [System.Serializable]
        public class Instrument
        {
            [SerializeField]
            public int BankNum;
            [SerializeField]
            public int PresetNum;
            [SerializeField]
            public string Name;
            [SerializeField]
            public int BoundChannel;

            public override string ToString()
            {
                return Name;
            }
            
            public override bool Equals(object other)
            {
                if (other == null) return false;

                Instrument otherInstrument = other as Instrument;
                if (otherInstrument == null) return false;

                return BankNum == otherInstrument.BankNum &&
                       PresetNum == otherInstrument.PresetNum &&
                       Name == otherInstrument.Name;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + BankNum.GetHashCode();
                    hash = hash * 23 + PresetNum.GetHashCode();
                    hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                    return hash;
                }
            }
        }

        [System.Serializable]
        public class InstrumentList
        {
            [SerializeField]
            public List<Instrument> List = new();
        }

        [Header("MIDI Configuration")]
        [SerializeField] private List<MidiChannel> midiChannels = new List<MidiChannel>();
        [SerializeField] private InstrumentList availableInstruments = new InstrumentList();
        
        [Header("Playback Settings")]
        public int beatsPerLoop = 4;
        public int beatsPerMinute = 120;
        
        [Header("Runtime Graph Integration")]
        [SerializeField] private SpriteRuntimeGraph runtimeGraph;
        
        [Header("Midi Stream Integration")]
        [SerializeField] public MidiStreamPlayer midiPlayer;
        
        // Runtime state
        private Dictionary<string, int> nodeToChannelMapping = new Dictionary<string, int>();
        private int currentBeat = -1;
        
        // Events for external integration
        public System.Action<int, float, float> OnMidiTick;
        public System.Action OnMidiLoopCompleted;
        
        private void Awake()
        {
            // Find runtime graph if not assigned
            if (runtimeGraph == null)
            {
                runtimeGraph = GetComponent<SpriteRuntimeGraph>();
                if (runtimeGraph == null)
                {
                    runtimeGraph = FindObjectOfType<SpriteRuntimeGraph>();
                }
            }

            runtimeGraph.OnNodeActivated += OnNodeActivated;
        }
        
        private void Start()
        {
            InitializeConductorManager();
            InitializeInstruments();
            SetupNodeChannelMapping();
        }
        
        private void InitializeConductorManager()
        {
            
        }
        
        private void InitializeInstruments()
        {
            // Setup default MIDI channels if none configured
            if (midiChannels.Count == 0)
            {
                // Create default channels for common instruments
                midiChannels.Add(new MidiChannel { channel = 0, bankNum = 0, presetNum = 1 }); // Piano
                midiChannels.Add(new MidiChannel { channel = 1, bankNum = 0, presetNum = 25 }); // Steel Guitar
                midiChannels.Add(new MidiChannel { channel = 2, bankNum = 0, presetNum = 33 }); // Bass
                midiChannels.Add(new MidiChannel { channel = 9, bankNum = 128, presetNum = 0 }); // Drums
            }
            
            // Initialize channel configurations
            //midiPlayer.OnEventSynthStarted.AddListener(SetupMidiChannels);
            SetupMidiChannels();
        }


        private void SetupMidiChannels()
        {
            // Find MidiStreamPlayer if not assigned
            if (midiPlayer == null)
            {
                midiPlayer = GetComponent<MidiStreamPlayer>();
                if (midiPlayer == null)
                {
                    midiPlayer = FindObjectOfType<MidiStreamPlayer>();
                }
            }
            
            if (midiPlayer == null) 
            {
                Debug.LogError("MidiStreamPlayer not found! Please assign one to the SpriteRuntimeGraphSequencer.");
                return;
            }
            
            foreach (var midiChannel in midiChannels)
            {
                if (midiChannel.channel >= 0 && midiChannel.channel < 16)
                {
                    var channel = midiPlayer.Channels[midiChannel.channel];
                    channel.BankNum = midiChannel.bankNum;
                    channel.PresetNum = midiChannel.presetNum;
                    channel.Volume = midiChannel.volume;
                    
                    // Send bank select and program change
                    midiPlayer.MPTK_PlayEvent(new MPTKEvent()
                    {
                        Command = MPTKCommand.ControlChange,
                        Controller = MPTKController.BankSelectMsb,
                        Value = midiChannel.bankNum,
                        Channel = midiChannel.channel
                    });
                    
                    // Program change is handled automatically by setting PresetNum
                }
            }
        }
        
        private void SetupNodeChannelMapping()
        {
            if (runtimeGraph == null) return;
            
            // Map nodes to MIDI channels based on their properties
            var nodes = runtimeGraph.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                // Assign channels cyclically, avoiding channel 9 (drums) unless specifically requested
                int channelIndex = i % (midiChannels.Count - 1);
                if (channelIndex >= 9) channelIndex++; // Skip drum channel
                
                nodeToChannelMapping[node.id] = channelIndex;
            }
        }
        
        
        
        
        private void PlayNodeNote(SpriteNode.NodeData node)
        {
            if (!nodeToChannelMapping.TryGetValue(node.id, out int channelIndex)) return;
            if (channelIndex >= midiChannels.Count) return;
            
            var midiChannel = midiChannels[channelIndex];
            if (midiChannel.mute) return;
            
            // Play note based on node configuration
            var noteEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = Mathf.Clamp(node.note, 0, 127), // Use node's note value
                Duration = Mathf.RoundToInt(node.duration * 1000), // Convert to milliseconds
                Channel = midiChannel.channel,
                Velocity = Mathf.RoundToInt(node.velocity ) // Convert 0-1 to 0-127
            };
            
            midiPlayer.MPTK_PlayDirectEvent(noteEvent);
        }
        
        public void OnLoopCompleted()
        {
            Debug.Log("MIDI Loop completed");
            currentBeat = -1;
            OnMidiLoopCompleted?.Invoke();
        }
        
        public void PlayNoteOnChannel(int note, int channel, float duration = 0.5f, float velocity = 0.8f)
        {
            if (midiPlayer == null) return;
            if (channel < 0 || channel >= midiChannels.Count) return;
            
            var midiChannel = midiChannels[channel];
            if (midiChannel.mute) return;
            
            var noteEvent = new MPTKEvent()
            {
                Command = MPTKCommand.NoteOn,
                Value = Mathf.Clamp(note, 0, 127),
                Duration = Mathf.RoundToInt(duration * 1000),
                Channel = midiChannel.channel,
                Velocity = Mathf.RoundToInt(velocity )
            };
            
            midiPlayer.MPTK_PlayDirectEvent(noteEvent);
        }
        
        public void SetChannelInstrument(int channelIndex, int bankNum, int presetNum)
        {
            if (channelIndex < 0 || channelIndex >= midiChannels.Count) return;
            
            var midiChannel = midiChannels[channelIndex];
            midiChannel.bankNum = bankNum;
            midiChannel.presetNum = presetNum;
            
            // Update the actual MIDI channel
            if (midiPlayer != null)
            {
                var channel = midiPlayer.Channels[midiChannel.channel];
                channel.BankNum = bankNum;
                channel.PresetNum = presetNum;
                
                midiPlayer.MPTK_PlayEvent(new MPTKEvent()
                {
                    Command = MPTKCommand.ControlChange,
                    Controller = MPTKController.BankSelectMsb,
                    Value = bankNum,
                    Channel = midiChannel.channel
                });
                
                // Program change is handled automatically by setting PresetNum on the channel
            }
        }
        
        public void SetChannelVolume(int channelIndex, float volume)
        {
            if (channelIndex < 0 || channelIndex >= midiChannels.Count) return;
            
            var midiChannel = midiChannels[channelIndex];
            midiChannel.volume = Mathf.Clamp01(volume);
            
            if (midiPlayer != null)
            {
                midiPlayer.Channels[midiChannel.channel].Volume = volume;
            }
        }
        
        public void SetChannelMute(int channelIndex, bool mute)
        {
            if (channelIndex < 0 || channelIndex >= midiChannels.Count) return;
            
            midiChannels[channelIndex].mute = mute;
        }
        
        public void RefreshInstrumentList()
        {
            availableInstruments.List.Clear();
            
            ImSoundFont sfont = MidiPlayerGlobal.ImSFCurrent;
            if (sfont?.Banks == null) return;
            
            for (int i = 0; i < sfont.Banks.Length; i++)
            {
                var bank = sfont.Banks[i];
                if (bank?.defpresets == null) continue;
                
                for (int j = 0; j < bank.defpresets.Length; j++)
                {
                    var preset = bank.defpresets[j];
                    if (preset == null) continue;
                    
                    var instrument = new Instrument
                    {
                        BankNum = i,
                        PresetNum = j,
                        Name = preset.Name,
                        BoundChannel = 0 // Default channel
                    };
                    
                    availableInstruments.List.Add(instrument);
                }
            }
            
            Debug.Log($"Refreshed instrument list: {availableInstruments.List.Count} instruments found");
        }
        
        // Integration points for SpriteRuntimeGraph
        public void OnNodeActivated(SpriteNode.NodeData nodeData)
        {
            if (midiPlayer == null) return;
            
            // Find the MIDI channel that matches the node's channel setting
            int channelIndex = nodeData.channel;
            
            // If no matching channel found, use default channel 0
            if (channelIndex == -1 && midiChannels.Count > 0)
            {
                channelIndex = 0;
            }
            
            if (channelIndex >= 0)
            {
                PlayNoteOnChannel(nodeData.note, channelIndex, nodeData.duration, nodeData.velocity);
            }
        }
        
        public void OnTravelerArrivedAtNode(string nodeId, SpriteNode.NodeData nodeData)
        {
            OnNodeActivated(nodeData);
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

            availableInstruments = list;
        }
        
        
        // Public properties for external access
        public List<MidiChannel> MidiChannels => midiChannels;
        public InstrumentList AvailableInstruments => availableInstruments;
        public SpriteRuntimeGraph RuntimeGraph => runtimeGraph;
    }
}