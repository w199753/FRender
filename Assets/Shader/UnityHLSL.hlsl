
#ifndef __UNITY__HLSL__
#define __UNITY__HLSL__

#define UNITY_PI			3.14159265359f
#define UNITY_TWO_PI		6.28318530718f
#define UNITY_FOUR_PI		12.56637061436f
#define UNITY_INV_PI		0.31830988618f
#define UNITY_INV_TWO_PI	0.15915494309f
#define UNITY_INV_FOUR_PI	0.07957747155f
#define UNITY_HALF_PI		1.57079632679f
#define UNITY_INV_HALF_PI	0.636619772367f

#ifdef UNITY_COLORSPACE_GAMMA
#define unity_ColorSpaceGrey fixed4(0.5, 0.5, 0.5, 0.5)
#define unity_ColorSpaceDouble fixed4(2.0, 2.0, 2.0, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.220916301, 0.220916301, 0.220916301, 1.0 - 0.220916301)
#define unity_ColorSpaceLuminance half4(0.22, 0.707, 0.071, 0.0) // Legacy: alpha is set to 0.0 to specify gamma mode
#else // Linear values
#define unity_ColorSpaceGrey fixed4(0.214041144, 0.214041144, 0.214041144, 0.5)
#define unity_ColorSpaceDouble fixed4(4.59479380, 4.59479380, 4.59479380, 2.0)
#define unity_ColorSpaceDielectricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
#define unity_ColorSpaceLuminance half4(0.0396819152, 0.458021790, 0.00609653955, 1.0) // Legacy: alpha is set to 1.0 to specify linear mode
#endif



#endif