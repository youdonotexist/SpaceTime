using UnityEditor;
using UnityEngine;

namespace UE.Script.Grid.Editor
{
    [CustomEditor(typeof(AttackGrid))]
    [CanEditMultipleObjects]
    public class AttackGridEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {

            serializedObject.Update();

            if (GUILayout.Button("Rebuild Grid"))
            {
                ((AttackGrid)target).BuildGrid();
            }
            

            // Save the changes back to the object
            EditorUtility.SetDirty(target);

            serializedObject.ApplyModifiedProperties();
            
            base.OnInspectorGUI();
        }
    }
}