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
        private const string PackageName = "AppsFlyer Wrapper";
        
        [MenuItem("Tools/Omnilatent/AppsFlyer/Import essential files")]
#if !OMNILATENT_APPSFLYER_WRAPPER
        [InitializeOnLoadMethod]
#endif
        public static void ShowInstallWindow()
        {
            InitialSetup wnd = GetWindow<InitialSetup>();
            wnd.maxSize = new Vector2(670f, 280f);
            wnd.minSize = new Vector2(500f, 280f);
            wnd.titleContent = new GUIContent($"{PackageName} Initial Setup");
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

            Button button = new Button();
            button.style.height = 80;
            button.style.marginTop = new StyleLength(StyleKeyword.Auto);
            button.style.marginBottom = 10;
            button.name = "button";
            button.text = $"Import {PackageName}'s essential files";
            button.clicked += OnInstall;
            root.Add(button);
        }

        private void OnInstall()
        {
            ImportRequiredFiles();
            InitScriptingDefineSymbol();
        }
        
        public static void ImportRequiredFiles()
        {
            string path = GetPackagePath("Assets/Omnilatent/AppsFlyerWrapper/AppsFlyerConnectorAsmdef.unitypackage", "AppsFlyerConnectorAsmdef");
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