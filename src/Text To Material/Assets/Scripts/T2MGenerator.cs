using System;
using System.Collections;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static T2M.T2MGenerator;
using static T2M.T2MWindow;

namespace T2M
{
    public static class T2MGenerator
    {
        public static string GetSelectedGPTModel(GPTModelType gptModel)
        {
            return gptModel switch
            {
                GPTModelType.GPT4 => "gpt-4-1106-preview",
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
            if (!Directory.Exists(generatedMaterialPath))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(generatedMaterialPath), Path.GetFileName(generatedMaterialPath));

            string specificMatPrompt = $"Generate a Unity Standard Shader material with the following characteristics: {matPrompt}. " +
                "Please provide the material properties in the following format: Albedo: [color], Metallic: [value], Smoothness: [value], " +
                "Emission: [color]. For Tiling and Offset, provide two values separated by a space, in the format 'x y'. " +
                "Example format for Tiling and Offset: 'Tiling: 1 1', 'Offset: 0 0'." +
                "Please provide the material properties in the specified format without brackets." +
                "For example: Albedo: red, Metallic: 1, Smoothness: 1, Emission: red, Tiling: 10 10, Offset: 0 0";

            string specificTexPrompt = $"Create a high-quality, strictly seamless texture that can be tiled flawlessly for a 3D material, matching these characteristics: {texPrompt}. " +
                "The texture must have absolutely no visible seams or discontinuities, ensuring it can be tiled repeatedly without any noticeable edges. " +
                "It should also be evenly lit and high-resolution to support close-up views and detailed rendering without pixelation.";

            string materialProperties = OpenAIUtil.InvokeChat(specificMatPrompt, GetSelectedGPTModel(settings.GPT_MODEL));
            
            MaterialProperties materialValues = ParseMaterialProperties(materialProperties);
            string textureUrl = OpenAIUtil.InvokeImage(specificTexPrompt, GetSelectedDalleModel(settings.DALLE_MODEL), 
                GetSelectedSize(settings.DALLE_MODEL, settings.DALLE3_SIZE, settings.DALLE2_SIZE), GetSelectedQuality(settings.QUALITY));
            
            float normalMapStrength = 30.0f; // Adjust this value as needed

            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadTextureFromUrl(textureUrl, (Texture2D texture, Texture2D normalMap) =>
            {
                string dateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
                if (texture == null)
                {
                    Debug.LogError("Failed to download texture.");
                    return;
                }

                string texPath = $"Assets/T2M Materials/T2M_Tex_{dateTime}.png";
                SaveTexture(texture, texPath);

                string normPath = $"Assets/T2M Materials/T2M_Norm_{dateTime}.png";
                SaveTexture(normalMap, normPath);

                TextureImporter importer = AssetImporter.GetAtPath(normPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }


                // Load the textures from the asset database
                Texture2D texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                Texture2D normAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);


                // Apply texture to a new material
                Material newMaterial = new Material(Shader.Find("Standard"));

                string matPath = $"{generatedMaterialPath}/T2M_Mat_{dateTime}.mat";
                AssetDatabase.CreateAsset(newMaterial, matPath);
                AssetDatabase.ImportAsset(matPath);
                ApplyMaterialProperties(newMaterial, materialValues, texAsset, normAsset);
                /*EditorUtility.SetDirty(newMaterial);
                AssetDatabase.SaveAssetIfDirty(newMaterial);*/
                AssetDatabase.Refresh();
            }, normalMapStrength));
        }

        private static void SaveTexture(Texture2D texture, string path)
        {
            byte[] pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                AssetDatabase.ImportAsset(path); // Refresh the AssetDatabase to show the new asset
            }
        }

        private static void ApplyMaterialProperties(Material material, MaterialProperties values, Texture2D texture, Texture2D normalMap)
        {
            material.color = values.Albedo;
            material.SetFloat("_Metallic", values.Metallic);
            material.SetFloat("_Glossiness", values.Smoothness);

            
            material.SetTextureScale("_MainTex", values.Tiling);
            material.SetTextureOffset("_MainTex", values.Offset);
            material.SetTexture("_MainTex", texture);
            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.EnableKeyword("_NORMALMAP");
            }

            /*if (values.Emission != default(Color))
            {
                material.SetColor("_EmissionColor", values.Emission);
                material.EnableKeyword("_EMISSION");
            }*/

            // Additional properties like HeightMap and OcclusionMap can be set here if available
            EditorUtility.SetDirty(material);
        }

        public static IEnumerator DownloadTextureFromUrl(string url, System.Action<Texture2D, Texture2D> onCompleted, float normalStrength = 100.0f)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                // Send the request
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Error while downloading the texture: " + uwr.error);
                    onCompleted?.Invoke(null, null);
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    Texture2D normalMap = ConvertToNormalMap(texture, normalStrength);
                    onCompleted?.Invoke(texture, normalMap);
                }
            }
        }
        private static Texture2D ConvertToNormalMap(Texture2D sourceTexture, float strength)
        {
            Texture2D normalMap = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.ARGB32, true);

            for (int y = 0; y < normalMap.height; y++)
            {
                for (int x = 0; x < normalMap.width; x++)
                {
                    // Calculate the strength of the normal (you can adjust this value)
                    float xLeft = sourceTexture.GetPixel(x - 1 < 0 ? 0 : x - 1, y).grayscale * strength;
                    float xRight = sourceTexture.GetPixel(x + 1 == normalMap.width ? x : x + 1, y).grayscale * strength;
                    float yUp = sourceTexture.GetPixel(x, y - 1 < 0 ? 0 : y - 1).grayscale * strength;
                    float yDown = sourceTexture.GetPixel(x, y + 1 == normalMap.height ? y : y + 1).grayscale * strength;

                    // Calculate x and y gradients
                    float xGradient = (xLeft - xRight + 1) * 0.5f;
                    float yGradient = (yUp - yDown + 1) * 0.5f;

                    // Create normal
                    Vector3 normal = new Vector3(xGradient, yGradient, 1.0f).normalized;

                    // Store the normal in the texture
                    normalMap.SetPixel(x, y, new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1));
                }
            }

            normalMap.Apply();
            return normalMap;
        }
        public struct MaterialProperties
        {
            public Color Albedo;
            public float Metallic;
            public float Smoothness;
            /*public Texture2D NormalMap;
            public bool HasNormalMap;
            public Texture2D HeightMap;
            public bool HasHeightMap;
            public Texture2D OcclusionMap;
            public bool HasOcclusion;*/
            public Color Emission;
            public Vector2 Tiling;
            public Vector2 Offset;
        }

        public static MaterialProperties ParseMaterialProperties(string response)
        {
            MaterialProperties values = new MaterialProperties();

            // Split the response into lines if necessary, or however the data is structured
            string[] properties = response.Split(',');

            foreach (string property in properties)
            {
                string[] keyValue = property.Split(':');
                if (keyValue.Length != 2) continue; // Skip improperly formatted lines

                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();

                switch (key)
                {
                    case "Albedo":
                        Color albedoColor;
                        if (ColorUtility.TryParseHtmlString(value, out albedoColor))
                        {
                            values.Albedo = albedoColor;
                        }
                        break;
                    case "Metallic":
                        if (float.TryParse(value, out float metallic))
                        {
                            values.Metallic = metallic;
                        }
                        break;
                    case "Smoothness":
                        if (float.TryParse(value, out float smoothness))
                        {
                            values.Smoothness = smoothness;
                        }
                        break;
                    /*case "Normal Map":
                         values.HasNormalMap = value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                         // Assuming value contains the path or indication of the texture
                         values.NormalMap = LoadTexture(value, values.HasNormalMap);
                         break;
                     case "Height Map":
                         values.HasHeightMap = value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                         // Assuming value contains the path or indication of the texture
                         values.HeightMap = LoadTexture(value, values.HasHeightMap);
                         break;
                     case "Occlusion Map":
                         values.HasOcclusion = value.Equals("yes", StringComparison.OrdinalIgnoreCase);
                         // Assuming value contains the path or indication of the texture
                         values.OcclusionMap = LoadTexture(value, values.HasOcclusion);
                         break;*/
                    case "Emission":
                        Color emissionColor;
                        if (ColorUtility.TryParseHtmlString(value, out emissionColor))
                        {
                            values.Emission = emissionColor;
                        }
                        break;
                    case "Tiling":
                        string[] tilingValues = value.Split(' ');
                        if (tilingValues.Length == 2 &&
                            float.TryParse(tilingValues[0], out float tilingX) &&
                            float.TryParse(tilingValues[1], out float tilingY))
                        {
                            values.Tiling = new Vector2(tilingX, tilingY);
                        }
                        break;
                    case "Offset":
                        string[] offsetValues = value.Split(' ');
                        if (offsetValues.Length == 2 &&
                            float.TryParse(offsetValues[0], out float offsetX) &&
                            float.TryParse(offsetValues[1], out float offsetY))
                        {
                            values.Offset = new Vector2(offsetX, offsetY);
                        }
                        break;
                        // Add cases for other properties as needed
                }
            }

            return values;
        }

    }
}
