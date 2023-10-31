using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using CapyTracerCore.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

enum ERenderPhase
{
    ParsingScene, SamplingDirect, SamplingIndirect, Finished
}

public class RayTracerDots : MonoBehaviour
{
    private const int QTY_RANDOMS = 15000;
    
    [SerializeField]
    private SceneExporter _sceneExporter;
    
    [SerializeField]
    private int _innerBatchLoopCount = 32;
    
    [SerializeField]
    private int _width = 300;
    
    [SerializeField]
    private int _height = 200;
    
    [SerializeField]
    private int _maxBounces = 1;
    
    [SerializeField]
    private int _maxIndirectSamples = 1;
    
    [SerializeField]
    private Color _ambientColor = Color.black;
    
    private Texture2D _renderTexture;
    private NativeArray<float4> _directSamples;
    private NativeArray<float4> _indirectSamples;
    private int _totalSize;
    private NativeArray<TriangleHitInfo> _cameraHits;
    private RenderScene _renderScene;
    private ERenderPhase _renderPhase;

    private int _iterations;
    
    private NativeArray<float3> _randomNumbers;

    private Stopwatch _stopwatch;

    private float _indirectSampleAvgTime = 0f;
    
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        
        _stopwatch = Stopwatch.StartNew();
         
        if (Application.isEditor)
        {
            _width = Math.Min(Screen.width, 300);
            _height = Math.Min(Screen.height, 200);
        }
        else
        {
            Screen.SetResolution(_width, _height, FullScreenMode.Windowed);    
        }

        yield return null;
        
        _renderPhase = ERenderPhase.ParsingScene;
        
        SerializedScene serializedScene = _sceneExporter.CreateSceneAsset();
        

        _renderScene = new RenderScene();
        _renderScene.lights = new NativeArray<RenderLight>(serializedScene.lights, Allocator.Persistent);
        _renderScene.materials = new NativeArray<RenderMaterial>(serializedScene.materials, Allocator.Persistent);

        List<RenderTriangle> allTriangles = new List<RenderTriangle>();
        foreach (var serializedMesh in serializedScene.meshes)
        {
            foreach (var triangle in serializedMesh.triangles)
            {
                triangle.Compute();
                allTriangles.Add(triangle);
            }
        }
        _renderScene.triangles = new NativeArray<RenderTriangle>(allTriangles.ToArray(), Allocator.Persistent);
        
        yield return null;
        
        _totalSize = _width * _height;
        
        _cameraHits = new NativeArray<TriangleHitInfo>(_totalSize, Allocator.Persistent);
        _directSamples = new NativeArray<float4>(_totalSize, Allocator.Persistent);
        _indirectSamples = new NativeArray<float4>(_totalSize, Allocator.Persistent);

        _randomNumbers = new NativeArray<float3>(QTY_RANDOMS, Allocator.Persistent);
        
        _renderTexture = new Texture2D(_width, _height);

