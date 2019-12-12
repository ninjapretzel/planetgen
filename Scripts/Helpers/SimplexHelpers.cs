using UnityEngine;
using static UnityEngine.Mathf;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;

/// <summary> Class holding extension methods for non-basic noise functionality. </summary>
public static class SimplexNoiseExt {

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Color PreProcessSplat(Color c) {
		float r = Abs(c.r);
		float g = Abs(c.g);
		float b = Abs(c.b);
		float a = Abs(c.a);

		//Square all color amounts to make sure they are positive (also makes transitions sharper)
		r = r * r;
		g = g * g;
		b = b * b;
		a = a * a;

		// Without sharpening just: 
		// r = Abs(r); g = Abs(g); b = Abs(b); a = Abs(a);

		//Normalize all values (sum to 1)
		float t = r + g + b + a;
		r /= t;
		g /= t;
		b /= t;
		a /= t;

		return new Color(r, g, b, a);
	}
	
	public static float[,] GetHeightsZX(this ComputeShader compute, ComputeBuffer buffer, Vector3 start, Vector3 end, int size, int kernelID) {

		uint w, h, d;
		compute.GetKernelThreadGroupSizes(kernelID, out w, out h, out d);
		if (w != 1 || h != 1 || d != 1) {
			Debug.LogWarning($"Kernel dimensions ({w}, {h}, {d}) do not fit expected (1, 1, 1) form for heightmaps!");
		}

		float[,] heights = new float[size + 1, size + 1];
		if (buffer.count != ((size+1)*(size+1)) || buffer.stride != sizeof(float)) {
			Debug.LogWarning("Heights buffer not expected size");
		}

		float sz = (float)size;
		Vector3 dist = (end - start) / sz;
		compute.SetInt("size", size);
		compute.SetFloat("sz", sz);
		compute.SetVector("start", start);
		compute.SetVector("end", end);
		compute.SetVector("dist", dist);
		
		compute.SetBuffer(kernelID, "Heights", buffer);

		// Unfortunately, heightmaps are (2^n)+1, so we always need to do at least one more...
		// Expects kernel to be numthreads(1,1,1)
		compute.Dispatch(kernelID, size + 1, 1, size + 1);
		float[] rawHeights = new float[(size + 1) * (size + 1)];
		buffer.GetData(rawHeights);

		// Unpack heights. gotta be a better way to do this, maybe a raw conversion from float[] to float[,]
		for (int i = 0; i < rawHeights.Length; i++) {
			int yy = i % (size + 1);
			int xx = i / (size + 1);
			heights[yy, xx] = rawHeights[i];
		}

		return heights;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] [Pure]
	private static int Mod(this int i, int d) { int r = i%d; return r < 0 ? r+d : r; }

	public static float[,,] GetSplats3d(this ComputeShader compute, ComputeBuffer buffer, Vector3 start, Vector3 end, int size, int layers, int kernelID) {

		uint w, h, d;
		compute.GetKernelThreadGroupSizes(kernelID, out w, out h, out d);
		if (h != 1) {
			Debug.LogWarning($"Kernel dimensions ({w}, {h}, {d}) do not fit expected (2^n, 1, 2^n) form for splats");
		}
		
		float[,,] splats = new float[size, size, layers];
		if (buffer.count != size*size*layers || buffer.stride != sizeof(float)) {
			Debug.LogWarning("Splats buffer not expected size");
		}

		float sz = (float)size;
		Vector3 dist = (end - start) / sz;
		compute.SetInt("size", size);
		compute.SetFloat("sz", sz);
		compute.SetVector("start", start);
		compute.SetVector("end", end);
		compute.SetVector("dist", dist);
		compute.SetInt("splatLayers", layers);

		compute.SetBuffer(kernelID, "Splats", buffer);

		compute.Dispatch(kernelID, size/((int)w), 1, size/((int)d));
		float[] rawSplats = new float[size * size * layers];
		buffer.GetData(rawSplats);
		
		// Unpack splats. gotta be a better way to do this, maybe a raw conversion from float[] to float[,,]
		for (int i = 0; i < rawSplats.Length; i++) {
			int xx = i;
			int yy = i / size;
			int layer = i / (size * size);
			
			splats[xx.Mod(size), yy.Mod(size), layer] = rawSplats[i];
		}

		return splats;
	}
	

}

