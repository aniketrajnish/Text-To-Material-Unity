# Text-To-Material-Unity
 Generate materials from text prompts in Unity. Uses `chatgpt` and `dall-e` models.
 
https://github.com/aniketrajnish/Text-To-Material-Unity/assets/58925008/59d84850-b002-425e-aeda-0a1c67a77153

## Usage
* Download the `.unitypackage` from the [Releases Section](https://github.com/aniketrajnish/Text-To-Material-Unity/releases/tag/v001).
* Create/Login to your OpenAI account and get your API key from [here](https://platform.openai.com/api-keys).
* Import the package in your Unity Project.
* Go to `Project Settings -> T2M -> paste your API key`
* Right click in the Project Window, click on `Create - > T2M` to open the Text-To-Material window.
* Enter the prompt describing the type of material and texture of the material you want to create.
* Choose the model to use, the image size, and the quality (only for `dall-e-3`) for textures.
* Click on Generate. The textures and materials are generated and saved in the `Assets/T2M Materials` folder.
* Note: `gpt-4` and `dall-e-3` might not be available for all the users.

## Contributing
Contributions to the project are welcome. Currently working on:
* Support for URP & HDRP materials.
* Support for materials based on different shaders.
* Better prompts for texture generation, to ensure seamless textures.
* Support for materials based on custom shaders (ig ambitious).
  
## License
MIT License
