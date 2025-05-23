﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using MPTK.NAudio.Midi;

namespace DemoMVP
{
    /// <summary>@brief
    /// Minimum Viable Product focus on the essentials of a Maestro function. 
    /// Only a few functions are presented. Links to the documentation are provided for further exploration.
    /// Therefore, error tests are absent, the user interface is almost non-existent and manipulations in Unity are reduced. 
    /// 
    /// The goal is rather to learn how to use the Maestro API and then progress by building more complex applications.
    /// Maestro is based on the use of prefabs (MidiFilePlayer, MidiStreamPlayer, …) which must be added in the Unity editor in the hierarchy of your project.
    /// In these demos, we prefer to create the prefabs by script to avoid manipulations in the editor. 
    /// It is rather recommended to create Prefabs in Unity Editor to take advantage of the Inspectors and its many directly accessible parameters.
    /// 
    /// Demonstration:      Play random note with random instrument when the space key is pressed, stop the note when the key is released.
    /// Implementation:     Add an empty gameobject in your Unity Scene then add this script to this gameobject.
    /// Running and using:  Play and stroke the space key, a random note will be played. Release the key, the note is stopped.
    /// 
    /// </summary>
    public class TheSimplestRandomPlayer : MonoBehaviour
    {

        public int StartNote = 62;
        public int EndNote = 72;

        // This class is able to play MIDI event: play note, play chord, patch change, apply effect, ... see doc!
        // https://mptkapi.paxstellar.com/d9/d1e/class_midi_player_t_k_1_1_midi_stream_player.html
        private MidiStreamPlayer midiStreamPlayer;

        // Description of the MIDI event which will hold the description of the note to played and 
        // https://mptkapi.paxstellar.com/d9/d50/class_midi_player_t_k_1_1_m_p_t_k_event.html
        private MPTKEvent mptkEvent;

        private void Awake()
        {
            // Search for an existing prefab in the scene
            midiStreamPlayer = FindFirstObjectByType<MidiStreamPlayer>();
            if (midiStreamPlayer == null)
            {
                // All lines bellow are useless if the prefab is found on the Unity Scene.
                Debug.Log("No MidiStreamPlayer Prefab found in the current Scene Hierarchy.");
                Debug.Log("It will be created by script. For a more serious project, add it to the scene!");

                // Create an empty gameobject in the scene
                GameObject go = new GameObject("HoldsMaestroPrefab");

                // MidiPlayerGlobal load the SoundFont. It is a singleton, only one instance will be created. 
                go.AddComponent<MidiPlayerGlobal>();

                // Add a MidiStreamPlayer prefab.
                midiStreamPlayer = go.AddComponent<MidiStreamPlayer>();

                // *** Set essential parameters ***

                // Core player is using internal thread for a good musical rendering
                midiStreamPlayer.MPTK_CorePlayer = true;

                // Display log about the MIDI events played.
                // Enable Monospace font in the Unity log window for better display.
                midiStreamPlayer.MPTK_LogEvents = true;

                // If disabled, nothing is send to the MIDI synth!
                midiStreamPlayer.MPTK_DirectSendToPlayer = true;
            }
            Debug.Log("Use <Space> key to play a note.");
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                midiStreamPlayer.MPTK_Channels[0].PresetNum = Random.Range(0, 127);
                Debug.Log($"Instrument selected: {midiStreamPlayer.MPTK_Channels[0].PresetName}");

                // Build a MIDI event for playing a random note with an infinite duration 
                mptkEvent = new MPTKEvent() { Channel = 0, Value = Random.Range(StartNote, EndNote + 1), Duration = -1 };
                // Start playing note. Stopped when space key is released.
                midiStreamPlayer.MPTK_PlayEvent(mptkEvent);
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                // Stop playing our "Hello, World!" note C5
                midiStreamPlayer.MPTK_StopEvent(mptkEvent);
            }
        }
    }
}