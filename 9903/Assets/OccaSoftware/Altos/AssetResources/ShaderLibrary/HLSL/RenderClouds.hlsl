#ifndef ALTOS_VOLUMETRIC_CLOUDS_INCLUDED
#define ALTOS_VOLUMETRIC_CLOUDS_INCLUDED

#include "TextureUtils.hlsl"

// Static Constant Defines
static half EPSILON = 0.0001;
static half EPSILON_SMALL = 0.000001;
static half highAltitudeFadeDistance = 350000.0;
static half _CIRRUS_CLOUD_HEIGHT = 20000.0;
static half3 _CLOUD_ALBEDO = half3(1.0, 0.964, 0.92);
static half3 _PLANET_CENTER;
static half _CONVERT_KM_TO_M = 1000.0;

// Variable Global Property Defines
uniform half2 _CLOUD_WEATHERMAP_VELOCITY = half2(0.0, 0.0);
uniform int _USE_CLOUD_WEATHERMAP_TEX = 0;
uniform half _CLOUD_WEATHERMAP_SCALE = 0.0;
uniform half2 _CLOUD_WEATHERMAP_VALUE_RANGE = half2(0.0, 1.0);
uniform Texture2D _CLOUD_DENSITY_CURVE_TEX;


half CalculateHorizonFalloff(half3 rayPosition, half3 lightDirection, half planetRadius)
{
	half h = max(rayPosition.y, 0);
	half r = planetRadius;
	half a = r + h;
	half b = r / a;
	half c = acos(b);
	half angle = lightDirection.y * 1.571;
	half d = angle - c;
	
	return smoothstep(radians(-5.0), radians(5.0), d);
}

half GetHeight01(half3 rayOrigin, half3 rayPos, half atmosThickness, half planetRadius, half atmosHeight)
{
	half height01 = distance(rayPos, _PLANET_CENTER) - (planetRadius + atmosHeight);
	height01 /= atmosThickness;
	return saturate(height01);
}

bool IsInsideSDFSphere(half3 pointToCheck, half3 spherePosition, half sphereRadius)
{
	half dist = distance(pointToCheck, spherePosition);
	
	if (dist < sphereRadius)
		return true;
	
	return false;
}

struct IntersectData
{
	bool hit;
	bool inside;
	half frontfaceDistance;
	half backfaceDistance;
};

// Resources...
// http://kylehalladay.com/blog/tutorial/math/2013/12/24/Ray-Sphere-Intersection.html
// https://stackoverflow.com/questions/6533856/ray-sphere-intersection
// https://www.cs.colostate.edu/~cs410/yr2017fa/more_progress/pdfs/cs410_F17_Lecture10_Ray_Sphere.pdf
// 
bool SolveQuadratic(float a, float b, float c, out float x0, out float x1)
{
	float discr = b * b - 4 * a * c;
	if (discr < 0)
		return false;
	else if (discr == 0)
		x0 = x1 = -0.5 * b / a;
	else
	{
		float q = (b > 0) ?
            -0.5 * (b + sqrt(discr)) :
            -0.5 * (b - sqrt(discr));
		x0 = q / a;
		x1 = c / q;
	}
	
	float lT = min(x0, x1);
	float gT = max(x0, x1);
	x0 = lT;
	x1 = gT;
 
	return true;
}

bool IntersectSphere(half3 rayOrigin, half3 rayDir, half sphereRad, half3 spherePosition, out float nearHit, out float farHit)
{
	IntersectData intersectionData;
	nearHit = 0.0;
	farHit = 0.0;
	
	half a = dot(rayDir, rayDir);
	float3 L = rayOrigin - spherePosition;
	half b = 2.0 * dot(rayDir, L);
	half c = dot(L, L) - (sphereRad * sphereRad);
	float t0, t1;
	if (!SolveQuadratic(a, b, c, t0, t1))
		return false;
	
	float lt = min(t0, t1);
	float gt = max(t0, t1);
	t0 = lt;
	t1 = gt;
	
	if (t0 < 0)
	{
		t0 = t1;
		if(t0 < 0)
			return false;
	}
	nearHit = max(t0, 0);
	farHit = max(t1, 0);
	return true;
}

struct AtmosHitData
{
	bool didHit;
	bool doubleIntersection;
	half nearDist;
	half nearDist2;
	half farDist;
	half farDist2;
};


