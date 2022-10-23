using System;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour {
	
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    private static int emissionColorId = Shader.PropertyToID("_EmissionColor");
    static MaterialPropertyBlock block;
	
    [SerializeField]
    Color baseColor = Color.white;
    
    [SerializeField,ColorUsage(false,true)]
    Color emissionColor = Color.black;
    
    [SerializeField, Range(0f, 1f)]
    float cutoff = 0.5f;

    [SerializeField, Range(0f, 1f)]
    private float metallic = 0f;
    [SerializeField, Range(0f, 1f)]
    private float smoothness = 0.5f;
    
    private void Awake()
    {
	    OnValidate();
    }

    void OnValidate () {
	    if (block == null) {
		    block = new MaterialPropertyBlock();
	    }
	    block.SetColor(baseColorId, baseColor);
	    block.SetColor(emissionColorId,emissionColor);
	    block.SetFloat(cutoffId, cutoff);
	    block.SetFloat(metallicId,metallic);
	    block.SetFloat(smoothnessId,smoothness);
	    GetComponent<Renderer>().SetPropertyBlock(block);
    }
}