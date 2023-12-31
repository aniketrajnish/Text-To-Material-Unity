using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Networking;

namespace T2M
{
    /// <summary>
    /// ref0 - https://github.com/keijiro/AICommand/blob/main/Assets/Editor/OpenAIUtil.cs  
    /// Utility class for interacting with OpenAI's API by sending a POST request and processing the response. 
    /// </summary>
    static class OpenAIUtil
    {
        static string CreateImgRequestBody(string prompt, string model = "dall-e-2", string size = "256x256", string quality = "standard", int n = 1)
        {
            var req = new OpenAI.imgRequest // json request body for dalle
            {
                model = model,
                prompt = prompt,
                size = size,
                quality = quality,
                n = n
            };

            return JsonUtility.ToJson(req); // converted to json
        }
        public static string InvokeImage(string prompt, string model = "dall-e-2", string size = "256x256", string quality = "standard", int n = 1)
        {           
            using var post = UnityWebRequest.Post
             (OpenAI.Api.imgUrl, CreateImgRequestBody(prompt, model, size, quality, n), "application/json"); // sending post request
            post.SetRequestHeader("Authorization", "Bearer " + T2MSettings.instance.apiKey); // auth
            
            var response = post.SendWebRequest();
            
            for (var progress = 0.0f; !response.isDone; progress += 0.01f) // fake progress bar
            {
                EditorUtility.DisplayProgressBar("T2M", "Generating Textrue...", progress);
                System.Threading.Thread.Sleep(100);
                progress += 0.01f;
            }
            EditorUtility.ClearProgressBar();

            if (post.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("DALL-E Request failed: " + post.error);
                return null;
            }

            try
            {
                var json = post.downloadHandler.text;
                var _data = JsonUtility.FromJson<OpenAI.imgResponse>(json); // getting the json response and converting into our struct format
                return _data.data[0].url; 
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error parsing DALL-E response: " + ex.Message);
                return null;
            }
        }
        
        static string CreateChatRequestBody(string prompt, string model = "gpt-3.5-turbo")
        {
            var msg = new OpenAI.RequestMessage(); // json request body for chatgpt
            msg.role = "user";
            msg.content = prompt;

            var req = new OpenAI.Request();
            req.model = model;
            req.messages = new[] { msg };

            return JsonUtility.ToJson(req);
        }

        public static string InvokeChat(string prompt, string model = "gpt-3.5-turbo")
        {
            using var post = UnityWebRequest.Post
              (OpenAI.Api.Url, CreateChatRequestBody(prompt, model), "application/json");

            post.SetRequestHeader
              ("Authorization", "Bearer " + T2MSettings.instance.apiKey);

            var req = post.SendWebRequest();

            for (var progress = 0.0f; !req.isDone; progress += 0.01f)
            {
                EditorUtility.DisplayProgressBar
                  ("T2M", "Generating...", progress);
                System.Threading.Thread.Sleep(100);
                progress += 0.01f;
            }
            EditorUtility.ClearProgressBar();

            var json = post.downloadHandler.text;
            var data = JsonUtility.FromJson<OpenAI.Response>(json);
            return data.choices[0].message.content;
        }

    }

}
#endif