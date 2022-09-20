using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class EditorTools : MonoBehaviour
{
    //Building your server common library and copying it to the Unity Editor
    
    [MenuItem("Neon/Update server library")]
    static void DoSomething()
    {
        //getting project root directory 
        string libProjectPath = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "Server",
            "CommonLib");
        //getting .csproj path
        string csProjPath = Path.Combine(libProjectPath, "CommonLib.csproj");
        //getting destination directory
        string targetPath = Path.Combine(Application.dataPath, "Libs");

        //if destination not exists, create it
        if (Directory.Exists(targetPath))
            Directory.Delete(targetPath, true);

        Directory.CreateDirectory(targetPath);

        //starting assembly publishing
        ProcessStartInfo info =
            new ProcessStartInfo("dotnet", $"publish \"{csProjPath}\" -o \"{targetPath}\" -c Release");
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        Debug.Log($"Publishing {csProjPath} to {targetPath}");
        var proc = Process.Start(info);
        proc.WaitForExit();
        while (!proc.StandardOutput.EndOfStream)
        {
            if (proc.ExitCode == 0)
                Debug.Log(proc.StandardOutput.ReadLine());
            else
                Debug.LogError(proc.StandardOutput.ReadLine());
        }

        //updating Libs directory
        AssetDatabase.IsValidFolder($"Assets/Libs");
    }
}
