using UnityEngine;
using UnityEditor;
using RuntimeGraph.Sprite;

namespace RuntimeGraph.Editor
{
    /// <summary>
    /// Custom editor for SpriteRuntimeGraphSequencer that adds a button to execute DebugPrintInstruments
    /// </summary>
    [CustomEditor(typeof(SpriteRuntimeGraphSequencer))]
    public class SpriteRuntimeGraphSequencerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector
            DrawDefaultInspector();
            
            // Add some space before our custom button
            GUILayout.Space(10);
            
            // Get reference to the target component
            SpriteRuntimeGraphSequencer sequencer = (SpriteRuntimeGraphSequencer)target;
            
            // Add the DebugPrintInstruments button
            if (GUILayout.Button("Debug Print Instruments"))
            {
                sequencer.DebugPrintInstruments();
            }
        }
    }
}