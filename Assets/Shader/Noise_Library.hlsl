#ifndef _Noise_Library_
#define _Noise_Library_


float RandN(float2 pos, float2 random)
{
	return frac(sin(dot(pos.xy + random, float2(12.9898, 78.233))) * 43758.5453);
}

float2 RandN2(float2 pos, float2 random)
{
	return frac(sin(dot(pos.xy + random, float2(12.9898, 78.233))) * float2(43758.5453, 28001.8384));
}

float RandS(float2 pos, float2 random)
{
	return RandN(pos, random) * 2.0 - 1.0;
}

float InterleavedGradientNoise (float2 pos, float2 random)
{
	float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
	return frac(magic.z * frac(dot(pos.xy + random, magic.xy)));
}

float Step1(float2 uv,float n)
{
	float 
	a = 1.0,
	b = 2.0,
	c = -12.0,
	t = 1.0;
		   
	return (1.0/(a*4.0+b*4.0-c))*(
			  RandS(uv+float2(-1.0,-1.0)*t,n)*a+
			  RandS(uv+float2( 0.0,-1.0)*t,n)*b+
			  RandS(uv+float2( 1.0,-1.0)*t,n)*a+
			  RandS(uv+float2(-1.0, 0.0)*t,n)*b+
			  RandS(uv+float2( 0.0, 0.0)*t,n)*c+
			  RandS(uv+float2( 1.0, 0.0)*t,n)*b+
			  RandS(uv+float2(-1.0, 1.0)*t,n)*a+
			  RandS(uv+float2( 0.0, 1.0)*t,n)*b+
			  RandS(uv+float2( 1.0, 1.0)*t,n)*a+
			 0.0);
}

float Step2(float2 uv,float n)
{
	float a=1.0,b=2.0,c=-2.0,t=1.0;   
	return (4.0/(a*4.0+b*4.0-c))*(
			  Step1(uv+float2(-1.0,-1.0)*t,n)*a+
			  Step1(uv+float2( 0.0,-1.0)*t,n)*b+
			  Step1(uv+float2( 1.0,-1.0)*t,n)*a+
			  Step1(uv+float2(-1.0, 0.0)*t,n)*b+
			  Step1(uv+float2( 0.0, 0.0)*t,n)*c+
			  Step1(uv+float2( 1.0, 0.0)*t,n)*b+
			  Step1(uv+float2(-1.0, 1.0)*t,n)*a+
			  Step1(uv+float2( 0.0, 1.0)*t,n)*b+
			  Step1(uv+float2( 1.0, 1.0)*t,n)*a+
			 0.0);
}

float3 Step3T(float2 uv, float time)
{
	float a=Step2(uv, 0.07*(frac(time)+1.0));    
	float b=Step2(uv, 0.11*(frac(time)+1.0));    
	float c=Step2(uv, 0.13*(frac(time)+1.0));
	return float3(a,b,c);
}


//--圆形噪声
#define NUM_SAMPLES 20
#define NUM_RINGS 20

float2 poissonDisk[NUM_SAMPLES];
float rand_1to1(float x ) { 
  // -1 -1
  
  return frac(sin(x)*10000.0);
}

float rand_2to1(float2 uv ) { 
  // 0 - 1
	float a = 12.9898, b = 78.233, c = 43758.5453;
	float dt = dot( uv, float2( a,b ) );
    float sn = dt % UNITY_PI;
    //float sn = modf( dt, PI );
	return frac(sin(sn) * c);
}
void poissonDiskSamples(in float2 randomSeed ) {

  float ANGLE_STEP = UNITY_TWO_PI * float( NUM_RINGS ) / float( NUM_SAMPLES );
  float INV_NUM_SAMPLES = 1.0 / float( NUM_SAMPLES );

  float angle = rand_2to1( randomSeed ) * UNITY_TWO_PI;
  float radius = INV_NUM_SAMPLES;
  float radiusStep = radius;

  for( int i = 0; i < NUM_SAMPLES; i ++ ) {
    poissonDisk[i] = float2( cos( angle ), sin( angle ) ) * pow( radius, 0.75 );
    radius += radiusStep;
    angle += ANGLE_STEP;
  }
}

void uniformDiskSamples(in float2 randomSeed ) {

  float randNum = rand_2to1(randomSeed);
  float sampleX = rand_1to1( randNum ) ;
  float sampleY = rand_1to1( sampleX ) ;

  float angle = sampleX * UNITY_TWO_PI;
  float radius = sqrt(sampleY);

  for( int i = 0; i < NUM_SAMPLES; i ++ ) {
    poissonDisk[i] = float2( radius * cos(angle) , radius * sin(angle)  );

    sampleX = rand_1to1( sampleY ) ;
    sampleY = rand_1to1( sampleX ) ;

    angle = sampleX * UNITY_TWO_PI;
    radius = sqrt(sampleY);
  }
}

#endif
