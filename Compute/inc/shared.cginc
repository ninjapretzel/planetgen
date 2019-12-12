#ifndef SHARED_CGINC
#define SHARED_CGINC

// Group of data for use with noises, matching SimplexNoise struct from C# side
struct NoiseData {
	float octaves;
	float persistence;
	float scale;
	float octaveScale;
	float3 noiseOffset;
};
// Buffer for piping in noisedatas
RWStructuredBuffer<NoiseData> Noises;
// Expected to be the number of available NoiseData
int numNoises = 0;

// General shared noise data
int octaves = 4;
float persistence = 0.95;
float scale = 1.0;
float octaveScale = 1.5;
float3 noiseOffset = float3(0,0,0);

// Used to locate samples in region by ID.
int size;
float sz;
float3 start;
float3 end;
float3 dist;

// Output location for height samples
RWStructuredBuffer<float> Heights;

// Output location for splat samples
RWStructuredBuffer<float> Splats;
uint splatLayers;

// example kernels:
/*
[numthreads(1,1,1)]
void Heightmap(uint3 id : SV_DispatchThreadID) {
	float3 position = start + float3(id.x, id.y, id.z) * dist;
	float height = .....;
	Heights[(size+1) * id.x + id.z] = height;
}
*/


// Other helper functions.

/// Create default noise data with 5 shared params
NoiseData DefaultNoiseData() {
	NoiseData DATA;
	DATA.octaves = octaves;
	DATA.octaveScale = octaveScale;
	DATA.scale = scale;
	DATA.noiseOffset = noiseOffset;
	DATA.persistence = persistence;
	return DATA;
}
#endif