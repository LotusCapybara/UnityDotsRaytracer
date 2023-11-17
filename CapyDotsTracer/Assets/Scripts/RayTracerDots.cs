using System;
using System.Collections;
using System.Diagnostics;
using CapyTracerCore.Core;
using CapyTracerCore.Tracer;
using Jobs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public enum ERenderPhase
{
    ParsingScene, SamplingPaths, Finished
}

public class RayTracerDots : MonoBehaviour
{
    public Texture2D renderTexture;
    public int iterations;
    public float totalTime = 0f;
    public float indirectSampleAvgTime = 0f;
    public ERenderPhase renderPhase;
    
    [SerializeField]
    private int _innerBatchLoopCount = 32;
    
    private int _width = 300;
    private int _height = 200;
    private NativeArray<float4> _directSamples;
    private NativeArray<float4> _pathSamples;
    private int _totalSize;
    private NativeArray<RenderRay> _cameraRays;
    private RenderScene _renderScene;
    private RenderCamera _renderCamera;
    private NativeArray<Random> _randoms;

    private Stopwatch _stopwatch;
    private bool _isTracing = false;
    
    public void StartTracing(TracerSettings settings, string scenePath)
    {
        if(_isTracing)
            return;

        _isTracing = true;

        StartCoroutine(StartTracingRoutine(settings, scenePath));
    }
    
    private IEnumerator StartTracingRoutine(TracerSettings settings, string scenePath)
    {
        Application.runInBackground = true;
        
        _stopwatch = Stopwatch.StartNew();
         
        // this raytracer is really slow when executed inside the editor, so we override some values to avoid stalling the editor
        // Please use this raytracer in a build, unless you want to debug something
        if (Application.isEditor)
        {
            _width = Math.Min(Screen.width, 100);
            _height = Math.Min(Screen.height, 100);
            settings.maxIterations = 1;
        }
        else
        {
            _width = settings.width;
            _height = settings.height;
            Screen.SetResolution(_width, _height, FullScreenMode.Windowed);    
        }
        
        _totalSize = _width * _height;

        yield return null;


        renderPhase = ERenderPhase.ParsingScene;

        SerializedScene serializedScene = SceneExporter.DeserializeScene(scenePath);

        _renderCamera = new RenderCamera(_width, _height, serializedScene.camera);
        
        _renderScene = new RenderScene();
        _renderScene.settings = settings;
        _renderScene.bounds = new BoundsBox(serializedScene.boundMin, serializedScene.boundMax);
        _renderScene.lights = new NativeArray<RenderLight>(serializedScene.lights, Allocator.Persistent);
        _renderScene.materials = new NativeArray<RenderMaterial>(serializedScene.materials, Allocator.Persistent);
        _renderScene.triangles = new NativeArray<RenderTriangle>(serializedScene.triangles, Allocator.Persistent);
        
        _randoms = new NativeArray<Random>(_totalSize, Allocator.Persistent);
        
        yield return null;

        Job_GenerateBHV generateBhv = new Job_GenerateBHV
        {
            scene = _renderScene
        };
        
        generateBhv.Execute();

        _renderScene.bvhNodes = new NativeArray<StackBVHNode>(generateBhv.outNodes, Allocator.Persistent);

        generateBhv.outNodes.Dispose();
        
        _cameraRays = new NativeArray<RenderRay>(_totalSize, Allocator.Persistent);
        _directSamples = new NativeArray<float4>(_totalSize, Allocator.Persistent);
        _pathSamples = new NativeArray<float4>(_totalSize, Allocator.Persistent);
        renderTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        
        // Initialize all black
        for (int i = 0; i < _totalSize; i++)
        {
            _directSamples[i] = new float4(0, 0, 0, 1);
            _pathSamples[i] = new float4(0, 0, 0, 1);
        }
        
        StartCoroutine(RenderRoutine());
    }

    private void OnDestroy()
    {
        _directSamples.Dispose();
        _pathSamples.Dispose();
        _cameraRays.Dispose();
        _renderScene.lights.Dispose();
        _renderScene.materials.Dispose();
        _renderScene.triangles.Dispose();
        _randoms.Dispose();

        if (_renderScene.bvhNodes.IsCreated)
        {
            _renderScene.bvhNodes.Dispose();
        }
    }
    
    private IEnumerator RenderRoutine()
    {
        // create camera Rays
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                _cameraRays[y * _width + x] = _renderCamera.GetRay(x, y);
            }
        }

        for(int i = 0; i <_totalSize; i++)
            _randoms[i] = Random.CreateFromIndex((uint) UnityEngine.Random.Range(0, int.MaxValue));
        
        renderPhase = ERenderPhase.SamplingPaths;
        for (int i = 0; i < _renderScene.settings.maxIterations; i++)
        {
            yield return SamplePaths();    
            totalTime = (float) _stopwatch.Elapsed.TotalSeconds;
        }

        renderPhase = ERenderPhase.Finished;
        totalTime = (float) _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Stop();
    }
    
    private IEnumerator SamplePaths()
    {
        Stopwatch swIndirect = Stopwatch.StartNew();
        
        Job_PathTracing pathTracingJob = new Job_PathTracing
        {
            iterations = iterations,
            randoms =  _randoms,
            scene = _renderScene,
            cameraRays = _cameraRays,
            indirectColors = _pathSamples,
        };
        
        var handle = pathTracingJob.Schedule(_totalSize, _innerBatchLoopCount);
        handle.Complete();

        iterations++;
        
        swIndirect.Stop();
        
        float iterationWeight = 1f / (iterations);
        
        indirectSampleAvgTime = indirectSampleAvgTime * (1f - iterationWeight) + (float) swIndirect.Elapsed.TotalSeconds * iterationWeight;

        yield return null;
        
        UpdateTexture();

        yield return null;
    }
    
    private void UpdateTexture()
    {
        NativeArray<float4> finalColors = new NativeArray<float4>(_totalSize, Allocator.Temp);
        Job_PostProcess jobPostProcess = new Job_PostProcess
        {
            sampleColors = _pathSamples,
            finalColors = finalColors
        };
        jobPostProcess.Schedule(_totalSize, 128);
        
        
        Color[] texturePixes = new Color[_totalSize];

        for (int i = 0; i < _totalSize; i++)
        {
            Color pixelColor = Color.black;
            pixelColor.r = finalColors[i].x;  
            pixelColor.g = finalColors[i].y; 
            pixelColor.b = finalColors[i].z; 
            pixelColor.a = 1f; 

            texturePixes[i] = pixelColor;
        }

        finalColors.Dispose();
        
        renderTexture.SetPixels(texturePixes);
        renderTexture.Apply();
    }
}