AtmosHitData AtmosphereIntersection(half3 rayOrigin, half3 rayDir, half atmosHeight, half planetRadius, half atmosThickness, half maxDist)
{
	half3 sphereCenter = _PLANET_CENTER;
	half innerRad = planetRadius + atmosHeight;
	half outerRad = planetRadius + atmosHeight + atmosThickness;
	
	half innerNear, innerFar, outerNear, outerFar;
	bool hitInner = IntersectSphere(rayOrigin, rayDir, innerRad, sphereCenter, innerNear, innerFar);
	bool hitOuter = IntersectSphere(rayOrigin, rayDir, outerRad, sphereCenter, outerNear, outerFar);
	
	AtmosHitData hitData;
	hitData.didHit = false;
	hitData.doubleIntersection = false;
	
	bool insideInner = IsInsideSDFSphere(rayOrigin, sphereCenter, innerRad);
	bool insideOuter = IsInsideSDFSphere(rayOrigin, sphereCenter, outerRad);
	
	half nearIntersectDistance = 0.0;
	half farIntersectDistance = 0.0;
	half nearIntersectDistance2 = 0.0;
	half farIntersectDistance2 = 0.0;
	
	//Case 1
	if (insideInner && insideOuter)
	{
		nearIntersectDistance = innerNear;
		farIntersectDistance = outerNear;
	}
	
	// Case 2
	if (!insideInner && insideOuter)
	{
		farIntersectDistance = min(outerNear, maxDist);
		
		// InnerData.frontFaceDistance > 0 when the ray intersects with the inner sphere.
		if (innerNear > 0.0)
		{
			farIntersectDistance = min(innerNear, maxDist);
			
			if (innerFar < maxDist)
			{
				nearIntersectDistance2 = innerFar;
				farIntersectDistance2 = min(outerFar, maxDist);
			}
		}
	}
	
	bool lookingAboveClouds = false;
	// Case 3
	if (!insideInner && !insideOuter)
	{
		if (!hitInner && !hitOuter)
			lookingAboveClouds = true;
		
		nearIntersectDistance = outerNear;
		farIntersectDistance = min(outerFar, maxDist);
		
		// InnerData.frontFaceDistance > 0 when the ray intersects with the inner sphere.
		if (innerNear > 0.0)
		{
			farIntersectDistance = min(innerNear, maxDist);
			if (innerFar < maxDist)
			{
				nearIntersectDistance2 = innerFar;
				farIntersectDistance2 = min(outerFar, maxDist);
			}
		}
	}
	
	hitData.nearDist = nearIntersectDistance;
	hitData.nearDist2 = nearIntersectDistance2;
	hitData.farDist = farIntersectDistance;
	hitData.farDist2 = farIntersectDistance2;
	
	if (hitData.nearDist < maxDist)
		hitData.didHit = true;
	
	if (hitData.nearDist2 > 0.0)
		hitData.doubleIntersection = true;
	
	if (lookingAboveClouds)
		hitData.didHit = false;
	
	return hitData;
}


struct AtmosphereData
{
	half atmosThickness;
	half atmosHeight;
	half cloudFadeDistance;
	half distantCoverageAmount;
	half distantCoverageDepth;
};


struct StaticMaterialData
{
	SamplerState fogSampler;
	
	half3 rayOrigin;
	half3 sunPos;
	half3 sunColor;
	half sunIntensity;
	
	bool renderLocal;
	
	half cloudiness;
	half alphaAccumulation;
	half4 baseRGBAInf;
	half3 extinction;
	half3 highAltExtinction;
	half HG;
	
	half ambientExposure;
	half3 ambientColor;
	half3 fogColor;
	half fogPower;
	
	half multipleScatteringAmpGain;
	half multipleScatteringDensityGain;
	int multipleScatteringOctaves;
	
	Texture3D baseTexture;
	half3 baseScale;
	half3 baseTimescale;
	
	Texture2D curlNoise;
	half curlScale;
	half curlStrength;
	half curlTimescale;
	
	Texture3D detail1Texture;
	half3 detail1Scale;
	half detail1Strength;
	half3 detail1Timescale;
	bool detail1Invert;
	half2 detail1HeightRemap;
	half4 detail1RGBAInf;
	
	Texture2D highAltTex1;
	Texture2D highAltTex2;
	Texture2D highAltTex3;
	half highAltitudeAlphaAccumulation;
	half2 highAltOffset1;
	half2 highAltOffset2;
	half2 highAltOffset3;
	half2 highAltScale1;
	half2 highAltScale2;
	half2 highAltScale3;
	half highAltitudeCloudiness;
	half highAltInfluence1;
	half highAltInfluence2;
	half highAltInfluence3;
	
	int lightingDistance;
	int planetRadius;
	
	half heightDensityInfluence;
	half cloudinessDensityInfluence;
	
	Texture2D weathermapTex;
};

struct RayData
{
	half3 rayOrigin;
	half3 rayPosition;
	half3 rayDirection;
	half relativeDepth;
	half rayDepth;
	half meanStepSize;
	half noiseAdjustment;
	half noiseIntensity;
};


half GetDistantCloudiness(half cloudiness, half rayDepth, AtmosphereData atmosData)
{
	if(rayDepth < atmosData.distantCoverageDepth)
		return cloudiness;
	
	half t = Remap01(atmosData.distantCoverageDepth, atmosData.cloudFadeDistance, rayDepth);
	t = 1.0 - t;
	t *= t;
	t = 1.0 - t;
	return lerp(cloudiness, atmosData.distantCoverageAmount, t);
}


half GetCloudShape2D(StaticMaterialData m, float3 rayPosition, AtmosphereData atmosData, int mip)
{
	half2 uv = rayPosition.xz * 0.00001;
	half timeOffset = _Time.y * 0.0001;
	half2 uv1 = (uv + timeOffset * m.highAltOffset1) * m.highAltScale1;
	
	
	half coverage = m.highAltTex1.SampleLevel(linear_repeat_sampler, uv1, mip).r;
	coverage = Remap01(1.0 - m.highAltitudeCloudiness, 1.0, coverage);
	
	if(coverage < EPSILON)
		return 0;
	
	half2 uv2 = (uv + timeOffset * m.highAltOffset2) * m.highAltScale2;
	half2 uv3 = (uv + timeOffset * m.highAltOffset3) * m.highAltScale3;
	half value = m.highAltTex2.SampleLevel(linear_repeat_sampler, uv2, mip).r;
	value += m.highAltTex3.SampleLevel(linear_repeat_sampler, uv3, mip).r;
	value *= 0.5;
	
	value = lerp(value, 1.0, m.highAltitudeCloudiness);
	value = Remap01(1.0 - coverage, 1.0, value);
	
	return value;
}