        StartCoroutine(RenderRoutine());
    }

    private void OnDestroy()
    {
        _directSamples.Dispose();
        _indirectSamples.Dispose();
        _cameraHits.Dispose();

        _renderScene.lights.Dispose();
        _renderScene.materials.Dispose();
        _renderScene.triangles.Dispose();
        _randomNumbers.Dispose();
    }
    
    private IEnumerator RenderRoutine()
    {
        _renderPhase = ERenderPhase.SamplingDirect;
        yield return null;
        yield return DirectSample();

        yield return null;

        _renderPhase = ERenderPhase.SamplingIndirect;
        for (int i = 0; i < _maxIndirectSamples; i++)
        {
           yield return SampleIndirect();    
        }

        _renderPhase = ERenderPhase.Finished;
    }

    private IEnumerator DirectSample()
    {
        // Initialize all black
        for (int i = 0; i < _totalSize; i++)
        {
            _directSamples[i] = new float4(0, 0, 0, 1);
            _indirectSamples[i] = new float4(0, 0, 0, 1);
        }
        
        UpdateTexture();
        
        // create camera Rays
        // The SceneExporter already parses the tracerCamera and exports tracerCamera data. 
        // However, instead of implementing the tracerCamera rays in this Unity version, we can just rely
        // on Unity's tracerCamera for that, hence we gather the Unity tracerCamera
        Camera camera = _sceneExporter.SceneContainer.GetComponentInChildren<Camera>();
        
        
        NativeArray<RenderRay> cameraRays = new NativeArray<RenderRay>(_totalSize, Allocator.Persistent);
        
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                Ray unityRay = camera.ScreenPointToRay(new Vector3(x, y, 0));
        
                cameraRays[y * _width + x] = new RenderRay
                {
                    origin = unityRay.origin,
                    direction = unityRay.direction
                };
            }
        }

        // Do the direct Sample

        Job_DirectSamples directSamplesJob = new Job_DirectSamples
        {
            scene = _renderScene,
            cameraRays = cameraRays,
            cameraHits = _cameraHits,
            directColors = _directSamples
        };
        
        var handle = directSamplesJob.Schedule(_totalSize, _innerBatchLoopCount);
        handle.Complete();
        //
        cameraRays.Dispose();
        
        yield return null;
        
        UpdateTexture();
    }

    private IEnumerator SampleIndirect()
    {
        Stopwatch swIndirect = Stopwatch.StartNew();
        
        // randomize randoms
        UnityEngine.Random.InitState(_iterations);
        for (int i = 0; i < QTY_RANDOMS; i++)
        {
            _randomNumbers[i] = UnityEngine.Random.insideUnitSphere;
        }
        
        Job_IndirectSamples indirectSamplesJob = new Job_IndirectSamples
        {
            iteration = _iterations++,
            scene = _renderScene,
            cameraHits = _cameraHits,
            indirectColors = _indirectSamples,
            maxBounces = _maxBounces,
            randomNumbers = _randomNumbers
        };
        
        var handle = indirectSamplesJob.Schedule(_totalSize, _innerBatchLoopCount);
        handle.Complete();
        
        swIndirect.Stop();
        
        float iterationWeight = 1f / (_iterations);
        
        _indirectSampleAvgTime = _indirectSampleAvgTime * (1f - iterationWeight) + (float) swIndirect.Elapsed.TotalSeconds * iterationWeight;

        yield return null;
        
        UpdateTexture();

        yield return null;
    }
    
    private void UpdateTexture()
    {
        Color[] texturePixes = new Color[_totalSize];

        for (int i = 0; i < _totalSize; i++)
        {
            float4 resultColor = _directSamples[i] + _indirectSamples[i];
            
            Color pixelColor = Color.black;
            pixelColor.r = resultColor.x * 255f; 
            pixelColor.g = resultColor.y * 255f; 
            pixelColor.b = resultColor.z * 255f; 
            pixelColor.a = 255;
            
            texturePixes[i] = pixelColor;
        }

        _renderTexture.SetPixels(texturePixes);
        _renderTexture.Apply();
    }
    
    
    
    
    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(0, 0, _width, _height), _renderTexture);
        
        switch (_renderPhase)
        {
            case ERenderPhase.ParsingScene:
                GUI.Label(new Rect(10, 10, 1000, 350), "PARSING SCENE");
                break;
            case ERenderPhase.SamplingDirect:
                GUI.Label(new Rect(10, 10, 1000, 350), "SAMPLING DIRECT");
                // float averageSampleTime = (0.001f * _accumulatedSamplingTime) / Mathf.Max(_currentIteration, 1f);
                //
                // GUI.Label(new Rect(10, 10, 1000, 150), $"SAMPLE:{ _currentIteration.ToString()} avg: {averageSampleTime.ToString("F1")}");
                break;
            case ERenderPhase.SamplingIndirect:
                GUI.Label(new Rect(10, 10, 1000, 350), "SAMPLING INDIRECT");

                GUI.Label(new Rect(10, 30, 1000, 350), $"SAMPLE:{ _iterations.ToString()} avg: {_indirectSampleAvgTime.ToString("F1")}");
                break;
            case ERenderPhase.Finished:
                GUI.Label(new Rect(10, 10, 1000, 350), "FINISHED");
                break;
        }
        
        GUI.Label(new Rect(10, 50, 1000, 350), $"Time: {_stopwatch.Elapsed.TotalSeconds:F1}");
    }
}
