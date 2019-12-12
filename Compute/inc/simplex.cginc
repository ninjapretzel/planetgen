// LEL include guards
#ifndef SIMPLEX_CGINC
#define SIMPLEX_CGINC

/// 1d Gradient
inline float grad(int hh, float x) {
	uint h = hh & 0x0f; // 4 bits -> (-8...8) (16 values)
	float grad = 1.0 + (h & 0x07);
	if ((h & 0x08) != 0) { grad = -grad; }
	return grad * x;
}
/// 2d Gradient
inline float grad(int hh, float x, float y) {
	uint h = hh & 0x07; // 3 bits -> 8 directions
	float u = (h < 4) ? x : y;
	float v = (h < 4) ? y : x;
	return (((h & 0x01) != 0) ? -u : u) // dot with (x,y)
		+ (((h & 0x02) != 0) ? -2.0 : 2.0) * v;
	
}
/// 3d Gradient
inline float grad(int hh, float x, float y, float z) {
	uint h = hh & 0x0F; // 4 bits -> 12 directions, repeat thru (12-15)
	float u = h < 8 ? x : y;
	float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
	return (((h & 0x01) != 0) ? -u : u) // )
		+ (((h & 0x02) != 0) ? -v : v);
}	

float RawNoise1D(float x) {
	// 'corner' positions
	int i0 = floor(x);
	int i1 = i0 + 1;
	// distances to 'corners'
	float x0 = x - i0;
	float x1 = x0 - 1.0f;

	// Contribution of corners 
	float t0 = 1.0f - x0 * x0;
	t0 *= t0;
	float n0 = t0 * t0 * grad(hash(i0), x0);

	float t1 = 1.0f - x1 * x1;
	t1 *= t1;
	float n1 = t1 * t1 * grad(hash(i1), x1);

	// Scale result to fit into [-1,1]
	return .0395f * (n0 + n1);
}

#define SQRT3 1.73205080757
#define F2 (.5 * (SQRT3 - 1.0))
#define G2 ((3.0 - SQRT3) / 6.0)
float RawNoise2D(float x, float y) {
	//Noise contributions from the corners
	float n0, n1, n2;

	// skew input space to make math easier.
	float s = (x + y) * F2;
	int i = floor(x + s);
	int j = floor(y + s);

	float t = (i + j) * G2;
	//Unskew back into normal space
	float X0 = i - t;
	float Y0 = j - t;
	//The x,y distance from the cell's origin
	float x0 = x - X0;
	float y0 = y - Y0;

	// For the 2D case, the simplex shape is an equilateral triangle.
	// Determine which simplex we are in.
	int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
	if (x0 > y0) { i1 = 1; j1 = 0; } // lower triangle, XY order: (0,0)->(1,0)->(1,1)
	else { i1 = 0; j1 = 1; } // upper triangle, YX order: (0,0)->(0,1)->(1,1)

	// A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
	// a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
	// c = (3-sqrt(3))/6
	float x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
	float y1 = y0 - j1 + G2;
	float x2 = x0 - 1.0 + 2.0 * G2; // Offsets for last corner in (x,y) unskewed coords
	float y2 = y0 - 1.0 + 2.0 * G2;

	// Work out the hashed gradient indices of the three simplex corners
	int gi0 = hash(i + hash(j));
	int gi1 = hash(i + i1 + hash(j + j1));
	int gi2 = hash(i + 1 + hash(j + 1));

	// Calculate the contribution from the three corners
	float t0 = 0.5 - x0 * x0 - y0 * y0;
	if (t0 < 0) { n0 = 0.0; } else {
		t0 *= t0;
		n0 = t0 * t0 * grad(gi0, x0, y0);
	}

	float t1 = 0.5 - x1 * x1 - y1 * y1;
	if (t1 < 0) { n1 = 0.0; } else {
		t1 *= t1;
		n1 = t1 * t1 * grad(gi1, x1, y1);
	}

	float t2 = 0.5 - x2 * x2 - y2 * y2;
	if (t2 < 0) { n2 = 0.0; } else {
		t2 *= t2;
		n2 = t2 * t2 * grad(gi2, x2, y2);
	}

	//Add contributions from each corner to get the final noise value.
	//The result is scaled to return values in the interval [-1,1].
	/// formerly 70.0f
	return 45.23065 * (n0 + n1 + n2);
}
inline float RawNoise2D(float2 pos) { return RawNoise2D(pos.x, pos.y); }

