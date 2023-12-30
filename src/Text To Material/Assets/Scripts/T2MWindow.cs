using UnityEngine;
using UnityEditor;

namespace T2M
{
    /// <summary>
    /// Overriding the EditorWindow class to create a window with options for text prompts and options.
    /// The window is labeled to be opened as a Menu Item that can be accessed from the Create Menu.
    /// Add logo to the window
    /// </summary>
    public class T2MWindow : EditorWindow
    {
        [SerializeField] Texture2D logo;

        string materialPrompt = "Describe the material";
        string texturePrompt = "Describe the texture";
        string generatedMaterialPath = "Assets/T2M Materials";
        
        public struct CurrentSettings // to hold the current settings chose
        {
            public GPTModelType GPT_MODEL;
            public DalleModelType DALLE_MODEL;
            public Dalle3ImageSize DALLE3_SIZE;
            public Dalle2ImageSize DALLE2_SIZE;
            public ImageQuality QUALITY;
        }

        CurrentSettings settings = new CurrentSettings // intialize settings to default
        {
            GPT_MODEL = GPTModelType.GPT3_5TURBO,
            DALLE_MODEL = DalleModelType.DALLE2,
            DALLE3_SIZE = Dalle3ImageSize.SIZE_1024x1024,
            DALLE2_SIZE = Dalle2ImageSize.SIZE_256x256,
            QUALITY = ImageQuality.STANDARD
        };
        
        [MenuItem("Assets/Create/T2M")] 
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(T2MWindow)); // show existing window instance or create a window instance
        }
        private void OnEnable()
        {
            titleContent = new GUIContent("T2MWindow", logo); 
        }
        void OnGUI()
        {
            GUIStyle wrapTextStyle = new GUIStyle(EditorStyles.textArea); // to word wrap prompts
            wrapTextStyle.wordWrap = true;

            GUILayout.Label("Material Prompt", EditorStyles.boldLabel);
            materialPrompt = EditorGUILayout.TextArea(materialPrompt, wrapTextStyle, GUILayout.Height(75), GUILayout.Width(300));            

            GUILayout.Label("Model", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.ToggleLeft("gpt-3.5-turbo", settings.GPT_MODEL == GPTModelType.GPT3_5TURBO, GUILayout.Width(150))) // toggle buttons
                settings.GPT_MODEL = GPTModelType.GPT3_5TURBO;
            if (EditorGUILayout.ToggleLeft("gpt-4", settings.GPT_MODEL == GPTModelType.GPT4, GUILayout.Width(150)))            
                settings.GPT_MODEL = GPTModelType.GPT4;
            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Texture Prompt", EditorStyles.boldLabel);
            texturePrompt = EditorGUILayout.TextArea(texturePrompt, wrapTextStyle, GUILayout.Height(75), GUILayout.Width(300));

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
                T2MGenerator.GenerateMaterial(settings, materialPrompt, texturePrompt, generatedMaterialPath);            
        } 
    }
}
