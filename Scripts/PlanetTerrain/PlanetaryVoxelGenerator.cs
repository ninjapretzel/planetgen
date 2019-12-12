using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Ex.Data;
using LevelUpper.Extensions;
using static Util.Res;
using System.Runtime.InteropServices;
using Ex;

/// <summary> Generator that samples densities and builds meshes from them </summary>
public class PlanetaryVoxelGenerator : MonoBehaviour {

	public bool regen = false;
	public bool autoRegen = false;
	public bool next = false;
	public bool prev = false;
	public bool dumpJson = false;
	public float autoRegenTime = 1f;
	float autoRegenTimeout = 0f;

	/// <summary> Center of planet </summary>
	public Vector3 center { get { return transform.position; } }

	[Tooltip("Min radius of sphere's surface.")]
	public float minRadius = .7f;
	[Tooltip("Max radius of sphere's surface.")]
	public float maxRadius = .95f;
	
	[Range(.001f, .999f)]
	public float surface = .5f;

	[Tooltip("Maximum distance away from tracked point to use higher LOD than 4")]
	public int lodDist = 6;
	[Tooltip("Max distance to render terrain")]
	public int renderDist = 8;

	[Tooltip("Size of a single cubical cell")]
	public float cubeSize = 10.0f;

	[Tooltip("Maximum number of cells on highest LOD cube")]
	public int maxLOD = 64;

	/// <summary> Minimum LOD </summary>
	public const int MIN_LOD = 4;

	public long seed = 15L;
	public SimplexNoise[] noises;
	public UberData[] ubers;
	public ComputeShader shader;
	public ComputeShader marchingCubes;
	public string chunkName = "Chunk3d";
	public string densityKernelName = "Density";

	[Tooltip("Fractional power falloff per cell away from tracked. ")]
	public float lodFalloff = .5f;

	public CraterData craterData = new CraterData() {
		noise = new SimplexNoise() {
			octaves = 3,
			octaveScale = 1.7f,
			persistence = .98f,
			scale = 0.015f,
			noiseOffset = new Vector3(155, 137, 149),
		},
		comp = new Vector4(1, 0, 0, 1),
		craterMin = .141f,
		craterMax = .411f,
		craterLip = .559f,
	};
	public SimplexNoise densityNoise = new SimplexNoise() {
		scale = .015f,
		octaves = 7,
		octaveScale = 1.3f,
		persistence = .85f,
		noiseOffset = new Vector3(155, 137, 149)
	};

	public UberData densityUberNoise = UberData.Defaults;

	private SimplexGenerator generator;
	public Transform[] objects;
	public Transform tracked;
	private Dictionary<string, ComputeBuffer> buffers = new Dictionary<string, ComputeBuffer>();
	bool settingsUpdated = false;


	void Start() {
		objects = new Transform[1];

		var chunkPrefab = SafeLoad<Transform>(chunkName, "Chunk3D");
		objects[0] = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
		objects[0].gameObject.SetActive(false);
		
		var mc3d = objects[0].GetComponent<MeshChunk3D>();
		if (mc3d != null) {
			mc3d.generator = this;
		}

		generator = GetComponent<SimplexGenerator>();
		if (generator == null) { generator = gameObject.AddComponent<SimplexGenerator>(); }		

		UpdateGenerator();

	}
	void OnValidate() {
		settingsUpdated = true;
	}


	void Update() {
		if (tracked == null) { 
			if (Camera.main != null) {
				tracked = Camera.main.transform;
			}
		}

		if (autoRegen) {
			autoRegenTimeout += Time.deltaTime;
			if (autoRegenTimeout >= autoRegenTime) {
				regen = true;
				autoRegenTimeout = 0f;
			}
			if (next) { seed++; prev = false; }
			if (prev) { seed--; next = false; }
		} else {
			if (next) {
				regen = true;
				seed++;
				prev = next = false;
			}
			if (prev) {
				regen = true;
				seed--;
				prev = next = false;
			}
		}
		if (regen) {
			regen = false;
			UpdateGenerator();
			generator.Clear();
		}

		if (dumpJson) {
			dumpJson = false;
			// Debug.Log(DumpJson());
		}

	}
	
	void UpdateGenerator(){

		generator.noise = densityNoise;
		generator.repeating = Vector3.one * renderDist;
		generator.objects = objects;
		generator.offset = cubeSize*2;
		generator.lockY = false;
		
		UnityEngine.Random.InitState(unchecked((int)seed));
		Buffer("Perms", SimplexNoise.stdPerm);

		noises = new SimplexNoise[] { densityNoise, };
		Buffer("Noises", noises);

		ubers = new UberData[] { densityUberNoise, };
		Buffer("Ubers", ubers);
	}

