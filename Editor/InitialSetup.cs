using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Omnilatent.AppsFlyerWrapperNS.EditorNS
{
    public class InitialSetup : EditorWindow
    {
        private static InitialSetup _instance;
        private const string PackageName = "AppsFlyer Wrapper";

        #if !OMNILATENT_APPSFLYER_WRAPPER
        [UnityEditor.Callbacks.DidReloadScripts]
        #endif
        private static void ShowInstallWindowWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ShowInstallWindowWhenReady;
                return;
            }

            EditorApplication.delayCall += ShowInstallWindow;
        }

        [MenuItem("Tools/Omnilatent/AppsFlyer/Import essential files")]
        public static void ShowInstallWindow()
        {
            if (_instance == null)
            {
                _instance = GetWindow<InitialSetup>();
                _instance.maxSize = new Vector2(670f, 280f);
                _instance.minSize = new Vector2(500f, 280f);
                _instance.titleContent = new GUIContent($"{PackageName} Initial Setup");
            }
            else
            {
                _instance.Focus();
            }
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            Label label = new Label($"Click on the following button to import files needed for {PackageName} to work properly.");
            label.style.marginTop = label.style.marginBottom = 20;
            label.style.marginLeft = label.style.marginRight = 20;
            label.style.alignSelf = new StyleEnum<Align>(Align.Center);
            label.style.whiteSpace = WhiteSpace.Normal;
            root.Add(label);

            Button button = MakeButton($"1. Initialize Scripting define symbol", InitScriptingDefineSymbol);
            button.style.height = 80;
            root.Add(button);
            
            Button button2 = MakeButton($"2. (Optional) Import AppsFlyer Purchase Connector Assembly definition file", ImportRequiredFiles);
            button2.style.height = 50;
            root.Add(button2);

            Button MakeButton(string text, Action onClick)
            {
                Button button = new Button();
                button.style.marginTop = new StyleLength(StyleKeyword.Auto);
                button.style.marginBottom = 10;
                button.name = "button";
                button.text = text;
                button.clicked += onClick;
                return button;
            }
        }

        private void OnInstall()
        {
            ImportRequiredFiles(); //as of Appsflyer 6.14.3, this is not required anymore.
            // InitScriptingDefineSymbol();
        }

        public static void ImportRequiredFiles()
        {
            string path = GetPackagePath("Assets/Omnilatent/AppsFlyerWrapper/AppsFlyerConnectorAsmdef.unitypackage",
                "AppsFlyerConnectorAsmdef");
            AssetDatabase.ImportPackage(path, true);
        }

        static string GetPackagePath(string path, string filename)
        {
            if (!File.Exists($"{Application.dataPath}/../{path}"))
            {
                Debug.Log($"{filename} not found at {path}, attempting to search whole project for {filename}");
                string[] guids = AssetDatabase.FindAssets($"{filename} l:package", new string[] { "Assets", "Packages" });
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
                else
                {
                    Debug.LogError($"{filename} not found at {Application.dataPath}/../{path}");
                    return null;
                }
            }

            return path;
        }

        const string SYMBOL = "OMNILATENT_APPSFLYER_WRAPPER";

        public static void InitScriptingDefineSymbol()
        {
            #if !OMNILATENT_APPSFLYER_WRAPPER
            // Get current defines
            string defineSymbolString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            // Split at ;
            List<string> symbols = defineSymbolString.Split(';').ToList();
            // check if defines already exist given define
            if (!symbols.Contains(SYMBOL))
            {
                // if not add it at the end with a leading ; separator
                defineSymbolString += $";{SYMBOL}";

                // write the new defines back to the PlayerSettings
                // This will cause a recompilation of your scripts
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defineSymbolString);

                Debug.Log($"Scripting Define Symbol '{SYMBOL}' was added.");
            }
            #endif
        }
    }
}