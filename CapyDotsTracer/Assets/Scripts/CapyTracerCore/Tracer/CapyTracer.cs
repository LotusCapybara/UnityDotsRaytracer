// using System.Diagnostics;
// using System.Runtime.CompilerServices;
// using CapyTracerCore.Core;
//
// namespace CapyTracerCore
// {
//
//     public class CapyTracer
//     {
//         private const int QTY_RANDOM_RAYS = 15000;
//
//         public byte[] pixelsDirect;
//         public byte[] pixelsIndirect;
//         public byte[] pixelsFinal;
//         public byte[] pixelsBHVDensity;
//
//         private readonly RenderTexture _rtDirect;
//         private readonly RenderTexture _rtIndirect;
//         private readonly RenderTexture _rtComposite;
//         private readonly Scene _scene;
//
//         private RenderRay[] _cameraRays;
//         private TriangleHitInfo?[] _directRayHits;
//         private bool[] _directRayHitFounds;
//         private bool _isRendering;
//         private readonly int _pixelsQty;
//         private float _totalIndirectTime;
//         private readonly Vec3[] _randomRays = new Vec3[QTY_RANDOM_RAYS];
//         private int _rayIndex;
//         private readonly Random _rnd = new Random(1687894);
//
//         public delegate void OnStepDelegate(EStepEvent step);
//
//         public OnStepDelegate onStepFinished = delegate { };
//
//         public int Width { get; }
//         public int Height { get; }
//         public int TotalTriangles { get; }
//         public int TotalPartitions { get; private set; }
//         public float BHVGenerationTime { get; private set; }
//         public int Iterations { get; private set; }
//         public float DirectSampleTime { get; private set; }
//         public float IndirectSampleAvgTime { get; private set; }
//
//         public CapyTracer(int width, int height, SerializedScene serializedScene)
//         {
//             Width = width;
//             Height = height;
//             _pixelsQty = width * height;
//
//             _rtDirect = new RenderTexture(width, height);
//             _rtIndirect = new RenderTexture(width, height);
//             _rtComposite = new RenderTexture(width, height);
//             pixelsDirect = new byte[_pixelsQty];
//             pixelsIndirect = new byte[_pixelsQty];
//             pixelsFinal = new byte[_pixelsQty];
//             pixelsBHVDensity = new byte[_pixelsQty];
//
//             TotalTriangles = 0;
//
//             foreach (var serializedSceneMesh in serializedScene.meshes)
//             {
//                 TotalTriangles += serializedSceneMesh.triangles.Length;
//             }
//
//             _scene = new Scene(serializedScene, width, height);
//
//             Iterations = 0;
//
//             DirectSampleTime = 0f;
//             IndirectSampleAvgTime = 0f;
//             _totalIndirectTime = 0;
//         }
//
//         public async void StartRenderRoutine()
//         {
//             if (_isRendering)
//                 return;
//
//             Iterations = 0;
//             DirectSampleTime = 0f;
//             IndirectSampleAvgTime = 0f;
//             _totalIndirectTime = 0;
//
//             _isRendering = true;
//
//             CreateRenderRays();
//
//             Stopwatch sw = Stopwatch.StartNew();
//             _scene.GenerateBHV();
//             BHVGenerationTime = (float)sw.Elapsed.TotalSeconds;
//
//             CalculateBHVDensity();
//
//             sw.Restart();
//             SampleDirect();
//             DirectSampleTime = (float)sw.Elapsed.TotalSeconds;
//
//             onStepFinished.Invoke(EStepEvent.DirectSample);
//             onStepFinished.Invoke(EStepEvent.IndirectSampleIteration);
//
//             for (int i = 0; i < TracerSettings.MAX_ITERATIONS; i++)
//             {
//                 await Task.Delay(100);
//
//                 sw.Restart();
//                 SampleIndirect();
//
//                 Iterations++;
//
//                 _totalIndirectTime += (float)sw.Elapsed.TotalSeconds;
//                 IndirectSampleAvgTime = _totalIndirectTime / (Iterations);
//
//
//                 onStepFinished.Invoke(EStepEvent.IndirectSampleIteration);
//
//                 if (Iterations >= 500)
//                     break;
//
//                 if (!_isRendering)
//                     return;
//             }
//
//             _isRendering = false;
//
//             onStepFinished.Invoke(EStepEvent.FinishedAll);
//         }
//
//         public void StopTracing()
//         {
//             _isRendering = false;
//         }
//
//         private void CalculateBHVDensity()
//         {
//             RenderColor[] densityColors = new RenderColor[_pixelsQty];
//
//             for (int i = 0; i < _pixelsQty; i++)
//             {
//                 int qty = TracerFunctions.FindQtyHittingNodes(_cameraRays[i]);
//                 RenderColor color = new RenderColor();
//                 color.r = Math.Clamp(0.01f * qty, 0.05f, 1);
//                 densityColors[i] = color;
//             }
//
//             pixelsBHVDensity = TracerFunctions.ColorsToBytes(densityColors);
//
//             TotalPartitions = 0;
//
//             foreach (var kdNode in Scene.s_nodesTree)
//             {
//                 TotalPartitions += kdNode.GetTotalPartitionsQty();
//             }
//
//             onStepFinished.Invoke(EStepEvent.BHVCalculated);
//         }
//
//         private void CreateRenderRays()
//         {
//             _cameraRays = new RenderRay[_pixelsQty];
//
//             for (int i = 0; i < _pixelsQty; i++)
//             {
//                 _cameraRays[i] = _scene.tracerCamera.GetRay(i % Width, i / Width);
//             }
//
//
//             GenerateRandomRayDirections();
//         }
//
//         private void SampleDirect()
//         {
//             _directRayHits = new TriangleHitInfo[_pixelsQty];
//             _directRayHitFounds = new bool[_pixelsQty];
//
//             for (int i = 0; i < _pixelsQty; i++)
//             {
//                 if (TracerFunctions.TryHitTriangle(out TriangleHitInfo hitInfo, _cameraRays[i], 999f, false))
//                 {
//                     _directRayHitFounds[i] = true;
//                     _rtDirect.colors[i] = GetLitColorOnSurface(hitInfo);
//                     _directRayHits[i] = hitInfo;
//                 }
//                 else
//                 {
//                     _rtDirect.colors[i] = RenderColor.Black;
//                     _directRayHitFounds[i] = false;
//                     _directRayHits[i] = null;
//                 }
//
//                 _rtIndirect.colors[i] = RenderColor.Black;
//             }
//
//             pixelsDirect = TracerFunctions.ColorsToBytes(_rtDirect.colors);
//             pixelsFinal = pixelsDirect;
//         }
//
//         private void GenerateRandomRayDirections()
//         {
//             for (int i = 0; i < QTY_RANDOM_RAYS; i++)
//             {
//                 float theta = (float)_rnd.NextDouble() * 2 * MathF.PI; // Random azimuthal angle
//                 float phi = MathF.Acos(2 * (float)_rnd.NextDouble() - 1); // Random polar angle
//                 _randomRays[i] = new Vec3
//                 {
//                     x = MathF.Sin(phi) * MathF.Cos(theta),
//                     y = MathF.Sin(phi) * MathF.Sin(theta),
//                     z = MathF.Cos(phi)
//                 }.GetNormalized();
//             }
//         }
//
//         [MethodImpl(MethodImplOptions.AggressiveInlining)]
//         private int GetNextRayIndex()
//         {
//             _rayIndex += (Iterations + 1);
//             if (_rayIndex >= QTY_RANDOM_RAYS)
//                 _rayIndex = 0;
//             return _rayIndex;
//         }
//
//         private void SampleIndirect()
//         {
//             GenerateRandomRayDirections();
//
//             float iterationWeight = 1f / (Iterations + 1f);
//             int bounces = TracerSettings.QTY_BOUNCES;
//
//             RenderRay bounceRay = new RenderRay();
//
//             for (int i = 0; i < _pixelsQty; i++)
//             {
//                 if (!_directRayHitFounds[i])
//                     continue;
//
//                 RenderColor indirectColor = RenderColor.Black;
//
//                 TriangleHitInfo? surfaceHit = _directRayHits[i];
//
//                 for (int b = 0; b < bounces; b++)
//                 {
//                     bool foundGoodBRDF = false;
//                     float brdf = 0;
//
//                     while (!foundGoodBRDF)
//                     {
//                         bounceRay.direction = _randomRays[GetNextRayIndex()];
//
//                         if (Vec3.Dot(bounceRay.direction, surfaceHit.normal) < 0)
//                             bounceRay.direction *= -1f;
//
//                         bounceRay.origin = surfaceHit.position + surfaceHit.normal * 0.001f;
//
//                         float roughness = Scene.s_materials[surfaceHit.materialIndex].roughness;
//
//                         float diffuseTerm = 0;
//                         float specularTerm = 0;
//
//                         if (roughness > 0f)
//                         {
//                             diffuseTerm = Vec3.Dot(bounceRay.direction, surfaceHit.normal);
//                         }
//
//                         if (roughness < 1f && _rnd.NextDouble() > roughness)
//                         {
//                             Vec3 reflectionVector = Vec3.Reflect(surfaceHit.incomingDirection, surfaceHit.normal);
//                             bounceRay.direction = reflectionVector;
//
//                             Vec3 H = bounceRay.direction + surfaceHit.incomingDirection;
//                             specularTerm = TracerFunctions.GetBlinnPhong(H, surfaceHit.normal, roughness);
//                         }
//
//
//                         brdf = Math.Clamp(diffuseTerm + specularTerm, 0, 1);
//
//                         foundGoodBRDF = true;
//                         if (brdf < 0.05f && _rnd.NextDouble() < 0.5)
//                         {
//                             foundGoodBRDF = false;
//                         }
//                     }
//
//
//                     if (TracerFunctions.TryHitTriangle(out TriangleHitInfo bounceHit, bounceRay, 999f, false))
//                     {
//                         RenderColor colorsOnSurface = GetLitColorOnSurface(bounceHit);
//
//                         indirectColor += colorsOnSurface * brdf * (1f / 3.14f);
//                         surfaceHit = bounceHit;
//
//                     }
//                     else
//                     {
//                         break;
//                     }
//                 }
//
//                 _rtIndirect.colors[i] =
//                     _rtIndirect.colors[i] * (1f - iterationWeight) + indirectColor * iterationWeight;
//                 _rtComposite.colors[i] =
//                     TracerFunctions.ACESFilter(_rtDirect.colors[i] +
//                                                _rtIndirect.colors[i] * (TracerSettings.INDIRECT_POWER));
//             }
//
//
//
//             pixelsIndirect = TracerFunctions.ColorsToBytes(_rtIndirect.colors);
//             pixelsFinal = TracerFunctions.ColorsToBytes(_rtComposite.colors);
//         }
//
//         private RenderColor GetLitColorOnSurface(in TriangleHitInfo surfaceHit)
//         {
//             RenderColor color = RenderColor.Black;
//
//             if (Scene.s_materials[surfaceHit.materialIndex].isEmissive)
//             {
//                 color.AddRGB(Scene.s_materials[surfaceHit.materialIndex].color);
//             }
//             else
//             {
//                 foreach (var light in Scene.s_lights)
//                 {
//                     color.AddRGB(light.GetColorContribution(surfaceHit));
//                 }
//
//                 color.MulRGB(Scene.s_materials[surfaceHit.materialIndex].color);
//             }
//
//             return color;
//         }
//     }
// }