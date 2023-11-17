using System.Collections.Generic;
using System.IO;
using System.Linq;
using CapyTracerCore.Core;
using Unity.Mathematics;
using UnityEngine;

public class SceneExporter : MonoBehaviour
{
    [SerializeField]
    private GameObject _sceneContainer;
    public GameObject SceneContainer => _sceneContainer;

    public SerializedScene CreateSceneAsset()
    {
        SerializedScene scene = new SerializedScene();
        
        Camera cam = _sceneContainer.transform.GetComponentInChildren<Camera>();
        scene.camera = new SerializedCamera();
        scene.camera.forward = cam.transform.forward;
        scene.camera.position = cam.transform.position;
        scene.camera.fov = cam.fieldOfView;
        scene.camera.right = cam.transform.right;
        scene.camera.up = cam.transform.up;
        scene.camera.horizontalSize = cam.orthographicSize;

        Light[] lights = _sceneContainer.transform.GetComponentsInChildren<Light>();
        scene.lights = new RenderLight[lights.Length];

        for (int l = 0; l < lights.Length; l++)
        {
            scene.lights[l] = new RenderLight
            {
                position = lights[l].transform.position,
                forward = lights[l].transform.forward,
                intensity = lights[l].intensity,
                range = lights[l].range,
                angle = lights[l].spotAngle,
                type = (int) lights[l].type,
                color = new float4(lights[l].color.r, lights[l].color.g, lights[l].color.b, 1f)
            };
        }

        List<Material> materials = new List<Material>();

        MeshRenderer[] meshes = _sceneContainer.transform.GetComponentsInChildren<MeshRenderer>();

        List<RenderTriangle> allTriangles = new List<RenderTriangle>();

        Bounds sceneBounds = new Bounds();
        
        for(int m = 0; m < meshes.Length; m++)
        {
            foreach (var meshRendererMaterial in meshes[m].sharedMaterials)
            {
                if (!materials.Any( m => m.name == meshRendererMaterial.name))
                    materials.Add(meshRendererMaterial);
            }

            var meshDef = meshes[m].GetComponent<MeshFilter>().sharedMesh;

            Vector3[] vertices = meshDef.vertices;
            Vector3[] normals = meshDef.normals;

            RenderMesh newMesh = new RenderMesh();
            newMesh.triangles = new RenderTriangle[meshDef.triangles.Length / 3];
            int t = 0;

            for (int subMesh = 0; subMesh < meshDef.subMeshCount; subMesh++)
            {
                Vector3 transformPos = meshes[m].transform.position;
                Vector3 transformScale = meshes[m].transform.lossyScale;
                Quaternion transformRotation = meshes[m].transform.rotation;
                
                int[] triangles = meshDef.GetTriangles(subMesh);

                for (int st = 0; st < triangles.Length; st += 3)
                {
                    RenderTriangle newTriangle = new RenderTriangle();
                    newTriangle.materialIndex = materials.FindIndex( mat => mat.name == meshes[m].sharedMaterials[subMesh].name);
                    
                    // copy position from unity triangle
                    // copy normals from unity triangle
                    for (int i = 0; i < 3; i++)
                    {
                        var pos = vertices[triangles[st + i]];
                        var nor  = normals[triangles[st + i]];
                        
                        // apply scale
                        pos.x *= transformScale.x;
                        pos.y *= transformScale.y;
                        pos.z *= transformScale.z;
                        
                        // apply rotation
                        pos = transformRotation * pos;
                        nor = transformRotation * nor;
                        
                        // apply translation
                        pos += transformPos;
                        
                       
                        newTriangle.SetVertexPos(i, pos);
                        newTriangle.SetVertexNormal(i, nor);
                    
                        sceneBounds.Encapsulate(new Vector3(pos.x, pos.y, pos.z));
                    }

                    newMesh.triangles[t] = newTriangle;

                    t++;
                    
                    allTriangles.Add(newTriangle);
                }
            }
        }

        scene.triangles = allTriangles.ToArray();
        
        scene.boundMin = sceneBounds.min;
        scene.boundMax = sceneBounds.max;

        scene.materials = new RenderMaterial[materials.Count];
        for (int m = 0; m < scene.materials.Length; m++)
        {

            bool isEmissive = materials[m].GetColor("_EmissionColor").maxColorComponent > 0;
            Color fromColor = isEmissive ?  materials[m].GetColor("_EmissionColor") : materials[m].color;

            scene.materials[m] = new RenderMaterial
            {
                color =  new float4(fromColor.r, fromColor.g, fromColor.b, 1f),
                roughness = math.clamp(1f - materials[m].GetFloat("_Smoothness"), 0f, 1f),
                isEmissive = isEmissive
            };
        }

        return scene;
    }

    public void GenerateAsBinary()
    {
        var scene = CreateSceneAsset();
        
        using (FileStream fileStream = new FileStream(Application.streamingAssetsPath + $"/{_sceneContainer.name}.dat", FileMode.Create))
        {
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // write scene bounds
                scene.boundMin.WriteBinary(writer);
                scene.boundMax.WriteBinary(writer);
                
                // write camera
                scene.camera.WriteBinary(writer);
                
                // write each Material
                writer.Write(scene.materials.Length);
                foreach (var renderMaterial in scene.materials)
                {
                    renderMaterial.WriteBinary(writer);
                }
                
                // write all triangles

                writer.Write(scene.triangles.Length);

                foreach (var triangle in scene.triangles)
                {
                    triangle.WriteBinary(writer);
                }
                
                // write lights
                writer.Write(scene.lights.Length);
                
                foreach (var renderLight in scene.lights)
                {
                    renderLight.WriteBinary(writer);   
                }
            }
        }
    }

    public static SerializedScene DeserializeScene(string path)
    {
        SerializedScene scene = new SerializedScene();
        
        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                // read scene bounds
                scene.boundMin = SceneBinaryRead.ReadFloat3(reader);
                scene.boundMax = SceneBinaryRead.ReadFloat3(reader);
                
                // read camera
                scene.camera = SceneBinaryRead.ReadCamera(reader);
                
                // read each Material
                scene.qtyMaterials = reader.ReadInt32();
                scene.materials = new RenderMaterial[scene.qtyMaterials];
                
                for (int m = 0; m < scene.qtyMaterials; m++)
                {
                    scene.materials[m] = SceneBinaryRead.ReadMaterial(reader);
                }
                
                // read all triangles
                scene.qtyTriangles = reader.ReadInt32();
                scene.triangles = new RenderTriangle[scene.qtyTriangles];
                
                for (int t = 0; t < scene.qtyTriangles; t++)
                {
                    RenderTriangle triangle = SceneBinaryRead.ReadTriangle(reader);
                    scene.triangles[t] = triangle;
                }
                
                // read lights
                scene.qtyLights = reader.ReadInt32();
                scene.lights = new RenderLight[scene.qtyLights];

                for (int l = 0; l < scene.qtyLights; l++)
                {
                    scene.lights[l] = SceneBinaryRead.ReadLight(reader);
                }
            }
        }

        return scene;
    }
}