using UnityEngine;
using UnityEditor;
/*
[CustomEditor(typeof(UpgradeData))]
public class UpgradeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        UpgradeData data = (UpgradeData)target;

        // Nombre del upgrade
        data.upgradeName = EditorGUILayout.TextField("Upgrade Name", data.upgradeName);

        // Base Cost con sufijo
        data.baseCost = EditorGUILayout.DoubleField("Base Cost", data.baseCost);
        EditorGUILayout.LabelField("Formatted Base Cost:", GoldManager.FormatNumber(data.baseCost));

        // Cost Multiplier
        data.costMultiplier = EditorGUILayout.DoubleField("Cost Multiplier", data.costMultiplier);

        // OPS por nivel con sufijo
        data.goldPerSecondPerLevel = EditorGUILayout.DoubleField("OPS per Level", data.goldPerSecondPerLevel);
        EditorGUILayout.LabelField("Formatted OPS/Level:", GoldManager.FormatNumber(data.goldPerSecondPerLevel));

        // Nivel actual (readonly)
        EditorGUILayout.LabelField("Current Level", data.currentLevel.ToString());

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}
*/