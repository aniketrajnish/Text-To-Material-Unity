using System;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static T2M.T2MWindow;

namespace T2M
{
    /// <summary>
    /// This static class is meant to generate the materials.
    /// We parse a txt file containing the additional prompts and send it to the API.
    /// We set the material properties, generate texture, convert it to normal map.
    /// We then apply the generated texture map and normal map to the material.
    /// </summary>
    public static class T2MGenerator
    {
        // the static methods below map the user settings enum to the corresponding string
        // to be sent into the json request body
        public static string GetSelectedGPTModel(GPTModelType gptModel)
        {
            return gptModel switch
            {
                GPTModelType.GPT4 => "gpt-4-1106-preview",
                GPTModelType.GPT3_5TURBO => "gpt-3.5-turbo",
                _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
            };
        }

        public static string GetSelectedDalleModel(DalleModelType dalleModel)
        {
            return dalleModel switch
            {
                DalleModelType.DALLE3 => "dall-e-3",
                DalleModelType.DALLE2 => "dall-e-2",
                _ => throw new ArgumentOutOfRangeException(nameof(dalleModel), dalleModel, null)
            };
        }

        public static string GetSelectedSize(DalleModelType dalleModel, Dalle3ImageSize dalle3Size, Dalle2ImageSize dalle2Size)
        {
            return dalleModel switch
            {
                DalleModelType.DALLE3 => dalle3Size.ToString().Substring("SIZE_".Length),
                DalleModelType.DALLE2 => dalle2Size.ToString().Substring("SIZE_".Length),
                _ => throw new ArgumentOutOfRangeException(nameof(dalleModel), dalleModel, null)
            };
        }

        public static string GetSelectedQuality(ImageQuality quality)
        {
            return quality switch
            {
                ImageQuality.HD => "hd",
                ImageQuality.STANDARD => "standard",
                _ => throw new ArgumentOutOfRangeException(nameof(quality), quality, null)
            };
        }
        // generate material based on texture and material prompt
        public static void GenerateMaterial(CurrentSettings settings, string matPrompt, string texPrompt, string generatedMaterialPath)
        {
            if (!Directory.Exists(generatedMaterialPath))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(generatedMaterialPath), Path.GetFileName(generatedMaterialPath));

            string specificMatPrompt = GetMaterialPrompt(matPrompt); // return response in a specified format to be parsed
            string specificTexPrompt = GetTexturePrompt(texPrompt); // return seamless textures

            string materialProperties = OpenAIUtil.InvokeChat(specificMatPrompt, GetSelectedGPTModel(settings.GPT_MODEL));   
            
            MaterialProperties materialValues = ParseMaterialProperties(materialProperties);

            string textureUrl = OpenAIUtil.InvokeImage(specificTexPrompt, GetSelectedDalleModel(settings.DALLE_MODEL), 
                GetSelectedSize(settings.DALLE_MODEL, settings.DALLE3_SIZE, settings.DALLE2_SIZE), GetSelectedQuality(settings.QUALITY));

            float normStrength = 20;

            EditorCoroutineUtility.StartCoroutineOwnerless(DownloadTextureFromUrl(textureUrl, (Texture2D texture, Texture2D normalMap) =>
            {
                string dateTime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"); // for unique material and texture names

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
                    importer.textureType = TextureImporterType.NormalMap; // convert texture to normal map, save and reimport
                    importer.SaveAndReimport();
                }

                Texture2D texAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath); // ref to the texture images saved for the material
                Texture2D normAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);

                Material newMaterial = new Material(Shader.Find("HDRP/Lit"));

                string matPath = $"{generatedMaterialPath}/T2M_Mat_{dateTime}.mat";
                AssetDatabase.CreateAsset(newMaterial, matPath);
                ApplyMaterialProperties(newMaterial, materialValues, texAsset, normAsset);
               
            }, normStrength));
        }
        // converting a text file to array of strings
        private static string[] ReadPromptsFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath); // each element of array is 1 line
                return lines;
            }
            else
            {
                Debug.LogError("File not found: " + filePath);
                return new string[0];
            }
        }

        public static string GetMaterialPrompt(string matPrompt)
        {
            string[] prompts = ReadPromptsFromFile(Path.Combine(Application.dataPath, "Scripts/T2M/prompts.txt")); 
            return string.Format(prompts.Length > 0 ? prompts[0] : "", matPrompt); // 1st line - mat prompt
        }

        public static string GetTexturePrompt(string texPrompt)
        {
            string[] prompts = ReadPromptsFromFile(Path.Combine(Application.dataPath, "Scripts/T2M/prompts.txt")); // 2nd line - tex prompt
            return string.Format(prompts.Length > 1 ? prompts[1] : "", texPrompt);
        }
        // download the image from the response url
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
        // convert texture to png to save
        private static void SaveTexture(Texture2D texture, string path)
        {
            byte[] pngData = texture.EncodeToPNG();
            if (pngData != null)
            {
                File.WriteAllBytes(path, pngData);
                AssetDatabase.ImportAsset(path); // reimport
            }
        }
        // convert the texture obtained downloaded from response to normal map (courtesy: chatgpt haha)
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
        // struct to hold material properties
        public struct MaterialProperties
        {
            public Color Albedo;
            public float Metallic;
            public float Smoothness;
            public Color Emission;
            public Vector2 Tiling;
            public Vector2 Offset;
        }
        // method to parse an return the properties of the material as the struct declared above
        public static MaterialProperties ParseMaterialProperties(string response)
        {
            MaterialProperties values = new MaterialProperties();

            string[] properties = response.Split(',');

            foreach (string property in properties)
            {
                string[] keyValue = property.Split(':');
                if (keyValue.Length != 2) continue;

                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();

                switch (key)
                {
                    case "Albedo":
                        string[] rgba1 = value.Split(' ');
                        if (rgba1.Length == 4 &&
                            float.TryParse(rgba1[0], out float r1) &&
                            float.TryParse(rgba1[1], out float g1) &&
                            float.TryParse(rgba1[2], out float b1) &&
                            float.TryParse(rgba1[3], out float a1))
                        {
                            values.Albedo = new Color(r1, g1, b1, a1);
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
                    case "Emission":
                        string[] rgba2 = value.Split(' ');
                        if (rgba2.Length == 4 &&
                            float.TryParse(rgba2[0], out float r2) &&
                            float.TryParse(rgba2[1], out float g2) &&
                            float.TryParse(rgba2[2], out float b2) &&
                            float.TryParse(rgba2[3], out float a2))
                        {
                            values.Emission = new Color(r2, g2, b2, a2);
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
                }
            }

            return values;
        }
        // applying material and texture properties obtained
        private static void ApplyMaterialProperties(Material material, MaterialProperties values, Texture2D texture, Texture2D normalMap)
        {
            material.color = values.Albedo;
            material.SetFloat("_Metallic", values.Metallic);
            material.SetFloat("_Smoothness", values.Smoothness);

            material.SetTextureScale("_BaseColorMap", values.Tiling);
            material.SetTextureOffset("_BaseColorMap", values.Offset);

            material.SetTexture("_BaseColorMap", texture);
            if (normalMap != null)
            {
                material.SetTexture("_NormalMap", normalMap);
            }

            if (values.Emission != default(Color))
            {
                material.SetColor("_EmissiveColor", values.Emission);
                material.EnableKeyword("_UseEmissiveIntensity");
            }
        }
    }
}
#endif