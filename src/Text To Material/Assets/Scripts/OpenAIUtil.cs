using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Drawing;

namespace T2M
{
    static class OpenAIUtil
    {
        static string CreateImgRequestBody(string prompt, string model = "dall-e-2", string size = "1024x1024", string quality = "standard", int n = 1)
        {
            var req = new OpenAI.imgRequest
            {
                model = model,
                prompt = prompt,
                size = size,
                quality = quality,
                n = n
            };

            return JsonUtility.ToJson(req);
        }
        public static string InvokeImage(string prompt, string model = "dall-e-2", string size = "1024x1024", string quality = "standard", int n = 1)
        {           
            using var post = UnityWebRequest.Post
             (OpenAI.Api.imgUrl, CreateImgRequestBody(prompt, model, size, quality, n), "application/json");
            post.SetRequestHeader("Authorization", "Bearer " + T2MSettings.instance.apiKey);

            // Request start
            var response = post.SendWebRequest();

            // Progress bar
            for (var progress = 0.0f; !response.isDone; progress += 0.01f)
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
                var _data = JsonUtility.FromJson<OpenAI.imgResponse>(json);
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
            var msg = new OpenAI.RequestMessage();
            msg.role = "user";
            msg.content = prompt;

            var req = new OpenAI.Request();
            req.model = model;
            req.messages = new[] { msg };

            return JsonUtility.ToJson(req);
        }

        public static string InvokeChat(string prompt, string model = "gpt-3.5-turbo")
        {
            // POST
            using var post = UnityWebRequest.Post
              (OpenAI.Api.Url, CreateChatRequestBody(prompt, model), "application/json");

            // API key authorization
            post.SetRequestHeader
              ("Authorization", "Bearer " + T2MSettings.instance.apiKey);

            // Request start
            var req = post.SendWebRequest();

            // Progress bar (Totally fake! Don't try this at home.)
            for (var progress = 0.0f; !req.isDone; progress += 0.01f)
            {
                EditorUtility.DisplayProgressBar
                  ("T2M", "Generating...", progress);
                System.Threading.Thread.Sleep(100);
                progress += 0.01f;
            }
            EditorUtility.ClearProgressBar();

            // Response extraction
            var json = post.downloadHandler.text;
            var data = JsonUtility.FromJson<OpenAI.Response>(json);
            return data.choices[0].message.content;
        }

    }

} 
