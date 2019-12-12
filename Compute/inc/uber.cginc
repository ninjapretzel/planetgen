// LEL include guards
#ifndef UBER_CGINC
#define UBER_CGINC

// Suggerted values taken from `COMPUTENOISEUBERSINGLEPASS.SHADER.H.BIN`
struct UberData {
	float octaves; 			// ~( 1.0, inf) (cap ~10 for reasonableness)
	
	float perturb; 			// ~(-0.4, 0.4)
	float sharpness;		// ~(-1.0, 1.0)
	float amplify;			// ~( 0.0, 0.5)
	
	float altitudeErosion; 	// ~( 0.0, 0.25)
	float ridgeErosion;		// ~( 0.0, 1.0)
	float slopeErosion;		// ~( 0.0, 1.0)
	
	float lacunarity;		// = 2.0f
	float gain;				// = 0.5f
	float startAmplitude;	// = 0.9f
	float scale;			// = 1.0f
};
RWStructuredBuffer<UberData> Ubers;
int UbersLength;

float uberNoise(UberData data, float3 position) {
	
	float sum = 0.0;
	float total = 0.0;
	float frequency = 1.0 * data.scale;
	float amplitude = data.startAmplitude;
	
	/// @??? - Why round this?
	// float sharpness = (float)round(data.sharpness);
	// float sharpness = data.sharpness;
	
	float3 slopeErosionDerivativeSum = float3(0, 0, 0);
	float3 perturbDerivativeSum = float3(0, 0, 0);
	float3 ridgeErosionDerivativeSum = float3(0, 0, 0);
	
	float dampedAmplitude = amplitude;
	float currentGain = data.gain + (data.slopeErosion * 0.75);
	
	for (int i = 0; i < data.octaves; i++) {
		float3 octavePosition = (position * frequency) + perturbDerivativeSum;
		
		float4 sample = DNoise3D(octavePosition);
		float noise = sample.x;
		float3 derivative = sample.yzw;
		
		float featureNoise = noise;
		
		{ 
			// Sub function, apply ridge/billow noise effects.
			float ridgedNoise = (1.0 - abs(featureNoise));
			float billowNoise = featureNoise * featureNoise;
			
			// Maybe parameterize these constant values?
			ridgedNoise = (ridgedNoise * 1.8f) - 1.2f;
			billowNoise = (billowNoise * 2.5f) - 0.45f;
			
			featureNoise = lerp(featureNoise, billowNoise, max(0.0, data.sharpness));
			featureNoise = lerp(featureNoise, ridgedNoise, abs(min(0.0, data.sharpness)));
		}
		
		// derivative *= noise * noise;
		derivative *= featureNoise;
		
		slopeErosionDerivativeSum += derivative * data.slopeErosion;
		ridgeErosionDerivativeSum += derivative;
		perturbDerivativeSum += derivative * data.perturb;
		
		const float maxAdd = dampedAmplitude * (1.0 / ( 1.0 + dot(slopeErosionDerivativeSum, slopeErosionDerivativeSum)));
		sum += maxAdd;
		total += featureNoise * maxAdd;
		
		
		frequency *= data.lacunarity;
		amplitude *= lerp(currentGain, currentGain * smoothstep(0.0, 1.0, sum), data.altitudeErosion);
		
		currentGain = currentGain + data.amplify;
		dampedAmplitude = amplitude * (1.0 - (data.ridgeErosion / (1.0 + dot(ridgeErosionDerivativeSum, ridgeErosionDerivativeSum))));
		
	}
	
	
	return total / sum;
	
}

float recursiveUber(int octaves, int indexMin, int indexMax, float3 position) {
	UberData gen;
	gen.octaves = octaves;
	UberData umin = Ubers[indexMin % UbersLength];
	UberData umax = Ubers[indexMax % UbersLength];
	
	gen.perturb = uberNoise(Ubers[(indexMin) % UbersLength], position);
	gen.perturb = lerp(umin.perturb, umax.perturb, saturate(gen.perturb));
	gen.sharpness = uberNoise(Ubers[(indexMin+1) % UbersLength], position);
	gen.sharpness = lerp(umin.sharpness, umax.sharpness, saturate(gen.sharpness));
	gen.amplify = uberNoise(Ubers[(indexMin+2) % UbersLength], position);
	gen.amplify = lerp(umin.amplify, umax.amplify, saturate(gen.amplify));
	
	gen.altitudeErosion = uberNoise(Ubers[(indexMin+3) % UbersLength], position);
	gen.altitudeErosion = lerp(umin.altitudeErosion, umax.altitudeErosion, saturate(gen.altitudeErosion));
	gen.ridgeErosion = uberNoise(Ubers[(indexMin+4) % UbersLength], position);
	gen.ridgeErosion = lerp(umin.ridgeErosion, umax.ridgeErosion, saturate(gen.ridgeErosion));
	gen.slopeErosion = uberNoise(Ubers[(indexMin+5) % UbersLength], position);
	gen.slopeErosion = lerp(umin.slopeErosion, umax.slopeErosion, saturate(gen.slopeErosion));
	
	gen.lacunarity = uberNoise(Ubers[(indexMin+6) % UbersLength], position);
	gen.lacunarity = lerp(umin.lacunarity, umax.lacunarity, saturate(gen.lacunarity));
	gen.gain = uberNoise(Ubers[(indexMin+7) % UbersLength], position);
	gen.gain = lerp(umin.gain, umax.gain, saturate(gen.gain));
	gen.startAmplitude = uberNoise(Ubers[(indexMin+8) % UbersLength], position);
	gen.startAmplitude = lerp(umin.startAmplitude, umax.startAmplitude, saturate(gen.startAmplitude));
	gen.scale = uberNoise(Ubers[(indexMin+9) % UbersLength], position);
	gen.scale = lerp(umin.scale, umax.scale, saturate(gen.scale));
	
	return uberNoise(gen, position);
}


#endif
