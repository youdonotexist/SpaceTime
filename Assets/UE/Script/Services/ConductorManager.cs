using System.Collections.Generic;
using MidiPlayerTK;
using MPTK.NAudio.Midi;
using UE.Script.Utility.ServiceLocatorSample.ServiceLocator;
using UnityEngine;

namespace UE.Script
{
    public interface IConductorEventReceiver
    {
        void OnTick(float loopPositionInBeats, float songPositionInBeats, IMidiPlayer midiPlayer);
        void OnLoopCompleted();
    }

    public interface IMidiPlayer
    {
        void PlayEvent(MPTKEvent e);
    }

    [System.Serializable]
    public class BeatEvent
    {
        public BeatEvent()
        {
            
        }
        
        public BeatEvent(float beat, int[] midiNotes, string tool, int channel, float duration = 200)
        {
            Beat = beat;
            MidiNotes = midiNotes;
            Channel = channel;
            Duration = duration;
            Tool = tool;
            BeatWasHit = false;
        }

        public float Beat;
        public int[] MidiNotes;
        public int Channel;
        public float Duration;
        public string Tool;
        public bool BeatWasHit;
    }

    /**
     * The conductor manager is managing the entire Battle. 
     */
    public class ConductorManager : MonoBehaviour, IGameService, IMidiPlayer
    {
        private bool _started;
        
        //the current position of the song (in seconds)
        private float _songPosition;

        //the current position of the song (in beats)
        private float _songPositionInBeats;

        public float SongPositionInBeats => _songPositionInBeats;

        //Song 
        private float _songPositionInAnalog;

        public float SongPositionInAnalog => _songPositionInAnalog;

        //the duration of a beat
        private float _secPerBeat;

        //how much time (in seconds) has passed since the song started
        private float _dspTimeSong;

        //the total number of loops completed since the looping clip first started
        private int _completedLoops = 0;

        //The current position of the song within the loop in beats.
        private float _loopPositionInBeats;

        public float LoopPositionInBeats => _loopPositionInBeats;

        //beats per minute of a song
        // (60/(500,000e-6))*b/4, with b the lower numeral of the time signature
        private float _bpm = 128;

        //the number of beats in each loop
        private float _beatsPerSong = 4.0f;

        private float _pauseTime = 0.0f;
        private bool _isPaused = false;

        public static bool MainConductorPaused = false;
        
        
        public readonly List<IConductorEventReceiver> EventReceiver = new();
        
        public MidiStreamPlayer mainPlayer;

        private void Awake()
        {
            ServiceLocator.Current.Register(this);
            
            mainPlayer = GetComponentInChildren<MidiStreamPlayer>();
            mainPlayer.MPTK_InitSynth();
        }

        public void Initialize(float bpm, float beatsPerLoop)
        {
            _bpm = bpm;
            _beatsPerSong = beatsPerLoop;

            StartSong();
        }

        public float BeatsPerSong => _beatsPerSong;

        public void SetPaused(bool paused)
        {
            if (_started)
            {
                _isPaused = paused;
            }
        }

        void FixedUpdate()
        {
            if (_isPaused)
            {
                _pauseTime += Time.fixedDeltaTime;
                return;
            }
            
            if (_started && !_isPaused)
            {
                //calculate the position in seconds
                _songPosition = (float)(AudioSettings.dspTime - _dspTimeSong - _pauseTime);

                //calculate the position in beats
                //determine how many beats since the song started
                _songPositionInBeats = _songPosition / _secPerBeat;

                //calculate where we are for position, normalized
                _songPositionInAnalog = _loopPositionInBeats / _beatsPerSong;

                _loopPositionInBeats = _songPositionInBeats - (_completedLoops * _beatsPerSong);

                if (EventReceiver != null)
                {
                    EventReceiver.ForEach(receiver => receiver.OnTick(_loopPositionInBeats, _songPositionInBeats, this));
                }

                if (_songPositionInBeats >= (_completedLoops + 1) * _beatsPerSong)
                {
                    _completedLoops++;
                    if (EventReceiver != null)
                    {
                        EventReceiver.ForEach(receiver => receiver.OnLoopCompleted());
                    }
                }
            }
        }
        
        

        private void StartSong()
        {
            //calculate how many seconds is one beat
            //we will see the declaration of bpm later
            _secPerBeat = 60f / _bpm;

            //record the time when the song starts
            _dspTimeSong = (float)AudioSettings.dspTime;

            _started = true;
            
            //start the song
            GameObject.Find("GlobalMidiStream").GetComponent<AudioSource>().PlayScheduled(_dspTimeSong);

        }

        public float NormalizedPositionInLoop(float beatPosition)
        {
            return beatPosition / _beatsPerSong;
        }

        public long BeatsToSeconds(float beats)
        {
            // _songPositionInBeats = _songPosition / _secPerBeat;
            // _songPosition = _songPositionInBeats * _secPerBeats

            return (long) (beats * _secPerBeat);
        }

        public void PlayEvent(MPTKEvent e)
        {
            mainPlayer.MPTK_PlayDirectEvent(e);
        }
    }
}