half2 GetWeathermapUV(half3 rayPosition, half3 rayOrigin, bool doFloatingOrigin)
{
	half2 UV = rayPosition.xz;
	if (doFloatingOrigin)
		UV -= rayOrigin.xz;
	
	UV *= 0.0001;
	UV += _CLOUD_WEATHERMAP_VELOCITY * _Time.y * 0.01;
	
	return UV;
}


half GetWeathermap(StaticMaterialData materialData, RayData rayData, half cloudinessAtPoint)
{
	half2 weathermapUV;
	half weathermapSample = 0;
	
	if (_USE_CLOUD_WEATHERMAP_TEX)
	{
		weathermapUV = GetWeathermapUV(rayData.rayPosition, materialData.rayOrigin, true);
		weathermapSample = materialData.weathermapTex.SampleLevel(linear_repeat_sampler, weathermapUV, 0).r;
	}
	else
	{
		weathermapUV = GetWeathermapUV(rayData.rayPosition, materialData.rayOrigin, false);
		weathermapUV *= _CLOUD_WEATHERMAP_SCALE;
		weathermapSample = GetLayeredPerlinNoise(3, weathermapUV, 0.3, 2.0);
	}
	
	return Remap01(1.0 - cloudinessAtPoint, 1.0, weathermapSample);
}

half GetCloudDensityByHeight(half height01)
{
	return saturate(_CLOUD_DENSITY_CURVE_TEX.SampleLevel(linear_clamp_sampler, float2(height01, 0), 0).r);
}

half GetCloudShapeVolumetric(StaticMaterialData m, RayData rayData, AtmosphereData atmosData, half weathermap, half densityAtHeight, half height01, half cloudinessAtPoint, int mip)
{
	half coverage = weathermap * densityAtHeight;
	
	half3 uvw = rayData.rayPosition * 0.000005;
	
	// Sample Curl
	half curlOffset = m.curlTimescale * _Time.x;
	half3 curlUV = uvw * m.curlScale;
	curlUV += curlOffset;
	half3 curlSampleXZ = m.curlNoise.SampleLevel(linear_repeat_sampler, curlUV.xz, mip).rgb;
	half3 curlSampleXY = m.curlNoise.SampleLevel(linear_repeat_sampler, curlUV.xy, mip).rgb;
	half3 curlSample = (curlSampleXY + curlSampleXZ) * 0.5;
	curlSample = (curlSample - 0.5) * 2.0;
	curlSample *= m.curlStrength;
	
	
	// Sample Base
	half3 heightOffset = (height01 * height01 * m.baseTimescale);
	half3 baseOffset = m.baseTimescale * _Time.y;
	half3 baseUVW = m.baseScale * (uvw + baseOffset);
	baseUVW += curlSample + heightOffset;
	
	half4 baseSample = m.baseTexture.SampleLevel(linear_repeat_sampler, baseUVW, mip).rgba;
	half baseVal = (baseSample.r * m.baseRGBAInf.r + baseSample.g * m.baseRGBAInf.g + baseSample.b * m.baseRGBAInf.b + baseSample.a * m.baseRGBAInf.a);
	baseVal *= baseVal;
	baseVal = lerp(baseVal, 1.0, cloudinessAtPoint);
	
	half value = Remap01(1.0 - baseVal, 1.0, coverage);
	
	
	if (value < EPSILON)
		return 0;
	
	
	if (rayData.rayDepth < 15000)
	{
		// Sample Detail 1
		if (m.detail1Strength > EPSILON)
		{
			half3 detail1Offset = (m.detail1Timescale * _Time.y) * 0.0001;
			half3 detail1UVW = m.detail1Scale * (uvw + detail1Offset);
	
			half4 detail1Sample = m.detail1Texture.SampleLevel(linear_repeat_sampler, detail1UVW, mip).rgba;
			half valueDetail = detail1Sample.r * m.detail1RGBAInf.r + detail1Sample.g * m.detail1RGBAInf.g + detail1Sample.b * m.detail1RGBAInf.b + detail1Sample.a * m.detail1RGBAInf.a;
			if (valueDetail > EPSILON)
			{
				valueDetail *= valueDetail;
				valueDetail = lerp(valueDetail, 1.0, cloudinessAtPoint);
				valueDetail = lerp(valueDetail, 1.0, 1.0 - m.detail1Strength);
				half falloff = saturate(RemapUnclamped(m.detail1HeightRemap.x, m.detail1HeightRemap.y, 0.0, 1.0, height01));
				valueDetail = lerp(1.0, valueDetail, falloff);
			
				value = Remap01(1.0 - valueDetail, 1.0, value);
			}
		}
	}
	
	
	if (value < EPSILON)
		return 0;
	
	
	value *= lerp(1.0, height01, m.heightDensityInfluence);
	value *= lerp(1.0, cloudinessAtPoint, m.cloudinessDensityInfluence);
	value *= Remap(atmosData.cloudFadeDistance * 0.8, atmosData.cloudFadeDistance, 1.0, 0.0, rayData.rayDepth);
	value *= 5.0;
	return saturate(value);
}

half BeerLambert(half absorptionCoefficient, half stepSize, half density)
{
	return exp(-absorptionCoefficient * stepSize * density);
}

