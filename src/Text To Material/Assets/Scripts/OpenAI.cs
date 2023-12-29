namespace T2M.OpenAI
{
    public static class Api
    {
        public const string Url = "https://api.openai.com/v1/chat/completions";
        public const string imgUrl = "https://api.openai.com/v1/images/generations";
    }

    [System.Serializable]
    public struct ResponseMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public struct ResponseChoice
    {
        public int index;
        public ResponseMessage message;
    }

    [System.Serializable]
    public struct Response
    {
        public string id;
        public ResponseChoice[] choices;
    }

    [System.Serializable]
    public struct RequestMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public struct Request
    {
        public string model;
        public RequestMessage[] messages;
    }
    [System.Serializable]
    public struct imgRequest
    {
        public string model;
        public string prompt;
        public string size;
        public string quality;
        public int n;
    }

    [System.Serializable]
    public struct imgResponseData
    {
        public string url;
    }

    [System.Serializable]
    public struct imgResponse
    {
        public imgResponseData[] data;
    }
}
