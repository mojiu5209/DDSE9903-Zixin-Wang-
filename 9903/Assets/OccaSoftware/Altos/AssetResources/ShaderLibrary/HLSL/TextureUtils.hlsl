#ifndef ALTOS_TEX_UTILS_INCLUDED
#define ALTOS_TEX_UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float _CLOUD_RENDER_SCALE;
Texture2D _BLUE_NOISE;
SamplerState linear_clamp_sampler;
SamplerState linear_repeat_sampler;
SamplerState point_repeat_sampler;
SamplerState point_clamp_sampler;



half InverseLerp(half a, half b, half v)
{
	return (v - a) / (b - a);
}

half RemapUnclamped(half iMin, half iMax, half oMin, half oMax, half v)
{
	half t = InverseLerp(iMin, iMax, v);
	return lerp(oMin, oMax, t);
}

half Remap(half iMin, half iMax, half oMin, half oMax, half v)
{
	v = clamp(v, iMin, iMax);
	return RemapUnclamped(iMin, iMax, oMin, oMax, v);
}

half Remap01(half iMin, half iMax, half v)
{
	return saturate(Remap(iMin, iMax, 0, 1, v));
}

half EaseIn(half a)
{
	return a * a;
}

half EaseOut(half a)
{
	return 1 - EaseIn(1 - a);
}

half EaseInOut(half a)
{
	return lerp(EaseIn(a), EaseOut(a), a);
}

float2 GetTexCoordSize(float renderTextureScale)
{
	return 1.0 / (_ScreenParams.xy * renderTextureScale);
}



float rand2dTo1d(float2 vec, float2 dotDir = float2(12.9898, 78.233))
{
	float random = dot(sin(vec.xy), dotDir);
	random = frac(sin(random) * 143758.5453);
	return random;
}

float2 rand2dTo2d(float2 vec, float2 seed = 4605)
{
	return float2(
		rand2dTo1d(vec + seed),
		rand2dTo1d(vec + seed, float2(39.346, 11.135))
	);
}

half2 GetDir(half x, half y)
{
	return rand2dTo2d(float2(x, y)) * 2.0 - 1.0;
}

half GetPerlinNoise(half2 position)
{
	half2 lowerLeft = GetDir(floor(position.x), floor(position.y));
	half2 lowerRight = GetDir(ceil(position.x), floor(position.y));
	half2 upperLeft = GetDir(floor(position.x), ceil(position.y));
	half2 upperRight = GetDir(ceil(position.x), ceil(position.y));
	
	half2 f = frac(position);
	
	lowerLeft = dot(lowerLeft, f);
	lowerRight = dot(lowerRight, f - half2(1.0, 0.0));
	upperLeft = dot(upperLeft, f - half2(0.0, 1.0));
	upperRight = dot(upperRight, f - half2(1.0, 1.0));
	
	half2 t = half2(EaseInOut(f.x), EaseInOut(f.y));
	half lowerMix = lerp(lowerLeft.x, lowerRight.x, t.x);
	half upperMix = lerp(upperLeft.x, upperRight.x, t.x);
	return lerp(lowerMix, upperMix, t.y);
}

half GetLayeredPerlinNoise(int octaves, half2 position, half gain, half lacunarity)
{
	half value = 0.0;
	half amp = 1.0;
	half frequency = 1.0;
	half c = 0.0;
	
	for (int i = 1; i <= octaves; i++)
	{
		value += GetPerlinNoise(position * frequency) * amp;
		c += amp;
		amp *= gain;
		frequency *= lacunarity;
	}
	value /= c;
	return saturate(value + 0.5);
}


void GradientPerlinNoise_half(half2 position, out half value)
{
	value = GetPerlinNoise(position);
}

void ScreenToViewVector_half(half2 UV, out half3 viewVector)
{
#ifdef SHADERGRAPH_PREVIEW
	viewVector = half3(0.0, 0.0, 0.0);
#endif
	
	float3 viewDirectionTemp = mul(unity_CameraInvProjection, float4(UV * 2 - 1, 0.0, -1));
	viewVector = mul(unity_CameraToWorld, viewDirectionTemp);
}

