#if UNITY_IOS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEditor.iOS.Xcode;

public class PostProcessIOS
{
    [PostProcessBuild]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            string target = proj.TargetGuidByName("Unity-iPhone");

            // Enable BitCode
            //proj.SetBuildProperty(target, "ENABLE_BITCODE", "false");

            // Firebase configuration
            // Debug.Log("[Firebase] Adding frameworks to Xcode...");
            // proj.AddFrameworkToProject(target, "SafariServices.framework", false);

            File.WriteAllText(projPath, proj.WriteToString());

            // Get plist
            string plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            var rootDict = plist.root;

            // Fix "Missing Compliance" Apple Connect warning
            var buildKey = "ITSAppUsesNonExemptEncryption";
            rootDict.SetString(buildKey, "false");

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
}
#endif