	void OnDestroy() {
		// Release buffers
		foreach (var pair in buffers) {
			pair.Value.Release();
		}
		buffers.Clear();
	}

	public void ProcessParticles(Vector3 center, Vector3 extents) {
		uint w, h, d;

		Vector3 start = center - extents;
		Vector3 end = center + extents;

		int densityKernel = shader.FindKernel(densityKernelName);
		shader.GetKernelThreadGroupSizes(densityKernel, out w, out h, out d);

		PipeData(shader, craterData);
		PipeData(shader, densityKernel, "Perms");
		PipeData(shader, densityKernel, "Noises");
		PipeData(shader, densityKernel, "Ubers");

		int size = maxLOD;
		int n = size * size * size;
		ComputeBuffer densities = Buffer("Densities", n, sizeof(float) * 4);
		ComputeBuffer triangles = AppendBuffer<Triangle>("Triangles", n * 5);
		ComputeBuffer readCount = Buffer("ReadCount", 1, sizeof(float), ComputeBufferType.Raw);
		triangles.SetCounterValue(0);


		float sz = (float)size;
		Vector3 dist = (end - start) / sz;
		shader.SetInt("size", size);
		shader.SetFloat("sz", sz);
		shader.SetVector("start", start);
		shader.SetVector("end", end);
		shader.SetVector("dist", dist);
		shader.SetFloat("minRadius", minRadius);
		shader.SetFloat("maxRadius", maxRadius);
		shader.SetBuffer(densityKernel, "Densities", densities);

		shader.Dispatch(densityKernel, size / (int)w, size / (int)h, size / (int)d);

		ParticleSystem sys = transform.Require<ParticleSystem>();
		Vector4[] densityArr = new Vector4[densities.count];
		densities.GetData(densityArr, 0, 0, densityArr.Length);

		ParticleSystem.Particle[] particles = new ParticleSystem.Particle[densityArr.Length];
		for (int i = 0; i < particles.Length; i++) {
			Vector4 p = densityArr[i];
			particles[i].remainingLifetime = 99999f;
			particles[i].startLifetime = 99999f;
			particles[i].position = new Vector3(p.x, p.y, p.z);
			particles[i].startColor = Color.Lerp(new Color(0,0,0,.1f), Color.white, p.w);
			particles[i].startSize = dist.magnitude * 2.0f;
			particles[i].velocity = Vector3.zero;
			/*Debug.DrawLine(Vector3.zero, p);
			if (i % 1000 == 0) {
				Debug.Log($"{i}: {p}");
			}//*/
		}
		sys.Stop();
		sys.time = 0;

		sys.SetParticles(particles);

	}

