# Functionality and Goals

This is a research and educational project, I added comments and references in the code base.
The overall implementation is simplified and could be inaccurate. The main goal of this project was to experiment with DOTS and Burst, but I think I might end up doing more than that in the future.
Feel free to comment with your feedback!

# IMPORTANT! 

This raytracer is meant to be tested in a Build, not in Editor.
This is due the implementation being designed to utilized il2cpp cpu optimizations.
You can still execute it in Editor, but it's not going to run the whole logic, just a enforced scaled down logic to support debugging.

# SAMPLE SCENES
These 3 sample scenes are included in the project. You can build your own ones too (read below)

![Scenes](https://i.gyazo.com/f315a687544680c4393e5bb2e1a60c64.jpg)

# USAGE
Build the project using il2cpp, supporting unsafe code. 
Once in app you can choose between 3 different scenes and some options.

## BVH times 
In some scenes and with some configurations, BVH make take a while so you'll see a white screen.
It could even crash if you go too crazy with some options (specially in the vespa scene).

## Simple Materials
This implementation only supports simple default materials. Textures are ignored.

## Texture Mapping Support (TODO)
I might add texture mapping support in the future. The thing is that I don't want to make this implementation really slow, since it's just an educational project.
However, if I implement the GPU version of it then texture mapping will be added for sure.

## Scene Exporter
You'll find a Scene called "SceneBuilder" that contains the logic to create and export scenes for the raytracer.
Each scene is exported as a custom binary format. I made this in this way to support using the same scenes in other languages (I'm also creating a webasm raytracer example with Blazor)

## Build your own Scene
You can build your own scene by creating it inside a root gameobject, then assigning that root gameobject to the scene field of script in SceneExporter.
Press "export binary" and that should create a file inside the Streaming Assets folder.

### Considerations when building your own scene
* Take into account this is a CPU raytracer, so heavy geometry could be a breaker
* Point, Spot and Directional lights are supported.
* You need a single camera inside the scene
* Materials should be simple
* Only static meshes are supported
* Emissive materials are supported but they might work not greatly, that's something to improve

## Transparency and Transmission (TODO)

Only opaque materials are supported for now, but I'll be adding refraction and transmission on following iterations.

## GPU Implementation (TODO)
This project aimed to utilize CPU with Unity's Dots and Burst Compiler. 
Some people asked me to do also a GPU implementation, so I'm looking forward to do that in the future.
Of course this would be implemented with Compute Shaders. I'm considering giving RTX Acceleration Structures support, but Unity 2023 is required for that.

## MOTO VESPA ATTRIBUTION
The beautiful Moto Vespa was grabbed from Sketchfab and made by Mohamed Ouartassi 
https://sketchfab.com/3d-models/moto-vespa-f1c7cacdbc954b48a4062a4734617afb