#define F3 (1.0 / 3.0)
#define G3 (1.0 / 6.0)
float RawNoise3D(float x, float y, float z) {
	float n0, n1, n2, n3;

	// Skew/Unskew
	float s = (x + y + z) * F3;
	int i = floor(x + s);
	int j = floor(y + s);
	int k = floor(z + s);
	float t = (i + j + k) * G3;

	float X0 = i - t;
	float Y0 = j - t;
	float Z0 = k - t;
	float x0 = x - X0;
	float y0 = y - Y0;
	float z0 = z - Z0;

	int i1, j1, k1, i2, j2, k2;
	if (x0 > y0) {
		if (y0 >= z0) {
			i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; // X Y Z
		} else if (x0 >= z0) {
			i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; // X Z Y
		} else {
			i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; // Z X Y
		}
	} else { // x0 < y0
		if (y0 < z0) {
			i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; // Z Y X
		} else if (x0 < z0) {
			i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; // Y Z X
		} else {
			i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; // Y X Z
		}
	}

	// Steps reuse G3 offset to remain at 'corners'
	float x1 = x0 - i1 + G3;
	float y1 = y0 - j1 + G3;
	float z1 = z0 - k1 + G3;
	float x2 = x0 - i2 + 2.0f * G3;
	float y2 = y0 - j2 + 2.0f * G3;
	float z2 = z0 - k2 + 2.0f * G3;
	float x3 = x0 - 1.0f + 3.0f * G3;
	float y3 = y0 - 1.0f + 3.0f * G3;
	float z3 = z0 - 1.0f + 3.0f * G3;

	// Get corner values
	int gi0 = hash(i + hash(j + hash(k)));
	int gi1 = hash(i + i1 + hash(j + j1 + hash(k + k1)));
	int gi2 = hash(i + i2 + hash(j + j2 + hash(k + k2)));
	int gi3 = hash(i + 1 + hash(j + 1 + hash(k + 1)));

	float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
	if (t0 < 0) { n0 = 0.0f; } else {
		t0 *= t0;
		n0 = t0 * t0 * grad(gi0, x0, y0, z0);
	}

	float t1 = .6f - x1 * x1 - y1 * y1 - z1 * z1;
	if (t1 < 0) { n1 = 0.0f; } else {
		t1 *= t1;
		n1 = t1 * t1 * grad(gi1, x1, y1, z1);
	}

	float t2 = .6f - x2 * x2 - y2 * y2 - z2 * z2;
	if (t2 < 0) { n2 = 0.0f; } else {
		t2 *= t2;
		n2 = t2 * t2 * grad(gi2, x2, y2, z2);
	}

	float t3 = .6f - x3 * x3 - y3 * y3 - z3 * z3;
	if (t3 < 0) { n3 = 0.0f; } else {
		t3 *= t3;
		n3 = t3 * t3 * grad(gi3, x3, y3, z3);
	}

	// Scale to stay in [-1,1]
	return 32.0f * (n0 + n1 + n2 + n3);
}
inline float RawNoise3D(float3 pos) { return RawNoise3D(pos.x, pos.y, pos.z); }

// Derivitave noise 3d using simplex
float4 DNoise3D(float3 pos) {
	const float3 p = floor(pos);
	const float3 w = frac(pos);

	const float3 u = w*w*w*(w*(w*6.0-15.0)+10.0);
	const float3 du = 30.0*w*w*(w*(w-2.0)+1.0);

	const float a = .5 + .5 * RawNoise3D( p+float3(0,0,0) );
	const float b = .5 + .5 * RawNoise3D( p+float3(1,0,0) );
	const float c = .5 + .5 * RawNoise3D( p+float3(0,1,0) );
	const float d = .5 + .5 * RawNoise3D( p+float3(1,1,0) );
	const float e = .5 + .5 * RawNoise3D( p+float3(0,0,1) );
	const float f = .5 + .5 * RawNoise3D( p+float3(1,0,1) );
	const float g = .5 + .5 * RawNoise3D( p+float3(0,1,1) );
	const float h = .5 + .5 * RawNoise3D( p+float3(1,1,1) );

	const float k0 =   a;
	const float k1 =   b - a;
	const float k2 =   c - a;
	const float k3 =   e - a;
	const float k4 =   a - b - c + d;
	const float k5 =   a - c - e + g;
	const float k6 =   a - b - e + f;
	const float k7 = - a + b + c - d + e - f - g + h;
	
	const float3 rest = 2.0 * du * float3( k1 + k4*u.y + k6*u.z + k7*u.y*u.z,
                                      k2 + k5*u.z + k4*u.x + k7*u.z*u.x,
                                      k3 + k6*u.x + k5*u.y + k7*u.x*u.y );
	const float sample = -1.0+2.0*(k0 + k1*u.x + k2*u.y + k3*u.z + k4*u.x*u.y + k5*u.y*u.z + k6*u.z*u.x + k7*u.x*u.y*u.z);
	
    return float4(sample, rest.x, rest.y, rest.z);
}

