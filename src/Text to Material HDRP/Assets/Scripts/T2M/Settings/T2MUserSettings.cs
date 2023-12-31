namespace T2M
{
    /// <summary>
    /// These enums determine the various settings to be used while generating the materials.
    /// This includes the model types, and the various properties of texture output that user wants.
    /// </summary>
    public enum GPTModelType
    {
        GPT4,
        GPT3_5TURBO
    }

    public enum DalleModelType
    {
        DALLE3,
        DALLE2
    }

    public enum Dalle3ImageSize
    {
        SIZE_1024x1024,
        SIZE_1792x1024,
        SIZE_1024x1792
    }
    public enum Dalle2ImageSize
    {
        SIZE_256x256,
        SIZE_512x512,
        SIZE_1024x1024
    }

    public enum ImageQuality
    {
        HD,
        STANDARD
    }
}
