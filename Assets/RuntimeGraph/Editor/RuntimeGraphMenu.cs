#if UNITY_EDITOR
using System.IO;
using RuntimeGraph;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RuntimeGraphMenu
{
    private const string ScenesFolder = "Assets/Scenes";
    private const string DemoScenePath = ScenesFolder + "/RuntimeGraphDemo.unity";

    [MenuItem("Tools/Runtime Graph/Create Demo Scene", priority = 10)]
    public static void CreateDemoScene()
    {
        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "RuntimeGraphDemo";

        // Create Main Camera
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        camGO.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.11f, 0.12f, 0.13f);
        cam.orthographic = true;

        // Attach RuntimeGraphUI (UI Toolkit)
        if (camGO.GetComponent<RuntimeGraph.RuntimeGraphUI_Refactored>() == null)
        {
            camGO.AddComponent<RuntimeGraph.RuntimeGraphUI_Refactored>();
        }

        // Save the scene asset
        EditorSceneManager.SaveScene(scene, DemoScenePath);
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Runtime Graph", "Demo scene created at:\n" + DemoScenePath, "OK");
    }

    [MenuItem("Tools/Runtime Graph/Open Demo Scene", priority = 11)]
    public static void OpenDemoScene()
    {
        if (File.Exists(DemoScenePath))
        {
            EditorSceneManager.OpenScene(DemoScenePath);
        }
        else
        {
            if (EditorUtility.DisplayDialog("Runtime Graph", "Demo scene not found. Create it now?", "Create", "Cancel"))
            {
                CreateDemoScene();
            }
        }
    }
}
#endif