using System.Diagnostics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneExporter))]
public class SceneExporterEditor : Editor
{
    private Stopwatch stopwatch;

    void OnEnable()
    {
        stopwatch = Stopwatch.StartNew();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate as Json"))
        {
            SceneExporter exporter = (target as SceneExporter);
            exporter.GenerateAsJson();
        }
        
        if (GUILayout.Button("Generate as Binary"))
        {
            SceneExporter exporter = (target as SceneExporter);
            exporter.GenerateAsBinary();
        }
    }
}
