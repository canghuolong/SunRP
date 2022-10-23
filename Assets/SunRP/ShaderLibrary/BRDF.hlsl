#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float4 roughness;
};

float OneMinusReflectivity(float4 metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}


BRDF GetBRDF(inout Surface surface,bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if(applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;    
    }
    
    brdf.specular = lerp(MIN_REFLECTIVITY,surface.color,surface.metallic);
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}


#endif