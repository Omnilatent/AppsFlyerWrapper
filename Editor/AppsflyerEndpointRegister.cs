using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace Omnilatent.AppsFlyerWrapperNS.EditorNS
{
    public class AppsflyerEndpointRegister
    {
        [PostProcessBuildAttribute]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
#if UNITY_IOS
            if (target == BuildTarget.iOS)
            {
                string plistPath = pathToBuiltProject + "/Info.plist";
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                PlistElementDict rootDict = plist.root;
                rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com/");

                /*** To add more keys :
                 ** rootDict.SetString("<your key>", "<your value>");
                 ***/

                File.WriteAllText(plistPath, plist.WriteToString());

                Debug.Log("Info.plist updated with NSAdvertisingAttributionReportEndpoint");
            }
#endif
        }
    }
}