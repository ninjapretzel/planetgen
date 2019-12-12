#ifndef HASH_PRIMITIVES
#define HASH_PRIMITIVES

///Integer hash
inline uint hash(uint i) { return Perms[i & 0xFF]; }
#define DEFAULT_SEED 31337.1337
#define Y_STEP 157.0
#define Z_STEP 113.0
///Simple float hash primitive, range [0, 1)
inline float hashF(float n, float seed) { return frac(abs(sin(n) * seed)); }
inline float hashF(float n) { return hashF(n, DEFAULT_SEED); }
inline float hashF2(float2 v) { return hashF(v.x + v.y * Y_STEP); }
inline float hashF3(float3 v) { return hashF(v.x + v.y * Y_STEP + Z_STEP * v.z); }
///Fast Smooth
inline float2 smooth(float2 uv) { return uv*uv*(3.-2.*uv); }

///Quick, linear interpolated noise.
inline float qnoise1(float x) {
	const float p = floor(x);
	const float f = frac(x);
	return lerp(hashF(p+0.0), hashF(p+1.0), f);
}
///Smooth 1d noise
inline float noise1(float x) {
	const float p = floor(x);
	const float f = frac(x);
	const float f2= f*f*(3.0-2.0*f);
	return lerp(hashF(p+0.0), hashF(p+1.0), f2);
}

///Smooth, standard 3d FBM like noise.
inline float noise(float3 x) {
	const float3 p = floor(x);
	const float3 f = frac(x);
	const float3 f2= f*f*(3.0-2.0*f);
	const float n = p.x + p.y*157.0 + 113.0*p.z;

	return lerp(lerp(	lerp( hashF(n+0.0), hashF(n+1.0),f2.x),
						lerp( hashF(n+157.0), hashF(n+158.0),f2.x),f2.y),
				lerp(	lerp( hashF(n+113.0), hashF(n+114.0),f2.x),
						lerp( hashF(n+270.0), hashF(n+271.0),f2.x),f2.y),f2.z);
}
#endif