	public List<Mesh> ProcessMesh(Vector3 center, Vector3 extents, int lod) {
		uint w, h, d;

		Vector3 start = center - extents;
		Vector3 end = center + extents;

		int densityKernel = shader.FindKernel(densityKernelName);
		shader.GetKernelThreadGroupSizes(densityKernel, out w, out h, out d);

		PipeData(shader, craterData);
		PipeData(shader, densityKernel, "Perms");
		PipeData(shader, densityKernel, "Noises");
		PipeData(shader, densityKernel, "Ubers");
		
		int size = lod;
		int n = size * size * size;
		ComputeBuffer densities = Buffer("Densities", n, sizeof(float) * 4);
		ComputeBuffer triangles = AppendBuffer<Triangle>("Triangles", n * 5);
		ComputeBuffer readCount = Buffer("ReadCount", 1, sizeof(float), ComputeBufferType.Raw);
		triangles.SetCounterValue(0);


		float sz = (float) size;
		Vector3 dist = (end - start) / sz;
		shader.SetInt("size", size);
		shader.SetFloat("sz", sz);
		shader.SetVector("start", start);
		shader.SetVector("end", end);
		shader.SetVector("dist", dist);
		shader.SetFloat("minRadius", minRadius);
		shader.SetFloat("maxRadius", maxRadius);
		shader.SetBuffer(densityKernel, "Densities", densities);

		shader.Dispatch(densityKernel, size/(int)w, size/(int)h, size/(int)d);
		//shader.Dispatch(densityKernel, size, size, size);
		// Densities is now populated with densities from shader
		
		// Pass density samples on to MarchingCubes shader
		int cubeKernel = marchingCubes.FindKernel("MarchingCubes");
		marchingCubes.GetKernelThreadGroupSizes(cubeKernel, out w, out h, out d);

		marchingCubes.SetInt("size", size);
		marchingCubes.SetFloat("surface", surface);
		marchingCubes.SetBuffer(cubeKernel, "Densities", densities);
		marchingCubes.SetBuffer(cubeKernel, "Triangles", triangles);

		marchingCubes.Dispatch(cubeKernel, size / (int)w, size / (int)h, size / (int)d);
		//marchingCubes.Dispatch(cubeKernel, size-1, size-1, size-1);
		// Triangles should have been filled with triangle positions now.

		ComputeBuffer.CopyCount(triangles, readCount, 0);
		int[] cnt = { 0 };
		readCount.GetData(cnt);
		int actualTris = cnt[0];
		
		Triangle[] tris = new Triangle[actualTris];
		triangles.GetData(tris, 0, 0, actualTris);

		List<Mesh> meshes = new List<Mesh>();
		// int actVerts = actualTris * 3;
		int trisPerSub = (1<<16) / 3;
		int numSubs = (int) Mathf.Ceil((0.0f+actualTris) / trisPerSub);
		
		//Debug.Log($"Total Tris: {tris.Length} raw ( {actualTris} ), num subs {numSubs}");
		//Debug.Log($"Total Verts: {actualTris * 3}");
		for (int k = 0; k < numSubs; k++) {
			Mesh m = new Mesh();
			int vertsInSub = k == numSubs-1 ? (actualTris % trisPerSub) * 3 : (trisPerSub * 3);

			var verts = new Vector3[vertsInSub];
			var mtris = new int[vertsInSub];
			int startTri = k * trisPerSub;

			// Debug.Log($"k={k}, #verts: {vertsInSub}, starting from {startTri}");
			for (int i = 0; i < vertsInSub; i++) {
				int pos = startTri + i/3;
				mtris[i] = i;
				
				verts[i] = i % 3 == 0 ? tris[pos].a : (i % 3 == 1 ? tris[pos].b : tris[pos].c);
			}
			
			m.vertices = verts;
			m.triangles = mtris;
			m.RecalculateNormals();
			m.RecalculateTangents();
			meshes.Add(m);
		}

		return meshes;
	}

#pragma warning disable 649
	struct Triangle { public Vector3 a,b,c; }
#pragma warning restore 649


	public ComputeBuffer Buffer<T>(string name, T[] data) where T : struct {
		int count = data.Length;
		int stride = StructInfo<T>.size;

		ComputeBuffer buffer = Buffer(name, count, stride);
		buffer.SetData(data);

		return buffer;
	}

	public ComputeBuffer AppendBuffer<T>(string name, int count) where T: struct {
		int stride = StructInfo<T>.size;
		return Buffer(name, count, stride, ComputeBufferType.Append);
	}

	public ComputeBuffer Buffer(string name, int count, int stride, ComputeBufferType kind = ComputeBufferType.Default) {
		ComputeBuffer buffer = buffers.ContainsKey(name)
			? buffers[name]
			: (buffers[name] = new ComputeBuffer(count, stride, kind));

		// Replace buffer existing named buffer
		if (buffer.count != count || buffer.stride != stride) {
			buffer.Release();
			buffer = new ComputeBuffer(count, stride, kind);
			buffers[name] = buffer;
			return buffer;
		}
		return buffer;
	}

	public ComputeBuffer Buffer(string name) {
		if (buffers.ContainsKey(name)) {
			return buffers[name];
		}
		return null;
	}

	public void PipeData(ComputeShader compute, SimplexNoise noise) {
		compute.SetInt("octaves", (int)noise.octaves);
		compute.SetFloat("persistence", noise.persistence);
		compute.SetFloat("scale", noise.scale);
		compute.SetFloat("octaveScale", noise.octaveScale);
		compute.SetVector("noiseOffset", noise.noiseOffset);
	}
	public void PipeData(ComputeShader compute, CraterData craters) {
		compute.SetFloat("craterEffect", craters.craterEffect);
		compute.SetFloat("craterPoint", craters.craterPoint);
		compute.SetFloat("warpAmt", craters.warpAmount);

		compute.SetVector("craterComp", craters.comp);
		compute.SetFloat("craterMin", craters.craterMin);
		compute.SetFloat("craterMax", craters.craterMax);
		compute.SetFloat("craterLip", craters.craterLip);
		PipeData(compute, craters.noise);
	}

	public void PipeData(ComputeShader shader, int kernel, string bufferName) {
		var buffer = Buffer(bufferName);
		shader.SetBuffer(kernel, bufferName, buffer);
		shader.SetInt(bufferName + "Length", buffer.count);
	}
}
