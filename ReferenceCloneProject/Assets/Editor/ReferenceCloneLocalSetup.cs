#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ReferenceCloneLocalSetup
{
    private const string Define = "CLAW_REFERENCE_OBI";
    private const string ObiDllPath = "Assets/ReferenceOriginal/Plugins/Obi/Obi.dll";

    [MenuItem("ReferenceClone/Enable Local Obi")]
    public static void EnableLocalObi()
    {
        if (!File.Exists(ObiDllPath))
        {
            Debug.LogError(
                "ReferenceClone: local Obi assembly not found. Put your own reference copy at: " +
                ObiDllPath);
            return;
        }

        SetDefineEnabled(true);
        AssetDatabase.Refresh();
        Debug.Log("ReferenceClone: enabled CLAW_REFERENCE_OBI. Unity will recompile the canonical ClawRope layer.");
    }

    [MenuItem("ReferenceClone/Disable Local Obi")]
    public static void DisableLocalObi()
    {
        SetDefineEnabled(false);
        AssetDatabase.Refresh();
        Debug.Log("ReferenceClone: disabled CLAW_REFERENCE_OBI.");
    }

    private static void SetDefineEnabled(bool enabled)
    {
        BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (group == BuildTargetGroup.Unknown)
            group = BuildTargetGroup.Standalone;

        string current = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        string[] values = current
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct()
            .ToArray();

        if (enabled && !values.Contains(Define))
            values = values.Concat(new[] { Define }).ToArray();
        else if (!enabled)
            values = values.Where(v => v != Define).ToArray();

        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", values));
    }
}
#endif
