using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SunRP
{
    public class SunLitShaderGUI : ShaderGUI
    {
        enum ShadowMode
        {
            On,
            Clip,
            Dither,
            Off,
        }

        private MaterialEditor _editor;
        private Object[] _materials;
        private MaterialProperty[] _properties;

        RenderQueue RenderQueue
        {
            set
            {
                foreach (Material m in _materials)
                {
                    m.renderQueue = (int)value;
                }
            }
        }

        ShadowMode Shadows
        {
            set
            {
                if (SetProperty("_Shadows", (float)value))
                {
                    SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                    SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
                }
            }
        }


        private bool Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        private bool PremultiplyAlpha
        {
            set => SetProperty("_PremulAplha", "_PREMULTIPLY_ALPHA", value);
        }

        private BlendMode SrcBlend
        {
            set => SetProperty("_SrcBlend", (float)value);
        }

        private BlendMode DstBlend
        {
            set => SetProperty("_DstBlend", (float)value);
        }

        private bool ZWrite
        {
            set => SetProperty("_ZWrite", value ? 1f : 0f);
        }

        private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

        private bool _showPresets;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            base.OnGUI(materialEditor, properties);
            _editor = materialEditor;
            _materials = materialEditor.targets;
            _properties = properties;
            
            BakedEmission();
            
            EditorGUILayout.Space();
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
            if (_showPresets)
            {
                OpaquePreset();
                ClipPreset();
                FadePreset();
                TransparentPreset();
            }
            
            

            if (EditorGUI.EndChangeCheck())
            {
                SetShadowCasterPass();
                CopyLightMappingProperties();
            }
            
        }

        private void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                foreach (Material m in _materials)
                {
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in _materials)
                {
                    m.DisableKeyword(keyword);
                }
            }
        }

        private bool SetProperty(string name, float value)
        {
            MaterialProperty property = FindProperty(name, _properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }

            return false;
        }

        private void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1f : 0f))
            {
                SetKeyword(keyword, value);
            }
        }


        private bool HasProperty(string name) => FindProperty(name, _properties, false) != null;

        private bool PresetButton(string name)
        {
            if (GUILayout.Button(name))
            {
                _editor.RegisterPropertyChangeUndo(name);
                return true;
            }

            return false;
        }

        private void OpaquePreset()
        {
            if (PresetButton("Opaque"))
            {
            }
        }

        private void ClipPreset()
        {
            if (PresetButton("Clip"))
            {
            }
        }

        private void FadePreset()
        {
            if (PresetButton("Fade"))
            {
            }
        }

        private void TransparentPreset()
        {
            if (HasProperty("_PremulAlpha") && PresetButton("Transparent"))
            {
            }
        }

        private void SetShadowCasterPass()
        {
            MaterialProperty shadows = FindProperty("_Shadows", _properties, false);
            if (shadows == null || shadows.hasMixedValue)
            {
                return;
            }

            bool enabled = shadows.floatValue < (float)ShadowMode.Off;
            foreach (Material m in _materials)
            {
                m.SetShaderPassEnabled("ShadowCaster",enabled);
            }
        }

        private void BakedEmission()
        {
            EditorGUI.BeginChangeCheck();
            _editor.LightmapEmissionProperty();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material m in _editor.targets)
                {
                    m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
        void CopyLightMappingProperties () {
            MaterialProperty mainTex = FindProperty("_MainTex", _properties, false);
            MaterialProperty baseMap = FindProperty("_BaseMap", _properties, false);
            if (mainTex != null && baseMap != null) {
                mainTex.textureValue = baseMap.textureValue;
                mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
            }
            MaterialProperty color = FindProperty("_Color", _properties, false);
            MaterialProperty baseColor =
                FindProperty("_BaseColor", _properties, false);
            if (color != null && baseColor != null) {
                color.colorValue = baseColor.colorValue;
            }
        }
    }
}