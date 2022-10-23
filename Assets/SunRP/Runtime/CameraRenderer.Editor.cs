using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace SunRP.Runtime
{
    public partial class CameraRenderer
    {
#if UNITY_EDITOR
        static ShaderTagId[] _legacyShaderTagIds = {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        private static Material _errorMaterials;

        partial void DrawGizmos()
        {
            if (Handles.ShouldRenderGizmos())
            {
                _context.DrawGizmos(_camera,GizmoSubset.PreImageEffects);
                _context.DrawGizmos(_camera,GizmoSubset.PostImageEffects);
            }
        }
        
        partial void DrawUnsupportedShaders()
        {
            if (_errorMaterials == null)
            {
                _errorMaterials = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }
            var drawingSettings = new DrawingSettings(_legacyShaderTagIds[0], new SortingSettings(_camera))
            {
                overrideMaterial = _errorMaterials
            };
            for (int i = 1; i < _legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i,_legacyShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;
            _context.DrawRenderers(_cullingResults,ref drawingSettings,ref filteringSettings);
        }

        partial void PrepareForSceneWindow()
        {
            if (_camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
            }
        }

        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            _buffer.name = SampleName = _camera.name;
            Profiler.EndSample();
        }
#endif
    }
}
