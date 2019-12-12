
#ifndef VORONI_CGINC
#define VORONI_CGINC

#define VORONI_FROM (-1)
#define VORONI_TO (2)
#define DEFAULT_COMP (float4(-1, 1, 0, 1))

float Voroni3D(float3 pos, float4 comp) {
	float3 apos = pos + noiseOffset.xyz;
	float3 p = floor(apos);
	float3 f = abs(apos - p);
	float3 closest = float3(1,1,1);
	
	for (int k = VORONI_FROM; k <= VORONI_TO; k++) {
		for (int j = VORONI_FROM; j <= VORONI_TO; j++) {
			for (int i = VORONI_FROM; i <= VORONI_TO; i++) {
				float3 sampleOffset = float3(i, j, k);
				float3 feature = p + sampleOffset;
				// Offset of feature point
				// Raw noise is [-1,1] so scale it [0, .75]
				// this way features can't be moved into problematic locations.
				// Where we would have to loop extra in each direction
				float offsetValue = (.5 + .5 * RawNoise3D(feature)) * .75;
				
				// Direction is always (x+ y+ z+)
				// to help reduce problematic feature placements.
				float sx = hashF(feature.x);
				float sy = hashF(feature.y);
				float sz = hashF(feature.z);
				float3 shift = float3(sx, sy, sz) * offsetValue;
				
				float3 pointToFeature = sampleOffset - f + shift;
				float dist = 0;
				
				// DIFFMODE: MANHATTAN
				// featurePoint = new Vector3(Abs(featurePoint.x), Abs(featurePoint.y), Abs(featurePoint.z));
				// dist = Max(Max(featurePoint.x, featurePoint.y), featurePoint.z);
				
				// DIFFMODE: EUCLID
				dist = length(pointToFeature);
				if (dist < closest[0]) { closest[2] = closest[1]; closest[1] = closest[0]; closest[0] = dist; } 
				else if (dist < closest[1]) { closest[2] = closest[1]; closest[1] = dist; } 
				else if (dist < closest[2]) { closest[2] = dist; }
				
			}
		}
	}
	return comp.w * abs(comp.x * closest.x + comp.y * closest.y + comp.z * closest.z);
}
#endif