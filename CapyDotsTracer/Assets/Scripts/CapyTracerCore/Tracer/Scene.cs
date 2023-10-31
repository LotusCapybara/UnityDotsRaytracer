// using System.Collections.Generic;
//
// namespace CapyTracerCore.Core
// {
//
//     public class TracerScene
//     {
//         public static RenderMaterial[] s_materials;
//         public static RenderLight[] s_lights;
//
//         public SerializedCamera camera;
//
//         public static List<KDNode> s_nodesTree;
//
//         private SerializedScene _serializedScene;
//
//         public TracerScene(SerializedScene serializedScene, int width, int height)
//         {
//             _serializedScene = serializedScene;
//
//             camera = new SerializedCamera(width, height);
//
//             camera.position = serializedScene.camera.position;
//             camera.forward = serializedScene.camera.forward;
//             camera.up = serializedScene.camera.up;
//             camera.right = serializedScene.camera.right;
//             camera.horizontalSize = serializedScene.camera.horizontalSize;
//             camera.fov = serializedScene.camera.fov;
//
//             camera.UpdateValues();
//
//             s_materials = serializedScene.materials;
//             s_lights = serializedScene.lights;
//         }
//
//         public void GenerateBHV()
//         {
//             s_nodesTree = new List<KDNode>();
//
//             for (int m = 0; m < _serializedScene.meshes.Length; m++)
//             {
//                 var bounds = new BoundsBox();
//
//                 List<RenderTriangle> triangles = new List<RenderTriangle>();
//
//                 for (int t = 0; t < _serializedScene.meshes[m].triangles.Length; t++)
//                 {
//                     RenderTriangle triangle = _serializedScene.meshes[m].triangles[t];
//                     triangle.Compute();
//
//                     bounds.ExpandWithTriangle(triangle);
//                     triangles.Add(triangle);
//                 }
//
//                 KDNode meshNode = new KDNode(bounds);
//
//                 foreach (var t in triangles)
//                 {
//                     meshNode.TryInsertTriangle(t);
//                 }
//
//                 meshNode.FinishGeneration();
//
//                 s_nodesTree.Add(meshNode);
//             }
//         }
//     }
// }