half HenyeyGreenstein(half cos_angle, half eccentricity)
{
	half e2 = eccentricity * eccentricity;
	float f = abs((1.0 + e2 - 2.0 * eccentricity * cos_angle));
	return ((1.0 - e2) / pow(f, 1.5)) / 4.0 * 3.1416;
}

struct OSLightingData
{
	half3 baseLighting;
	half3 outScatterLighting;
};

OSLightingData GetLightingDataVolumetric(StaticMaterialData materialData, RayData rayData, AtmosphereData atmosData, int mip)
{
	OSLightingData data;
	
	half3 cachedRayOrigin = rayData.rayPosition;
	half r = rand2dTo1d(half2(rayData.rayDepth, _Time.x));
	r = lerp(0, r, rayData.noiseIntensity);
	r = Remap(0.0, 1.0, 2.0, 3.0, r);
	
	int sampleCount = 5;
	
	float t0, t1;
	IntersectSphere(rayData.rayPosition, materialData.sunPos, materialData.planetRadius + atmosData.atmosHeight + atmosData.atmosThickness, _PLANET_CENTER, t0, t1);
	
	half lightingDistanceToSample = min(t0, materialData.lightingDistance);
	
	half totalDensity = 0.0;
	half currentStepSize = 0.0;
	half3 extinction;
	half sAmp = 1.0;
	half sGain = 0.3;
	half3 densityAdj = 0;
	for (int i = 1; i <= sampleCount; i++)
	{
		half lightSample = half(i) / half(sampleCount + 1);
		lightSample = pow(abs(lightSample), r);	
		half totalDistance = lightSample * lightingDistanceToSample;
		
		
		rayData.rayPosition = cachedRayOrigin + (materialData.sunPos * totalDistance);
		
		
		half height01 = GetHeight01(cachedRayOrigin, rayData.rayPosition, atmosData.atmosThickness, materialData.planetRadius, atmosData.atmosHeight);
		half cloudinessAtPoint = GetDistantCloudiness(materialData.cloudiness, distance(rayData.rayPosition, rayData.rayOrigin), atmosData);
		half weather = GetWeathermap(materialData, rayData, cloudinessAtPoint);
		half densityAtHeight = GetCloudDensityByHeight(height01);
		half cloudDensity = GetCloudShapeVolumetric(materialData, rayData, atmosData, weather, densityAtHeight, height01, cloudinessAtPoint, mip);
		
		totalDensity += cloudDensity * totalDistance;
		
		extinction = materialData.extinction * sAmp;
		sAmp *= sGain;
		densityAdj += extinction * totalDensity;
	}
	
	data.baseLighting = exp(-densityAdj);
	
	data.outScatterLighting = 0.0;
	
	half msAmp = 1.0;
	for (int octaveCounter = 1; octaveCounter < materialData.multipleScatteringOctaves; octaveCounter++)
	{
		msAmp *= materialData.multipleScatteringAmpGain;
		totalDensity *= materialData.multipleScatteringDensityGain;
		data.outScatterLighting += exp(-materialData.extinction * totalDensity) * msAmp;
	}
	
	return data;
}

half GetPlaneOffset(float dist, float stepSize)
{
	float planeOffset = dist / stepSize;
	planeOffset = frac(dist);
	return planeOffset * stepSize;
}

half GetAmbientDensity(StaticMaterialData materialData, RayData rayData, AtmosphereData atmosData, half weather, half cloudinessAtPoint, int mip)
{
	half r = rand2dTo1d(half2(_Time.x, rayData.rayDepth));
	half step = r * atmosData.atmosThickness * 0.1;
	
	rayData.rayPosition += float3(0, 1, 0) * step;
	
	half height01 = GetHeight01(rayData.rayOrigin, rayData.rayPosition, atmosData.atmosThickness, materialData.planetRadius, atmosData.atmosHeight);
	half density = GetCloudDensityByHeight(height01);
	half d = GetCloudShapeVolumetric(materialData, rayData, atmosData, weather, density, height01, cloudinessAtPoint, mip);
	
	half ambientDensity = exp(-d * materialData.extinction.r * step * 0.3);
	
	return ambientDensity;
}


void ApplyLighting(StaticMaterialData materialData, half fogPower, half accumulatedDepth, half3 lightEnergy, half ambientEnergy, half baseEnergy, inout half3 cloudColor)
{
	half fogAttenuation = 1.0 - (1.0 / exp(accumulatedDepth * fogPower));
	cloudColor += materialData.fogColor * fogAttenuation * baseEnergy;
	cloudColor += lightEnergy * materialData.sunColor * materialData.sunIntensity * (1.0 - fogAttenuation);
	cloudColor += materialData.ambientColor * ambientEnergy * materialData.ambientExposure * (1.0 - fogAttenuation);
	
}

