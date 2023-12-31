#if UNITY_EDITOR
using UnityEditor;

namespace T2M
{
    /// <summary>
    /// ref0 - https://github.com/keijiro/AICommand/blob/main/Assets/Editor/AICommandSettings.cs
    /// This script is meant to hold and save the API key whose input field is exposed in the project settings.
    /// </summary>
    [FilePath("UserSettings/T2MSettings.asset",
              FilePathAttribute.Location.ProjectFolder)]
    public sealed class T2MSettings : ScriptableSingleton<T2MSettings>
    {
        public string apiKey = null;
        public void Save() => Save(true);
        void OnDisable() => Save();
    }

    sealed class T2MSettingsProvider : SettingsProvider
    {
        public T2MSettingsProvider()
          : base("Project/T2M", SettingsScope.Project) { }

        public override void OnGUI(string search)
        {
            var settings = T2MSettings.instance;
            var key = settings.apiKey;
            EditorGUI.BeginChangeCheck();
            key = EditorGUILayout.TextField("API Key", key);
            if (EditorGUI.EndChangeCheck())
            {
                settings.apiKey = key;
                settings.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateCustomSettingsProvider()
          => new T2MSettingsProvider(); // create a new custom settings provider
    }

} // namespace T2M
#endif