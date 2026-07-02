#ifndef ALTOS_TEMPORAL_AA_INCLUDED
#define ALTOS_TEMPORAL_AA_INCLUDED

#define _DEBUG_MOTION_VECTORS 0
#define _DEBUG_CHECKERBOARD 0

#include "TextureUtils.hlsl"

bool IsValidHistUV(half2 UV)
{
	if (UV.x <= 0.0 || UV.x >= 1.0 || UV.y <= 0.0 || UV.y >= 1.0)
	{
		return false;
	}
	return true;
}

void TAA_float(Texture2D HistoricData, Texture2D NewFrameData, float2 UV, float BlendFactor, float2 MotionVector, out half4 MergedData, out half3 MergedDataRGB, out half MergedDataA)
{
	#ifdef SHADERGRAPH_PREVIEW
	MergedData = 0;
	MergedDataRGB = 0;
	MergedDataA = 0;
	#endif
	
	float2 texCoord = (1.0 / _ScreenParams.xy);
	
	float4 newFrame = NewFrameData.SampleLevel(point_clamp_sampler, UV, 0);
	float cDepth01 = Linear01Depth(SampleSceneDepth(UV), _ZBufferParams);
	
	float2 offsets[8] =
	{
		float2(0, -1),
		float2(0, 1),
		float2(1, 0),
		float2(-1, 0),
		float2(-1, -1),
		float2(1, -1),
		float2(-1, 1),
		float2(1, 1)
	};
	
	float2 HistUV = UV + MotionVector;
	bool isValidHistUV = IsValidHistUV(HistUV);
	
	
	if (isValidHistUV)
	{
		half4 HistSample = HistoricData.SampleLevel(point_clamp_sampler, HistUV, 0);
			
		half4 minResult, maxResult;
		minResult = newFrame;
		maxResult = newFrame;
		
		for (int i = 0; i < 8; i++)
		{
			half neighborDepthRaw = SampleSceneDepth(UV + texCoord * offsets[i]);
			half neighborDepth01 = Linear01Depth(neighborDepthRaw, _ZBufferParams);
			
			if (abs(neighborDepth01 - cDepth01) < 0.1)
			{
				half4 v = NewFrameData.SampleLevel(point_clamp_sampler, UV + texCoord * offsets[i], 0);
				minResult = min(v, minResult);
				maxResult = max(v, maxResult);
			}
		}
		
		half4 clampedHist = clamp(HistSample, minResult, maxResult);
		newFrame = lerp(clampedHist, newFrame, BlendFactor);
	}
	
	#if _DEBUG_MOTION_VECTORS
	newFrame = half4(1.0, 1.0, 1.0, 0.0);
	
	if (isValidHistUV)
	{
		newFrame = half4((MotionVector).x, (MotionVector).y, 0, 0) * 10.0;
	}
	
	#endif
	
	#if _DEBUG_CHECKERBOARD
	half result = GetSampleInstruction(TexSize, UV);
	newFrame = half4(result, result, result, 0.0);
	#endif
	
	MergedData = newFrame;
	MergedDataRGB = MergedData.rgb;
	MergedDataA = MergedData.a;
}
#endif