void SampleHighAltitudeClouds(RayData rayData, StaticMaterialData materialData, AtmosphereData atmosData, float highAltitudeHitDistance, inout float alpha, inout float3 cloudColor)
{
	half simulatedStepLength = 100.0;
	
	half fadeOut = Remap(highAltitudeFadeDistance * 0.8, highAltitudeFadeDistance, 1.0, 0.0, highAltitudeHitDistance);
	fadeOut = saturate(fadeOut);
	
	half3 lightEnergy = 0;
	half baseEnergy = 0;
	half ambientEnergy = 0;
	for (int a = 0; a < 4; a++)
	{
		rayData.rayPosition = rayData.rayOrigin + rayData.rayDirection * highAltitudeHitDistance + simulatedStepLength * a;
		half valueAtPoint = GetCloudShape2D(materialData, rayData.rayPosition, atmosData, a) * fadeOut;
		
		
		if (valueAtPoint > EPSILON)
		{
			half priorAlpha = alpha;
					
			half3 sampleExtinction = materialData.highAltExtinction * valueAtPoint;
		
			half transmittance = exp(-sampleExtinction.r * simulatedStepLength);
			alpha *= transmittance;
			half3 invSampleExtinction = 1.0 / sampleExtinction;
			
			half totalDensity = 0;
			float stepLength = 0;
	
			if (materialData.sunIntensity > EPSILON && length(materialData.sunColor) > EPSILON)
			{	
				half3 extinction;
				half sAmp = 1.0;
				half sGain = 0.3;
				half3 densityAdj = 0;
				for (int i = 1; i < 4; i++)
				{
					stepLength += 50.0 * i;
					float3 lightRayPos = rayData.rayPosition + materialData.sunPos * stepLength;
					half lightSample = GetCloudShape2D(materialData, lightRayPos, atmosData, i);
					totalDensity += lightSample * stepLength;
					extinction = materialData.highAltExtinction * sAmp;
					sAmp *= sGain;
					densityAdj += extinction * totalDensity;
				}
					
				half3 lighting = exp(-densityAdj);
				
				half amp = 1.0;
				half outScattering = 0.0;
	
				for (int octaveCounter = 1; octaveCounter < materialData.multipleScatteringOctaves; octaveCounter++)
				{
					amp *= materialData.multipleScatteringAmpGain;
					totalDensity *= materialData.multipleScatteringDensityGain;
					outScattering += exp(-totalDensity * materialData.highAltExtinction) * amp;
				}
		
				half3 lightData = ((lighting * materialData.HG) + outScattering) * sampleExtinction;
				half3 intScatter = (lightData - (lightData * transmittance)) * invSampleExtinction;
				lightEnergy += intScatter * priorAlpha;
			}
			
			if (materialData.ambientExposure > EPSILON)
			{
				half intAmbient = 1.0 * sampleExtinction.r;
				intAmbient = (intAmbient - (intAmbient * transmittance)) * invSampleExtinction.r;
				ambientEnergy += intAmbient * priorAlpha;
			}
			
			
			half energData = (sampleExtinction.r - (sampleExtinction.r * transmittance)) * invSampleExtinction.r;
			baseEnergy += energData * priorAlpha;
		}
	}
	
	ApplyLighting(materialData, materialData.fogPower * 0.3, highAltitudeHitDistance, lightEnergy, ambientEnergy, baseEnergy, cloudColor);
}


// Scattering Values (source: https://journals.ametsoc.org/view/journals/bams/79/5/1520-0477_1998_079_0831_opoaac_2_0_co_2.xml):
// Cumulus: 50 - 120
// Stratus: 40 - 60
// Cirrus: 0.1 - 0.7
// Wavelength-specific Scattering Distribution for Cloudy medium : https://www.patarnott.com/satsens/pdf/opticalPropertiesCloudsReview.pdf

