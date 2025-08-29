// 8/20/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using Commonwealth.Script.Ship.Monitors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class PrefabCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Ship Prefabs")]
    public static void CreateShipPrefabs()
    {
        string assetPath = "Assets/Commonwealth/Prefab/";
        
        // Step 1: Create ShipStatUIItem Prefab
        GameObject shipStatUIItem = new GameObject("ShipStatUIItem");
        RectTransform rectTransform = shipStatUIItem.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(280, 25);
        shipStatUIItem.AddComponent<ShipStatUIItem>();
        shipStatUIItem.AddComponent<ShipStatAnimator>();

        // Add child objects for UI elements
        GameObject background = new GameObject("Background");
        background.transform.SetParent(shipStatUIItem.transform);
        background.AddComponent<Image>();

        GameObject statusIndicator = new GameObject("StatusIndicator");
        statusIndicator.transform.SetParent(shipStatUIItem.transform);
        statusIndicator.AddComponent<Image>();

        // Save ShipStatUIItem as a prefab
        SavePrefab(shipStatUIItem, $"{assetPath}ShipStatUIItem.prefab");

        // Step 2: Create ShipStatsUI Prefab
        GameObject shipStatsUI = new GameObject("ShipStatsUI");
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene. Please create a Canvas first.");
            return;
        }
        shipStatsUI.transform.SetParent(canvas.transform);

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(shipStatsUI.transform);
        panel.AddComponent<Image>();

        GameObject header = new GameObject("Header");
        header.transform.SetParent(panel.transform);
        header.AddComponent<Text>();

        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform);
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        viewport.AddComponent<Mask>();
        viewport.AddComponent<Image>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();

        scrollRect.content = content.GetComponent<RectTransform>();

        GameObject toggleButton = new GameObject("ToggleButton");
        toggleButton.transform.SetParent(panel.transform);
        toggleButton.AddComponent<Button>();

        ShipStatsUI shipStatsUIScript = shipStatsUI.AddComponent<ShipStatsUI>();
        shipStatsUIScript.statItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{assetPath}ShipStatUIItem.prefab");

        // Save ShipStatsUI as a prefab
        SavePrefab(shipStatsUI, $"{assetPath}ShipStatsUI.prefab");

        // Step 3: Create ShipStatsManager Prefab
        GameObject shipStatsManager = new GameObject("ShipStatsManager");
        shipStatsManager.AddComponent<ShipStatsManager>();

        // Save ShipStatsManager as a prefab
        SavePrefab(shipStatsManager, $"{assetPath}ShipStatsManager.prefab");

        Debug.Log("Prefabs created successfully!");
    }

    private static void SavePrefab(GameObject gameObject, string path)
    {
        PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.UserAction);
        DestroyImmediate(gameObject);
    }
}
