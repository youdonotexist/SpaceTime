using System;
using System.Collections.Generic;
using System.Linq;
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
            // Load instruments from JSON file
            LoadInstrumentsFromJson();
            
            // Setup MIDI channels from loaded instruments
            if (midiChannels.Count == 0)
            {
                CreateMidiChannelsFromInstruments();
            }
            
            // Initialize channel configurations
            //midiPlayer.OnEventSynthStarted.AddListener(SetupMidiChannels);
            SetupMidiChannels();
        }
        
        private void LoadInstrumentsFromJson()
        {
            try
            {
                string jsonPath = System.IO.Path.Combine(UnityEngine.Application.dataPath, "instruments.json");
                if (System.IO.File.Exists(jsonPath))
                {
                    string jsonContent = System.IO.File.ReadAllText(jsonPath);
                    InstrumentList instrumentList = UnityEngine.JsonUtility.FromJson<InstrumentList>(jsonContent);
                    
                    if (instrumentList != null && instrumentList.List != null)
                    {
                        // Filter to only use the 16 selected chiptune instruments
                        FilterToChiptuneInstruments(instrumentList);
                        UnityEngine.Debug.Log($"Filtered to {availableInstruments.List.Count} chiptune instruments from instruments.json for MIDI sequencer");
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning("instruments.json contains null data, using default instruments");
                        CreateDefaultInstruments();
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("instruments.json file not found, using default instruments");
                    CreateDefaultInstruments();
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to load instruments.json: {ex.Message}");
                CreateDefaultInstruments();
            }
        }

        private void FilterToChiptuneInstruments(InstrumentList fullInstrumentList)
        {
            // Define the 16 chiptune instruments we want to use (preset numbers from JSON)
            var selectedPresets = new Dictionary<int, (string Name, int Channel)>
            {
                { 1, ("100 Square", 0) },
                { 8, ("100 25% Pulse", 1) },
                { 60, ("048 Pulse 50%", 2) },
                { 9, ("100 12.5% Pulse", 3) },
                { 10, ("100 75% Pulse", 4) },
                { 11, ("100 PWM", 5) },
                { 12, ("100 Triangle", 6) },
                { 6, ("100 Saw Wave", 7) },
                { 13, ("100 Noise", 8) },
                { 4, ("100 Buzzy", 11) },
                { 14, ("100 Sub Bass", 12) },
                { 15, ("100 Lead", 13) },
                { 16, ("100 Arp", 14) },
                { 17, ("100 Pad", 15) }
            };

            // Drum sets (bank 128)
            var selectedDrumPresets = new System.Collections.Generic.Dictionary<int, (string Name, int Channel)>
            {
                { 0, ("Standard Drums", 9) },
                { 16, ("059 Drumkit", 10) }
            };

            availableInstruments = new InstrumentList();
            availableInstruments.List = new System.Collections.Generic.List<Instrument>();

            // Find and add matching instruments from the full list
            foreach (var instrument in fullInstrumentList.List)
            {
                // Check regular instruments (bank 0)
                if (instrument.BankNum == 0 && selectedPresets.ContainsKey(instrument.PresetNum))
                {
                    var selected = selectedPresets[instrument.PresetNum];
                    availableInstruments.List.Add(new Instrument
                    {
                        BankNum = instrument.BankNum,
                        PresetNum = instrument.PresetNum,
                        Name = selected.Name,
                        BoundChannel = selected.Channel
                    });
                }
                // Check drum sets (bank 128)
                else if (instrument.BankNum == 128 && selectedDrumPresets.ContainsKey(instrument.PresetNum))
                {
                    var selected = selectedDrumPresets[instrument.PresetNum];
                    availableInstruments.List.Add(new Instrument
                    {
                        BankNum = instrument.BankNum,
                        PresetNum = instrument.PresetNum,
                        Name = selected.Name,
                        BoundChannel = selected.Channel
                    });
                }
            }

            // If we couldn't find all instruments in the JSON, fall back to defaults
            if (availableInstruments.List.Count < 16)
            {
                UnityEngine.Debug.LogWarning($"Only found {availableInstruments.List.Count} of 16 expected chiptune instruments in JSON, using defaults");
                CreateDefaultInstruments();
            }
        }
        
        private void CreateDefaultInstruments()
        {
            // Create curated list of 16 chiptune instruments for MIDI channels 1-16
            availableInstruments.List = new System.Collections.Generic.List<Instrument>
            {
                // Required chiptune waveforms
                new Instrument { BankNum = 0, PresetNum = 1, Name = "100 Square", BoundChannel = 0 },
                new Instrument { BankNum = 0, PresetNum = 8, Name = "100 25% Pulse", BoundChannel = 1 },
                new Instrument { BankNum = 0, PresetNum = 60, Name = "048 Pulse 50%", BoundChannel = 2 },
                new Instrument { BankNum = 0, PresetNum = 9, Name = "100 12.5% Pulse", BoundChannel = 3 },
                new Instrument { BankNum = 0, PresetNum = 10, Name = "100 75% Pulse", BoundChannel = 4 },
                new Instrument { BankNum = 0, PresetNum = 11, Name = "100 PWM", BoundChannel = 5 }, // PWM for chorusy motion
                new Instrument { BankNum = 0, PresetNum = 12, Name = "100 Triangle", BoundChannel = 6 },
                new Instrument { BankNum = 0, PresetNum = 6, Name = "100 Saw Wave", BoundChannel = 7 },
                new Instrument { BankNum = 0, PresetNum = 13, Name = "100 Noise", BoundChannel = 8 },
                
                // Drum sets
                new Instrument { BankNum = 128, PresetNum = 0, Name = "Standard Drums", BoundChannel = 9 },
                new Instrument { BankNum = 128, PresetNum = 16, Name = "059 Drumkit", BoundChannel = 10 },
                
                // Additional chiptune-aesthetic instruments
                new Instrument { BankNum = 0, PresetNum = 4, Name = "100 Buzzy", BoundChannel = 11 },
                new Instrument { BankNum = 0, PresetNum = 14, Name = "100 Sub Bass", BoundChannel = 12 },
                new Instrument { BankNum = 0, PresetNum = 15, Name = "100 Lead", BoundChannel = 13 },
                new Instrument { BankNum = 0, PresetNum = 16, Name = "100 Arp", BoundChannel = 14 },
                new Instrument { BankNum = 0, PresetNum = 17, Name = "100 Pad", BoundChannel = 15 }
            };
        }
        
        private void CreateMidiChannelsFromInstruments()
        {
            foreach (var instrument in availableInstruments.List)
            {
                // Create a MIDI channel for each instrument
                var midiChannel = new MidiChannel 
                { 
                    channel = instrument.BoundChannel, 
                    bankNum = instrument.BankNum, 
                    presetNum = instrument.PresetNum,
                    volume = 1.0f,
                    mute = false
                };
                midiChannels.Add(midiChannel);
            }
            
            UnityEngine.Debug.Log($"Created {midiChannels.Count} MIDI channels from loaded instruments");
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
                Velocity = Mathf.RoundToInt(velocity)
            };
            
            midiPlayer.MPTK_PlayEvent(noteEvent);
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
        
        /// <summary>
        /// Debug method that plays a sequence of notes on each loaded instrument/channel
        /// </summary>
        public void DebugPlayAllInstruments()
        {
            if (midiPlayer == null)
            {
                Debug.LogError("MidiPlayer is null - cannot play debug notes");
                return;
            }

            StartCoroutine(DebugPlayAllInstrumentsCoroutine());
        }

        private System.Collections.IEnumerator DebugPlayAllInstrumentsCoroutine()
        {
            Debug.Log($"[DEBUG_LOG] Starting debug playback of all {midiChannels.Count} loaded instruments");

            // Define a sequence of notes to play (C major scale)
            int[] testNotes = { 60, 62, 64, 65, 67, 69, 71, 72 }; // C4 to C5
            float noteDuration = 1f;
            float noteVelocity = 0.8f;

            for (int channelIndex = 0; channelIndex < midiChannels.Count; channelIndex++)
            {
                var midiChannel = midiChannels[channelIndex];
                
                // Skip muted channels
                if (midiChannel.mute)
                {
                    Debug.Log($"[DEBUG_LOG] Skipping muted channel {channelIndex} (MIDI Channel {midiChannel.channel})");
                    continue;
                }

                // Find the corresponding instrument name
                string instrumentName = "Unknown";
                var instrument = availableInstruments.List.FirstOrDefault(inst => 
                    inst.BankNum == midiChannel.bankNum && 
                    inst.PresetNum == midiChannel.presetNum &&
                    inst.BoundChannel == midiChannel.channel);
                
                if (instrument != null)
                {
                    instrumentName = instrument.Name;
                }

                Debug.Log($"[DEBUG_LOG] Playing notes on channel {channelIndex}: {instrumentName} (Bank: {midiChannel.bankNum}, Preset: {midiChannel.presetNum}, MIDI Channel: {midiChannel.channel})");

                // Play each note in the sequence
                foreach (int note in testNotes)
                {
                    PlayNoteOnChannel(note, channelIndex, noteDuration, noteVelocity);
                    yield return new WaitForSeconds(noteDuration + 0.1f); // Small gap between notes
                }

                // Pause between instruments
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[DEBUG_LOG] Finished debug playback of all instruments");
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