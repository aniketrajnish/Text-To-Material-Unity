using UnityEngine;
using UnityEditor;

namespace T2M {

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
      : base("Project/T2M", SettingsScope.Project) {}

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
      => new T2MSettingsProvider();
}

} // namespace T2M
