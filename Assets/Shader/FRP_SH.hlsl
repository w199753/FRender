
#ifndef __FRP__SH__
#define __FRP__SH__

StructuredBuffer<float3> sh_coeff;

float4x4 rotRLight;
float4x4 rotGLight;
float4x4 rotBLight;
float shExposure;
struct SH9
{
	float c[9];
};


SH9 SHCosineLobe(float3 normal)
{
	const float Pi = 3.141592654f;
const float CosineA0 = 1;
const float CosineA1 = 1;
const float CosineA2 = 1;

	float x = normal.x; float y = normal.y; float z = normal.z;
	float x2 = x * x; float y2 = y * y; float z2 = z * z;
	SH9 sh;
	sh.c[0] = 0.28209478 * CosineA0;							//1/2*sqrt(1/Pi)
	sh.c[1] = 0.48860251 * y * CosineA1;						//sqrt(3/(4Pi))
	sh.c[2] = 0.48860251 * z * CosineA1;
	sh.c[3] = 0.48860251 * x * CosineA1;
	sh.c[4] = CosineA2 * 1.09254843 * x * y;					//1/2*sqrt(15/Pi)
	sh.c[5] = CosineA2 * 1.09254843 * y * z;					
	sh.c[6] = CosineA2 * 0.31539156 * (-x2 - y2 + 2 * z2);		//1/4*sqrt(5/Pi)
	sh.c[7] = CosineA2 * 1.09254843 * z * x;
	sh.c[8] = CosineA2 * 0.54627422 * (x2 - y2);				//1/4*sqrt(15/Pi)

	return sh;
}

SH9 SHCosineLobe11(float3 normal)
{
	const float Pi = 3.141592654f;
	const float CosineA0 = 2 * Pi / (1 + 1 * shExposure                          );;
	const float CosineA1 = 2 * PI / (2 + 1 * shExposure                          );
	const float CosineA2 = 2 * PI / (3 + 4 * shExposure + shExposure*shExposure        );

	float x = normal.x; float y = normal.y; float z = normal.z;
	float x2 = x * x; float y2 = y * y; float z2 = z * z;
	SH9 sh;
	sh.c[0] = 0.28209478 * CosineA0;							//1/2*sqrt(1/Pi)
	sh.c[1] = 0.48860251 * y * CosineA1;						//sqrt(3/(4Pi))
	sh.c[2] = 0.48860251 * z * CosineA1;
	sh.c[3] = 0.48860251 * x * CosineA1;
	sh.c[4] = CosineA2 * 1.09254843 * x * y;					//1/2*sqrt(15/Pi)
	sh.c[5] = CosineA2 * 1.09254843 * y * z;					
	sh.c[6] = CosineA2 * 0.31539156 * (-x2 - y2 + 2 * z2);		//1/4*sqrt(5/Pi)
	sh.c[7] = CosineA2 * 1.09254843 * z * x;
	sh.c[8] = CosineA2 * 0.54627422 * (x2 - y2);				//1/4*sqrt(15/Pi)

	return sh;
}

float3 CalVertexSH(float3 N)
{
    N = normalize(N);
	SH9 sh = SHCosineLobe11(N);
	float3 res = 0;
	float r=0;
	float g=0;
	float b=0;
	for (int i = 0; i < 9; i++)
	{
		int j = i/3;
		int k = i%3;
		r = rotRLight[j][k]*sh_coeff[i].x;
		g = rotGLight[j][k]*sh_coeff[i].y;
		b = rotBLight[j][k]*sh_coeff[i].z;
		r = sh_coeff[i].x;
		g = sh_coeff[i].y;
		b = sh_coeff[i].z;
		float c = sh.c[i];
		float hh = 1;
		//if(i==0)
		//hh = 0.8862;
		//if(i>=1&&i<=3)
		//hh = 1.0233;
		//else if(i>=4&&i<=8)
		//hh = 0.4954;
		float3 co = float3(r,g,b);
		res += c * co *hh;
	}
	return res / 3.141592654f;
}

#endif