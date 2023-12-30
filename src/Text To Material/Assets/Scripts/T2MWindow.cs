using UnityEngine;
using UnityEditor;
using System.Collections;
using T2M;
using System;

namespace T2M
{
    public class T2MWindow : EditorWindow
    {
        [SerializeField] Texture2D logo;

        string materialPrompt = "Describe the material";
        string texturePrompt = "Describe the texture";
        string generatedMaterialPath = "Assets/T2M Materials";
        
        public struct CurrentSettings
        {
            public GPTModelType GPT_MODEL;
            public DalleModelType DALLE_MODEL;
            public Dalle3ImageSize DALLE3_SIZE;
            public Dalle2ImageSize DALLE2_SIZE;
            public ImageQuality QUALITY;
        }

        CurrentSettings settings = new CurrentSettings
        {
            GPT_MODEL = GPTModelType.GPT3_5TURBO,
            DALLE_MODEL = DalleModelType.DALLE2,
            DALLE3_SIZE = Dalle3ImageSize.SIZE_1024x1024,
            DALLE2_SIZE = Dalle2ImageSize.SIZE_256x256,
            QUALITY = ImageQuality.STANDARD
        };
        
        // Add menu named "Material Generator" to the Window menu
        [MenuItem("Assets/Create/T2M")]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(T2MWindow));
        }
        private void OnEnable()
        {
            titleContent = new GUIContent("T2MWindow", logo);
        }
        void OnGUI()
        {
            GUILayout.Label("Generate Material from Text Prompts", EditorStyles.boldLabel);

            // Material Prompt
            GUILayout.Label("Material Prompt", EditorStyles.boldLabel);
            materialPrompt = EditorGUILayout.TextArea(materialPrompt, GUILayout.Height(75), GUILayout.Width(300));
            GUILayout.Label("Model", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (EditorGUILayout.ToggleLeft("gpt-3.5-turbo", settings.GPT_MODEL == GPTModelType.GPT3_5TURBO, GUILayout.Width(150)))
                settings.GPT_MODEL = GPTModelType.GPT3_5TURBO;

            if (EditorGUILayout.ToggleLeft("gpt-4", settings.GPT_MODEL == GPTModelType.GPT4, GUILayout.Width(150)))            
                settings.GPT_MODEL = GPTModelType.GPT4;   

            EditorGUILayout.EndHorizontal();

            // Texture Prompt
            GUILayout.Label("Texture Prompt", EditorStyles.boldLabel);
            texturePrompt = EditorGUILayout.TextArea(texturePrompt, GUILayout.Height(75), GUILayout.Width(300));
            GUILayout.Label("Model", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.ToggleLeft("dall-e-2", settings.DALLE_MODEL == DalleModelType.DALLE2, GUILayout.Width(150)))            
                settings.DALLE_MODEL = DalleModelType.DALLE2;
            
            if (EditorGUILayout.ToggleLeft("dall-e-3", settings.DALLE_MODEL == DalleModelType.DALLE3, GUILayout.Width(150)))            
                settings.DALLE_MODEL = DalleModelType.DALLE3;
            
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Model Options", EditorStyles.boldLabel);

            if (settings.DALLE_MODEL == DalleModelType.DALLE3)
            {
                settings.DALLE3_SIZE = (Dalle3ImageSize)EditorGUILayout.EnumPopup("Image Size", settings.DALLE3_SIZE, GUILayout.Width(300));
                settings.QUALITY = (ImageQuality)EditorGUILayout.EnumPopup("Image Quality", settings.QUALITY, GUILayout.Width(300));
            }
            else if (settings.DALLE_MODEL == DalleModelType.DALLE2)            
                settings.DALLE2_SIZE = (Dalle2ImageSize)EditorGUILayout.EnumPopup("Image Size", settings.DALLE2_SIZE, GUILayout.Width(300));
            

            if (GUILayout.Button("Generate", GUILayout.Width(120)))
            {
                T2MGenerator.GenerateMaterial(settings, materialPrompt, texturePrompt, generatedMaterialPath);
            }
        } 
    }
}