float4 ObjectToClipPos(float3 pos)
{
	return mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(pos, 1)));
}

half3 Saturation(half3 color, half amount)
{
	half l = dot(color, float3(0.2126, 0.7152, 0.0722));
	return l + amount * (color - l);
}

int _FrameId;
Texture2D _DitheredDepthTexture;

// <- Note on Upsample Methodology ->
// 
// Sample the local box and accumulate samples with a similar depth
// Then replace the color value here with a random sample from the local depth region
//
// <- End Note ->
float _UPSCALE_SOURCE_RENDER_SCALE;
void CheckerboardUpsample_half(Texture2D Tex, half2 UV, out half3 Color, out half Alpha)
{
#ifdef SHADERGRAPH_PREVIEW
	Color = 0;
	Alpha = 0;
#endif
	
	float2 o[4] =
	{
		float2(0.0, 0.0),
		float2(1.0, 0.0),
		float2(1.0, 1.0),
		float2(0.0, 1.0)
	};
	
	
	half2 ScreenPos = _ScreenParams.xy;
	half2 texCoord = GetTexCoordSize(_UPSCALE_SOURCE_RENDER_SCALE);
	
	float depthRaw = SampleSceneDepth(UV);
	float depth01 = Linear01Depth(depthRaw, _ZBufferParams);
	
	float4 col = Tex.SampleLevel(point_clamp_sampler, UV, 0);
	int validCounter = 0;
	float4 colors[4];
	
	for (int f = 0; f < 4; f++)
	{
		float sDepthRaw = _DitheredDepthTexture.SampleLevel(point_clamp_sampler, (UV + o[f] * texCoord), 0);
		float sDepth01 = Linear01Depth(sDepthRaw, _ZBufferParams);
		
		if (depth01 - sDepth01 < 0.01)
		{
			colors[validCounter] = Tex.SampleLevel(point_clamp_sampler, UV + o[f] * texCoord, 0);
			validCounter++;
		}
	}
	
	int rMod = floor(rand2dTo1d(UV + _Time.y) * validCounter);
	col = colors[rMod];
	
	col = Tex.SampleLevel(linear_clamp_sampler, UV, 0);
	
	Color = col.rgb;
	Alpha = col.a;
}


// <- Start Note ->
//
// For each 2x2 pixel block, I want to pick the farthest depth and assign it to all 4 pixels.
// 
// SampleSceneDepth returns a value in the range [1, 0], where 1 is the near plane and 0 is the far plane.
// To get the farthest sample, you want the lesser raw depth value.
// This may depend on the platform.
// If we start seeing bugs in this area, we can compare the Linear01Depths instead using Linear01Depth(rawDepth, _ZBufferParams) and match an index.
// For now, we aren't taking this approach because it can incur a performance cost.
//
// <- End Note ->
int _USE_DITHERED_DEPTH;

void DitherDepth_float(float2 UV, out float RawDepth)
{
	#ifdef SHADERGRAPH_PREVIEW
	RawDepth = 0;
	#endif
	
	
	if (_USE_DITHERED_DEPTH)
	{
		float2 o[4] =
		{
			float2(0.0, 0.0),
			float2(1.0, 0.0),
			float2(1.0, 1.0),
			float2(0.0, 1.0)
		};
		
		float2 s = GetTexCoordSize(1.0);
		float depthMaxRaw = 1.0;
		UV *= _ScreenParams.xy;
		UV *= 0.5;
		UV = floor(UV);
		UV *= 2.0;
		UV /= _ScreenParams.xy;
		
		for (int i = 0; i < 4; i++)
		{
			float2 coord = UV + s * o[i] + s * 0.5;
			float depthSampleRaw = SampleSceneDepth(coord);;
			depthMaxRaw = min(depthSampleRaw, depthMaxRaw);
		}
		RawDepth = depthMaxRaw;
	}
	else
	{
		RawDepth = SampleSceneDepth(UV);
	}
}
#endif
