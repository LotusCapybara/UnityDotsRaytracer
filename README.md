#IMPORTANT! 

This raytracer is meant to be tested in a Build, not in Editor.
This is due the implementation being designed to utilized il2cpp cpu optimizations.
You can still execute it in Editor, but it's not going to run the whole logic, just a enforced scaled down logic to support debugging.

#USAGE
Build the project using il2cpp, supporting unsafe code. 
Once in app you can choose between 3 different scenes and some options.

##BVH times
In some scenes and with some configurations, BVH make take a while so you'll see a white screen.
It could even crash if you go too crazy with some options (specially in the vespa scene).


#Functionality and Goals

This is a research and educational project, I added comments and references in the code base.
The overall implementation is simplified and could be inaccurate. Feel free to comment with your feedback!

##Transparency and Transmission (TODO)

Only opaque materials are supported for now, but I'll be adding refraction and transmission on following iterations.

##GPU Implementation (TODO)
This project aimed to utilize CPU with Unity's Dots and Burst Compiler. 
Some people asked me to do also a GPU implementation, so I'm looking forward to do that in the future.