void SampleClouds_half(SamplerState CloudSampler, half3 RayOrigin, half3 SunPos, Texture3D BaseTexture3D, Texture3D DetailTexture3D, half AlphaAccumulation, half Cloudiness, half3 SunColor, half3 AmbientColor, half BlueNoise, half NumSteps, half CloudLayerHeight, half CloudLayerThickness, half CloudFadeDistance, half3 BaseLayerScale, half BlueNoiseStrength, half Detail1Strength, half3 BaseTimescale, half3 Detail1Timescale, half3 Detail1Scale, half FogPower, half SceneDepthEye, half3 VolumetricsFogColor, half SceneDepth01, half LightingDistance, half PlanetRadius, Texture2D CurlNoise, half CurlScale, half CurlStrength, half CurlTimescale, half CurlAdjustmentBase, half SunIntensity, Texture3D DetailTexture3D2, half3 Detail2Scale, half3 Detail2Timescale, half Detail2Intensity, half AmbientExposure, half DistantCoverageDepth, half DistantCoverageAmount, half2 Detail1HeightRemap, bool Detail1Invert, half2 Detail2HeightRemap, bool Detail2Invert, half HeightDensityInfluence, half CloudinessDensityInfluence, Texture2D HighAltitudeCloudsTexture1, Texture2D HighAltitudeCloudsTexture2, Texture2D HighAltitudeCloudsTexture3, half2 HighAltOffset1, half2 HighAltOffset2, half2 HighAltOffset3, half2 HighAltScale1, half2 HighAltScale2, half2 HighAltScale3, half HighAltCloudiness, half HighAltInfluence1, half HighAltInfluence2, half HighAltInfluence3, half4 BaseRGBAInfluence, half4 Detail1RGBAInfluence, half4 Detail2RGBAInfluence, half HighAltitudeAlphaAccumulation, bool RenderLocal, half MultipleScatteringAmpGain, half MultipleScatteringDensityGain, int MultipleScatteringOctaves, half HGForward, half HGBack, half HGBlend, half HGStrength, Texture2D WeathermapTex, SamplerState WeathermapSampler, half2 UV, bool jitterUVs, out half4 cloudData, out half3 cloudColor, out half alpha)
{
	/*
	Optimizations:
	1. Sample weathermap rarely until you find a cloud, then sample the detail.
	*/
	
	#ifdef SHADERGRAPH_PREVIEW
	alpha = 1.0;
	cloudColor = half3(0, 0, 0);
	cloudData = half4(cloudColor, alpha);
	#endif
	alpha = 1.0;
	cloudColor = half3(0, 0, 0);
	cloudData = half4(cloudColor, alpha);
	
	// Material Data Setup
	StaticMaterialData materialData;
	
	materialData.fogSampler = CloudSampler;
	
	materialData.rayOrigin = RayOrigin;
	
	materialData.sunPos = normalize(SunPos);
	materialData.sunColor = GetMainLight().color * SunColor;
	materialData.sunIntensity = SunIntensity;
	materialData.ambientColor = Saturation(AmbientColor, 0.6);
	materialData.ambientExposure = AmbientExposure;
	materialData.fogColor = VolumetricsFogColor;
	
	materialData.cloudiness = Cloudiness;
	materialData.alphaAccumulation = AlphaAccumulation * 0.01;
	materialData.baseRGBAInf = BaseRGBAInfluence;
	
	materialData.multipleScatteringAmpGain = MultipleScatteringAmpGain;
	materialData.multipleScatteringDensityGain = MultipleScatteringDensityGain;
	materialData.multipleScatteringOctaves = MultipleScatteringOctaves;
	
	materialData.baseTexture = BaseTexture3D;
	materialData.baseScale = BaseLayerScale;
	materialData.baseTimescale = BaseTimescale * 0.0001;
	
	materialData.curlNoise = CurlNoise;
	
	materialData.detail1Texture = DetailTexture3D;
	materialData.detail1Scale = Detail1Scale;
	materialData.detail1Strength = Detail1Strength;
	materialData.detail1Timescale = Detail1Timescale;
	materialData.detail1Invert = Detail1Invert;
	materialData.detail1HeightRemap = Detail1HeightRemap;
	materialData.detail1RGBAInf = Detail1RGBAInfluence;
	
	
	materialData.lightingDistance = LightingDistance;
	materialData.planetRadius = PlanetRadius * _CONVERT_KM_TO_M;
	
	materialData.curlScale = CurlScale;
	materialData.curlStrength = CurlStrength * 0.01;
	materialData.curlTimescale = CurlTimescale * 0.005;
	
	materialData.heightDensityInfluence = HeightDensityInfluence;
	materialData.cloudinessDensityInfluence = CloudinessDensityInfluence;
	
	materialData.highAltTex1 = HighAltitudeCloudsTexture1;
	materialData.highAltTex2 = HighAltitudeCloudsTexture2;
	materialData.highAltTex3 = HighAltitudeCloudsTexture3;
	materialData.highAltitudeAlphaAccumulation = HighAltitudeAlphaAccumulation * 0.001;
	materialData.highAltitudeCloudiness = HighAltCloudiness;
	materialData.highAltOffset1 = HighAltOffset1;
	materialData.highAltOffset2 = HighAltOffset2;
	materialData.highAltOffset3 = HighAltOffset3;
	materialData.highAltScale1 = HighAltScale1;
	materialData.highAltScale2 = HighAltScale2;
	materialData.highAltScale3 = HighAltScale3;
	materialData.highAltInfluence1 = HighAltInfluence1;
	materialData.highAltInfluence2 = HighAltInfluence2;
	materialData.highAltInfluence3 = HighAltInfluence3;
	
	materialData.weathermapTex = WeathermapTex;
	
	materialData.renderLocal = RenderLocal;
	
	materialData.fogPower = FogPower * 0.0001;
	
	// Other properties...
	half accDepthSamples = 0;
	half ambientEnergy = 0.0;
	half baseEnergy = 0.0;
	half3 lightEnergy = 0.0;
	half valueAtPoint = 0;
	int mip = 0;
	
	
	
	
	// Depth, Ray, and UV Setup
	
	RayData rayData;
	float2 jitteredUV = UV;
	
	if (jitterUVs)
	{
		half2 texCoord = GetTexCoordSize(_CLOUD_RENDER_SCALE);
		float2 jitter = texCoord * (rand2dTo2d(UV + _Time.y) - 0.5);
		jitteredUV += jitter;
	}
	
	half3 viewVector = mul(unity_CameraInvProjection, float4(jitteredUV * 2 - 1, 0.0, -1)).xyz;
	viewVector = mul(unity_CameraToWorld, half4(viewVector, 0.0)).xyz;
	half viewLength = length(viewVector);
	
	half3 rayDir = viewVector / viewLength;
	
	rayData.rayDirection = rayDir;
	rayData.rayOrigin = RayOrigin;
	
	
	
	// Set up depth properties
	half depthRaw = _DitheredDepthTexture.SampleLevel(point_clamp_sampler, UV, 0).r;
	half depth01 = Linear01Depth(depthRaw, _ZBufferParams);
	half depthEye = LinearEyeDepth(depthRaw, _ZBufferParams);
	half realDepthEye = depthEye * viewLength;
	
	
	// Set up extinction properties
	// Albedo of cloudy material is near to 1. Given that extinction coefficient is calculated as absorption + scattering, when absorption = 0 then extinction = scattering.
	materialData.extinction = materialData.alphaAccumulation * _CLOUD_ALBEDO;
	materialData.highAltExtinction = materialData.highAltitudeAlphaAccumulation * _CLOUD_ALBEDO;
	
	
	// Cloud Parameter Setup
	AtmosphereData atmosData;
	atmosData.atmosThickness = CloudLayerThickness * _CONVERT_KM_TO_M;
	atmosData.atmosHeight = CloudLayerHeight * _CONVERT_KM_TO_M;
	atmosData.cloudFadeDistance = CloudFadeDistance * _CONVERT_KM_TO_M;
	atmosData.distantCoverageDepth = DistantCoverageDepth * _CONVERT_KM_TO_M;
	atmosData.distantCoverageAmount = DistantCoverageAmount;
	_PLANET_CENTER = half3(rayData.rayOrigin.x, -materialData.planetRadius, rayData.rayOrigin.z);
	
	
	// Lower Atmosphere Decisioning
	bool sampleLowAltitudeClouds = false;
	AtmosHitData hitData;
	hitData.nearDist = 0;
	hitData.didHit = false;
	if (materialData.cloudiness > EPSILON && materialData.extinction.r > EPSILON_SMALL)
	{
		hitData = AtmosphereIntersection(rayData.rayOrigin, rayData.rayDirection, atmosData.atmosHeight, materialData.planetRadius, atmosData.atmosThickness, atmosData.cloudFadeDistance);
		if (hitData.didHit && hitData.nearDist < atmosData.cloudFadeDistance)
			sampleLowAltitudeClouds = true;
	
		if (hitData.nearDist > realDepthEye && depth01 < 1.00)
			sampleLowAltitudeClouds = false;
	}
	
	
	// Upper Atmosphere Decisioning
	bool doSampleHighAlt = false;
	float nearHitUpperAtmosphere = 0;
	float farHitUpperAtmosphere = 0;
	if (materialData.highAltitudeCloudiness > EPSILON && materialData.highAltExtinction.r > EPSILON_SMALL)
	{
		half radius = materialData.planetRadius + atmosData.atmosHeight + atmosData.atmosThickness + _CIRRUS_CLOUD_HEIGHT;
		
		bool didHitUpperAtmosphere = IntersectSphere(rayData.rayOrigin, rayData.rayDirection, radius, _PLANET_CENTER, nearHitUpperAtmosphere, farHitUpperAtmosphere);
		rayData.rayDepth = nearHitUpperAtmosphere;
		
		if (didHitUpperAtmosphere)
			doSampleHighAlt = true;
		
		if (materialData.renderLocal && rayData.rayDepth > realDepthEye && depth01 < 1.0)
			doSampleHighAlt = false;
		
		if (rayData.rayDepth > highAltitudeFadeDistance)
			doSampleHighAlt = false;
	
		if ((rayData.rayOrigin + (rayData.rayDirection * rayData.rayDepth)).y < 0)
			doSampleHighAlt = false;
	}
	
	
	// Instead, check to see if the ray crosses the low altitude cloud layer on the way to the high altitude cloud layer.
	// If it does, then render the high altitude clouds second.
	int highAltPassId = 0;
	if (hitData.nearDist < nearHitUpperAtmosphere)
	{
		highAltPassId = 1;
	}
	
	if (doSampleHighAlt || sampleLowAltitudeClouds)
	{
		// Physically realistic HGForward Value is ~0.6. HGBack is a purely artistic factor.
		half cos_angle = dot(materialData.sunPos, rayData.rayDirection);
		HGForward = HenyeyGreenstein(cos_angle, HGForward);
		HGBack = HenyeyGreenstein(cos_angle, HGBack);
		half HG = lerp(HGForward, HGBack, 0.5);
		HG = lerp(1.0, HG, saturate(HGStrength));
		materialData.HG = HG;
	}
	
	
	if (doSampleHighAlt && highAltPassId == 0)
	{
		SampleHighAltitudeClouds(rayData, materialData, atmosData, nearHitUpperAtmosphere, alpha, cloudColor);
	}
	
	int r = 0;
	int g = 0;
	half sampleDepth = 0;
	
	
	if (sampleLowAltitudeClouds)
	{
		sampleDepth = hitData.nearDist;
		half maxDepth = min(hitData.farDist, atmosData.cloudFadeDistance);
		if (materialData.renderLocal && depth01 < 1.0)
		{
			maxDepth = min(maxDepth, realDepthEye);
		}
		
		half totalDepth = maxDepth - sampleDepth;
		
		bool accountedForDoubleIntersect = true;
		
		if (hitData.doubleIntersection)
		{
			totalDepth = totalDepth + (hitData.farDist2 - hitData.nearDist2);
			accountedForDoubleIntersect = false;
		}
		
		rayData.meanStepSize = totalDepth / NumSteps;

		#define _ONE_OVER_SIXTEEN 0.0625
		half2 repeat = ceil((_ScreenParams.xy * _CLOUD_RENDER_SCALE) * _ONE_OVER_SIXTEEN);
		BlueNoise = _BLUE_NOISE.SampleLevel(point_repeat_sampler, UV * repeat, 0).r;
		rayData.noiseIntensity = BlueNoiseStrength;
		rayData.noiseAdjustment = rayData.meanStepSize * BlueNoise * rayData.noiseIntensity;
		sampleDepth += rayData.noiseAdjustment;
		
		rayData.rayDepth = sampleDepth;
		
		
		for (int i = 1; i <= int(NumSteps); i++)
		{
			r++;
			rayData.rayPosition = rayData.rayOrigin + (rayData.rayDirection * rayData.rayDepth);
			
			if (rayData.rayPosition.y < 0.0)
				break;
			
			if (materialData.renderLocal && depth01 < 1.0 && rayData.rayDepth > realDepthEye)
				break;
			
			mip = 0;
			
			if(rayData.rayDepth > 5000)
				mip = 1;
			
			if(rayData.rayDepth > 12500)
				mip = 2;
			
			if(rayData.rayDepth > 25000)
				mip = 3;
			
			
			valueAtPoint = 0;
			
			half height01 = 0;
			half density = 0;
			half cloudinessAtPoint = GetDistantCloudiness(materialData.cloudiness, rayData.rayDepth, atmosData);
			half weather = GetWeathermap(materialData, rayData, cloudinessAtPoint);
			
			if (weather > EPSILON)
			{
				height01 = GetHeight01(rayData.rayOrigin, rayData.rayPosition, atmosData.atmosThickness, materialData.planetRadius, atmosData.atmosHeight);
				density = GetCloudDensityByHeight(height01);
				valueAtPoint = GetCloudShapeVolumetric(materialData, rayData, atmosData, weather, density, height01, cloudinessAtPoint, mip);
			}
			
			
			
			// If the cloud exists at this point, sample the lighting
			if (valueAtPoint > EPSILON)
			{
				g++;
				
				half priorAlpha = alpha;
				half3 sampleExtinction = materialData.extinction * valueAtPoint;
				half transmittance = exp(-sampleExtinction.r * rayData.meanStepSize);
				alpha *= transmittance;
				
				half3 invSampleExtinction = 1.0 / sampleExtinction;
				
				// Directives
				bool sampleDirectLighting = false;
				bool sampleAmbientLighting = false;
				
				if (materialData.sunIntensity > EPSILON && length(materialData.sunColor) > EPSILON)
					sampleDirectLighting = true;
				
				if (materialData.ambientExposure > EPSILON)
					sampleAmbientLighting = true;
				
				
				// Direct Lighting
				if (sampleDirectLighting)
				{
					// In-Scattering
					 
					/* <-- Note -->
					Tried using the transmittance value for the local density as well. 
					When calculated using the below algorithm, it yielded almost identical results as the implemented method.
					
					half loddedTransmittance = exp(-loddedVal * sampleExtinction.r * rayData.meanStepSize * 0.1);
					loddedTransmittance = 1.0 - loddedTransmittance;
					loddedTransmittance = saturate(loddedTransmittance * 20.0);
					loddedTransmittance = Remap(0, 1, 0.4, 1.0, loddedTransmittance);
					*/
					
					half loddedVal = GetCloudShapeVolumetric(materialData, rayData, atmosData, weather, density, height01, cloudinessAtPoint, mip + 2);
					half inScattering = loddedVal * loddedVal;
					inScattering *= 50.0;
					inScattering = saturate(inScattering);
					
					// Remaps from [0,1] to [0.4, 1.0]
					inScattering *= 0.6;
					inScattering += 0.4;
					
					
					OSLightingData lightingData = GetLightingDataVolumetric(materialData, rayData, atmosData, mip);
					half3 lightData = ((lightingData.baseLighting * materialData.HG) + lightingData.outScatterLighting) * sampleExtinction * (inScattering * _CLOUD_ALBEDO);
					half3 intScatter = (lightData - (lightData * transmittance)) * invSampleExtinction;
					lightEnergy += intScatter * priorAlpha;
				}
				
				//Ambient
				if (sampleAmbientLighting)
				{
					half ambHeight = Remap(0.1, 0.7, 0.6, 1.0, height01);
					half ambDensity = GetAmbientDensity(materialData, rayData, atmosData, weather, cloudinessAtPoint, mip + 2);
					
					half ambientData = sampleExtinction.r * (ambDensity + ambHeight) * 0.5;
					half intAmb = (ambientData - (ambientData * transmittance)) * invSampleExtinction.r;
					ambientEnergy += intAmb * priorAlpha;
				}
				
				
				//Fog
				half energData = (sampleExtinction.r - (sampleExtinction.r * transmittance)) * invSampleExtinction.r;
				baseEnergy += energData * priorAlpha;
				
				if (accDepthSamples <= EPSILON)
				{
					accDepthSamples = rayData.rayDepth;
				}
				else
				{
					accDepthSamples += (rayData.rayDepth - accDepthSamples) * energData * priorAlpha;
				}
			}
			
			if (alpha <= 0.01)
			{
				break;
			}
			
			rayData.rayDepth += rayData.meanStepSize;
			
			
			// Handle Double Intersect if needed.
			if (rayData.rayDepth > hitData.farDist && !accountedForDoubleIntersect)
			{
				rayData.rayDepth = hitData.nearDist2;
				accountedForDoubleIntersect = true;
			}
		}
		
		if (baseEnergy > EPSILON)
		{
			ApplyLighting(materialData, materialData.fogPower, accDepthSamples, lightEnergy, ambientEnergy, baseEnergy, cloudColor);
		}
	}
	
	if (doSampleHighAlt && highAltPassId == 1)
	{
		SampleHighAltitudeClouds(rayData, materialData, atmosData, nearHitUpperAtmosphere, alpha, cloudColor);
	}
	
	
	//#define DEBUG_CLOUDS
	
	#ifdef DEBUG_CLOUDS
	
	cloudColor = half3(0, 0, 0);
	cloudColor.r = float(r) / NumSteps;
	cloudColor.g = float(g) / NumSteps;
	alpha = 0;
	#endif
	
	cloudData = half4(cloudColor, alpha);
	
}

#endif