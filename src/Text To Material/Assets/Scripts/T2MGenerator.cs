using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static T2M.T2MWindow;

namespace T2M
{
    public static class T2MGenerator
    {
        public static string GetSelectedGPTModel(GPTModelType gptModel)
        {
            return gptModel switch
            {
                GPTModelType.GPT4 => "gpt-4",
                GPTModelType.GPT3_5TURBO => "gpt-3.5-turbo",
                _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
            };
        }

        // Method to get the selected Dalle model as a string
        public static string GetSelectedDalleModel(DalleModelType dalleModel)
        {
            return dalleModel switch
            {
                DalleModelType.DALLE3 => "dall-e-3",
                DalleModelType.DALLE2 => "dall-e-2",
                _ => throw new ArgumentOutOfRangeException(nameof(dalleModel), dalleModel, null)
            };
        }

        // Method to get the selected image size as a string
        public static string GetSelectedSize(DalleModelType dalleModel, Dalle3ImageSize dalle3Size, Dalle2ImageSize dalle2Size)
        {
            return dalleModel switch
            {
                DalleModelType.DALLE3 => dalle3Size.ToString().Substring("SIZE_".Length),
                DalleModelType.DALLE2 => dalle2Size.ToString().Substring("SIZE_".Length),
                _ => throw new ArgumentOutOfRangeException(nameof(dalleModel), dalleModel, null)
            };
        }

        // Method to get the selected image quality as a string
        public static string GetSelectedQuality(ImageQuality quality)
        {
            return quality switch
            {
                ImageQuality.HD => "hd",
                ImageQuality.STANDARD => "standard",
                _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
            };
        }
        public static void GenerateMaterial(CurrentSettings settings, string matPrompt, string texPrompt, string generatedMaterialPath)
        {
            if (!System.IO.Directory.Exists(generatedMaterialPath))            
                AssetDatabase.CreateFolder(Path.GetDirectoryName(generatedMaterialPath), Path.GetFileName(generatedMaterialPath));
            
            // Assuming InvokeChat and InvokeImage methods are updated to accept model and image settings
            string materialProperties = OpenAIUtil.InvokeChat(matPrompt, GetSelectedGPTModel(settings.GPT_MODEL));
            string textureUrl = OpenAIUtil.InvokeImage(texPrompt, GetSelectedDalleModel(settings.DALLE_MODEL), GetSelectedSize(settings.DALLE_MODEL, settings.DALLE3_SIZE, settings.DALLE2_SIZE), GetSelectedQuality(settings.QUALITY));

            // Download texture from URL (you need to implement this method)
            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadTextureFromUrl(textureUrl, (Texture2D texture) =>
            {
                string dateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                if (texture == null)
                {
                    Debug.LogError("Failed to download texture.");
                    return;
                }
                byte[] pngData = texture.EncodeToPNG();
                string path = $"Assets/T2M Materials/T2M_Tex_{dateTime}.png";
                if (pngData != null)
                {
                    // Write to a file in the project folder
                    File.WriteAllBytes(path, pngData);
                    AssetDatabase.ImportAsset(path); // Refresh the AssetDatabase to show the new asset
                }

                // Apply texture to a new material
                Material newMaterial = new Material(Shader.Find("Standard"));
                

                // Save the generated material
                AssetDatabase.CreateAsset(newMaterial, $"{generatedMaterialPath}/T2M_Mat_{dateTime}.mat");
                newMaterial.SetTexture("_MainTex", texture);
                AssetDatabase.Refresh();
            }));
        }

        public static IEnumerator DownloadTextureFromUrl(string url, System.Action<Texture2D> onCompleted)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                // Send the request
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error while downloading the texture: " + uwr.error);
                    onCompleted?.Invoke(null);
                }
                else
                {
                    Debug.Log("Downloaded Texture!");
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    onCompleted?.Invoke(texture);
                }
            }
        }
    }
}