float4 DFractal(NoiseData DATA, float3 pos)  {
	float frequency = DATA.scale;
	float sum = 0.0;
	float amplitude = 0.5;
	float3 dSum = float3(0.0, 0.0, 0.0);
	// @??? - why.
	//float3x3 m = float3x3(1.0,0.0,0.0,
	//				0.0,1.0,0.0,
	//				0.0,0.0,1.0);
	for( int i=0; i < DATA.octaves; i++ ) {
		float4 sample = DNoise3D(pos * frequency);
		// accumulate derivatives, amplitude optional.
		// dSum += amplitude * sample.yzw;	
		dSum += sample.yzw;	
		// accumulate values
		sum += amplitude * sample.x / (1.0 + dot(dSum, dSum));	
		
		frequency *= DATA.octaveScale;
		amplitude *= DATA.persistence;
		
		// @??? - why manipulate position ?
		// pos = frequency*m3*pos;
		// m = frequency*m3i*m;
	}
	return float4( sum, dSum );
}

float Fractal(NoiseData DATA, float x) {
	float total = 0;
	float sum = 0;
	float frequency = DATA.scale;
	float amplitude = 1;
	
	for (int i = 0; i < DATA.octaves; i++) {
		total += amplitude * RawNoise1D(DATA.noiseOffset.x + x * frequency);
		sum += amplitude;
		frequency *= DATA.octaveScale;
		amplitude *= DATA.persistence;
	}
	return total / sum;
}
float Fractal(NoiseData DATA, float x, float y) {
	float total = 0;
	float sum = 0;
	float frequency = DATA.scale;
	float amplitude = 1;

	for (int i = 0; i < DATA.octaves; i++) {
		total += amplitude * RawNoise2D(DATA.noiseOffset.x + x * frequency, 
										DATA.noiseOffset.y + y * frequency);
		sum += amplitude;

		frequency *= DATA.octaveScale;
		amplitude *= DATA.persistence;
	}

	return total / sum;
}
inline float Fractal(NoiseData DATA, float2 pos) { return Fractal(DATA, pos.x, pos.y); }
float Fractal(NoiseData DATA, float x, float y, float z) {
	float total = 0;
	float sum = 0;
	float frequency = DATA.scale;
	float amplitude = 1;

	for (int i = 0; i < DATA.octaves; i++) {
		total += amplitude * RawNoise3D(DATA.noiseOffset.x + x * frequency, 
										DATA.noiseOffset.y + y * frequency, 
										DATA.noiseOffset.z + z * frequency);
		sum += amplitude;

		frequency *= DATA.octaveScale;
		amplitude *= DATA.persistence;
	}

	return total / sum;
}
inline float Fractal(NoiseData DATA, float3 pos) { return Fractal(DATA, pos.x, pos.y, pos.z); }

inline float FBM1(NoiseData DATA, float x, float lo, float hi) {
	float val = Fractal(DATA, x);
	return lo + ((val + 1) / 2) * (hi - lo);
}
inline float FBM1(NoiseData DATA, float x) { return FBM1(DATA, x, 0, 1); }

inline float FBM2(NoiseData DATA, float x, float y, float lo, float hi) {
	float val = Fractal(DATA, x, y);
	return lo + ((val + 1) / 2) * (hi - lo);
}
inline float FBM2(NoiseData DATA, float x, float y) { return FBM2(DATA, x, y, 0, 1); }
inline float FBM2(NoiseData DATA, float2 pos) { return FBM2(DATA, pos.x, pos.y, 0, 1); }
inline float FBM2(NoiseData DATA, float2 pos, float lo, float hi) { return FBM2(DATA, pos.x, pos.y, lo, hi); }

inline float FBM3(NoiseData DATA, float x, float y, float z, float lo, float hi) {
	float val = Fractal(DATA, x, y, z);
	return lo + ((val + 1) / 2) * (hi - lo);
}
inline float FBM3(NoiseData DATA, float x, float y, float z) { return FBM3(DATA, x, y, z, 0, 1); }
inline float FBM3(NoiseData DATA, float3 pos) { return FBM3(DATA, pos.x, pos.y, pos.z, 0, 1); }
inline float FBM3(NoiseData DATA, float3 pos, float lo, float hi) { return FBM3(DATA, pos.x, pos.y, pos.z, 0, 1); }

inline float3 Warp(NoiseData DATA, float3 pos, float rate, float3 phaseX, float3 phaseY, float3 phaseZ) {
	float3 q = float3(FBM3(DATA, pos + phaseX), FBM3(DATA, pos + phaseY), FBM3(DATA, pos + phaseZ));
	return pos + rate * q;
}
inline float3 Warp(NoiseData DATA, float3 pos, float rate) {
	return Warp(DATA, pos, rate, float3(5.1, 1.3, 2.4), float3(1.7, 9.2, 3.5), float3(2.3, 1.2, 1.9));
}
inline float3 Warp(NoiseData DATA, float3 pos) {
	return Warp(DATA, pos, 4, float3(5.1, 1.3, 2.4), float3(1.7, 9.2, 3.5), float3(2.3, 1.2, 1.9));
}

#endif