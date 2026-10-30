using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class StripPerformanceTestResources : IPreprocessBuildWithReport, IFilterBuildAssemblies
{
    public int callbackOrder => -10000;

    [MenuItem("KoalaFalls/Clean Android Build Cache")]
    public static void CleanBuildCache()
    {
        string[] folders =
        {
            "Library/Bee",
            "Library/BuildPlayerData",
            "Library/PlayerDataCache",
            "Library/TypeDb"
        };

        for (int i = 0; i < folders.Length; i++)
        {
            string folder = folders[i];
            if (!Directory.Exists(folder))
                continue;
            try
            {
                Directory.Delete(folder, true);
                Debug.Log("Deleted " + folder);
            }
            catch (IOException ex)
            {
                Debug.LogWarning("Could not delete " + folder + ": " + ex.Message);
            }
        }

        DeletePerfFiles();
        Debug.Log("Build cache cleaned. Use File > Build Profiles > Clean Build next.");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        DeletePerfFiles();
    }

    public string[] OnFilterAssemblies(BuildOptions options, string[] assemblies)
    {
        List<string> keep = new List<string>(assemblies.Length);
        for (int i = 0; i < assemblies.Length; i++)
        {
            string path = assemblies[i].Replace('\\', '/').ToLowerInvariant();
            if (IsTestAssembly(path))
                continue;
            keep.Add(assemblies[i]);
        }
        return keep.ToArray();
    }

    static bool IsTestAssembly(string path)
    {
        return path.Contains("nunit.framework")
            || path.Contains("/nunit.")
            || path.Contains("unityengine.testrunner")
            || path.Contains("unityeditor.testrunner")
            || path.Contains("unity.collections.tests")
            || path.Contains("/tests/")
            || path.Contains(".tests.dll")
            || path.Contains("performance.test")
            || path.Contains("system.io.hashing.dll");
    }

    static void DeletePerfFiles()
    {
        string[] files =
        {
            "Assets/Resources/PerformanceTestRunInfo.json",
            "Assets/Resources/PerformanceTestRunSettings.json"
        };

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            if (!File.Exists(file))
                continue;
            File.Delete(file);
            string meta = file + ".meta";
            if (File.Exists(meta))
                File.Delete(meta);
        }
    }
}
