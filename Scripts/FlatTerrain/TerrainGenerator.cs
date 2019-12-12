using UnityEngine;
using System.Collections.Generic;
using LevelUpper.Extensions;
using static UnityEngine.Mathf;
using System.Runtime.InteropServices;
using Ex.Data;  // Note: in ExServer repository
using Random = UnityEngine.Random;
using static Util.Res;


/// <summary> Generator that samples heightmaps and tosses them into unity's terrain system. </summary>
public class TerrainGenerator : MonoBehaviour {

	public bool regen = false;
	public bool autoRegen = false;
	public bool next = false;
	public bool prev = false;
	public bool dumpJson = false;
	public float autoRegenTime = 1f;
	float autoRegenTimeout = 0f;

	public Vector3 tileSize = new Vector3(20, 8, 20);
	public float viewDist = 7;
	public string chunk;
	public Transform[] objects;
	public ComputeShader shader;
	public string heightmapKernelName = "Heightmap";
	public string splatmapKernelName = "Splatmap";

	public TerrainLayer[] terrainLayers;

	public int meshSamples = 64;
	public int splatSamples = 64;
	public float slopeAngle = 45.0f;

	//public float craterEffect = .5f;
	//public float craterPoint = .5f;
	//public float warpAmt = 4.0f;

	public long seed = 15L;
	public SimplexNoise[] noises;
	public UberData[] ubers;
	
	private SimplexGenerator generator;
	private Dictionary<string, ComputeBuffer> buffers = new Dictionary<string, ComputeBuffer>();

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
	
	public SimplexNoise objectNoise = new SimplexNoise() {
		scale = .07f,
		octaves = 4,
		octaveScale = 1.6f,
		persistence = .5f,
		noiseOffset = new Vector3(155, 137, 149)
	};

	public SimplexNoise heightmapNoise = new SimplexNoise() {
		scale = .015f,
		octaves = 7,
		octaveScale = 1.3f,
		persistence = .85f,
		noiseOffset = new Vector3(155, 137, 149)
	};

	public SimplexNoise splatmapNoise = new SimplexNoise() {
		scale = .001f,
		octaves = 5,
		octaveScale = 1.5f,
		persistence = .75f,
		noiseOffset = new Vector3(155, 137, 149)
	};
	
	public UberData objectUberNoise = UberData.Defaults;
	public UberData heightmapUberNoise = UberData.Defaults;
	public UberData splatmapUberNoise = UberData.Defaults;

	public UberData minUber = new UberData() {
		perturb = -.4f,
		sharpness = -1f,
		amplify = 0f,
		altitudeErosion = 0f,
		ridgeErosion = 0f,
		slopeErosion = 0f,
		lacunarity = 1.1f,
		gain = .2f,
		startAmplitude = .1f,
		scale = .0005f,
	};

	public UberData maxUber = new UberData() {
		perturb = .4f,
		sharpness = 1f,
		amplify = 0.5f,
		altitudeErosion = 0.25f,
		ridgeErosion = 1.0f,
		slopeErosion = 1.0f,
		lacunarity = 2.5f,
		gain = .8f,
		startAmplitude = 3f,
		scale = .0016f,
	};




	void Start() {
		objects = new Transform[1];

		var chunkPrefab = SafeLoad<Transform>(chunk, "TerrainChunk");
		objects[0] = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
		objects[0].gameObject.SetActive(false);

		var mchunk = objects[0].GetComponent<MeshChunk>();
		if (mchunk != null) {
			mchunk.terrainGenerator = this;
		}


		generator = GetComponent<SimplexGenerator>();
		if (generator == null) { generator = gameObject.AddComponent<SimplexGenerator>(); }

		//objectNoise.perms = SimplexNoise.NewPermutation(objectSeed);
		//heightmapNoise.perms = SimplexNoise.NewPermutation(heightmapSeed);
		//splatmapNoise.perms = SimplexNoise.NewPermutation(splatmapSeed);
		UpdateGenerator();

	}
		

	void OnDestroy() {
		foreach (var pair in buffers) {
			pair.Value.Release();
		}
		buffers.Clear();
	}
	
	void Update() {
		
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
			Debug.Log(DumpJson());
		}

	}
	
	private void UpdateGenerator() {
		generator.noise = objectNoise;
		generator.repeating = new Vector3(viewDist, 1, viewDist);
		generator.objects = objects;
		generator.offset = tileSize.x * 2;
		generator.lockY = true;
		
		// init buffer for later use
		Buffer("Perms", SimplexNoise.stdPerm);
		noises = new SimplexNoise[2] {
			heightmapNoise,
			splatmapNoise
		};
		Buffer("Noises", noises);
		List<UberData> uberData = new List<UberData>();
		uberData.Add(heightmapUberNoise);
		uberData.Add(splatmapUberNoise);
		// SRNG rng = new SRNG(seed);
		UnityEngine.Random.InitState(unchecked ((int)seed)); 

		for (int i = 0; i < 20; i++) {
			UberData data = MakeUberData(4, minUber, maxUber);
			uberData.Add(data);
		}
		ubers = uberData.ToArray();
		Buffer("Ubers", ubers);

	}
	
	private UberData MakeUberData(int octaves) {
		UberData it = new UberData();
		it.octaves = octaves;

		it.perturb = Random.Range(-.4f, .4f);
		it.sharpness = Random.Range(-1f, 1f);
		it.amplify = Random.Range(0, .5f);

		it.altitudeErosion = Random.Range(0, .25f);
		it.ridgeErosion = Random.Range(0, 1.0f);
		it.slopeErosion = Random.Range(0, 1.0f);

		it.lacunarity = Random.Range(1.1f, 2.5f);
		it.gain = Random.Range(.2f, .8f);
		it.startAmplitude = Random.Range(0.1f, 3f);
		it.scale = Random.Range(.0005f, .0016f);
		return it;
	}

	private UberData MakeUberData(int octaves, UberData min, UberData max) {
		UberData it = new UberData();
		it.octaves = octaves;

		it.perturb = Random.Range(min.perturb, max.perturb);
		it.sharpness = Random.Range(min.sharpness, max.sharpness);
		it.amplify = Random.Range(min.amplify, max.amplify);

		it.altitudeErosion = Random.Range(min.altitudeErosion, max.altitudeErosion);
		it.ridgeErosion = Random.Range(min.ridgeErosion, max.ridgeErosion);
		it.slopeErosion = Random.Range(min.slopeErosion, max.slopeErosion);

		it.lacunarity = Random.Range(min.lacunarity, max.lacunarity);
		it.gain = Random.Range(min.gain, max.gain);
		it.startAmplitude = Random.Range(min.startAmplitude, max.startAmplitude);
		it.scale = Random.Range(min.scale, max.scale);
		return it;
	}

	public ComputeBuffer Buffer<T>(string name, T[] data) where T : struct {
		int count = data.Length;
		int stride = Marshal.SizeOf<T>();
		
		ComputeBuffer buffer = Buffer(name, count, stride);
		buffer.SetData(data);

		return buffer;
	}

	public ComputeBuffer Buffer(string name, int count, int stride) {
		ComputeBuffer buffer = buffers.ContainsKey(name)
			? buffers[name]
			: (buffers[name] = new ComputeBuffer(count, stride, ComputeBufferType.Default));

		// Replace buffer existing named buffer
		if (buffer.count != count || buffer.stride != stride) {
			buffer.Release();
			buffer = new ComputeBuffer(count, stride, ComputeBufferType.Default);
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
		shader.SetInt(bufferName+"Length", buffer.count);
	}

	public void ProcessHeights(TerrainData data, Vector3 center) {
		Vector3 a = center - tileSize;
		Vector3 b = center + tileSize;
		
		data.heightmapResolution = meshSamples + 1;
		data.size = tileSize * 2;
		
		int heightmapKernel = shader.FindKernel(heightmapKernelName);
			
		PipeData(shader, craterData);

		PipeData(shader, heightmapKernel, "Perms");
		PipeData(shader, heightmapKernel, "Noises");
		PipeData(shader, heightmapKernel, "Ubers");
		int meshSz = meshSamples + 1;
		var heightsBuffer = Buffer("Heights", meshSz*meshSz, sizeof(float));

		var heights = shader.GetHeightsZX(heightsBuffer, a, b, meshSamples, heightmapKernel);
		data.SetHeights(0, 0, heights);
	}


	public void ProcessSplats(UnityEngine.Terrain t, Vector3 center) {
		TerrainData data = t.terrainData;
		var terrainLayers = this.terrainLayers;

		Vector3 a = center - tileSize;
		Vector3 b = center + tileSize;
		data.alphamapResolution = splatSamples;
		
		int splatmapKernel = shader.FindKernel(splatmapKernelName);

		PipeData(shader, splatmapKernel, "Perms");
		PipeData(shader, splatmapKernel, "Noises");
		PipeData(shader, splatmapKernel, "Ubers");

		PipeData(shader, craterData);
		int size = splatSamples;
		int layers = terrainLayers.Length;
		var splatsBuffer = Buffer("Splats", size * size * layers, sizeof(float));
		var splats = shader.GetSplats3d(splatsBuffer, a, b, splatSamples, layers, splatmapKernel);
		
		for (int xx = 0; xx < size; xx++) {
			for (int yy = 0; yy < size; yy++) {
				float sum = 0;
				for (int layer = 0; layer < layers; layer++) {
					float val = splats[xx, yy, layer];
					sum += val * val;
				}

				float nX = xx * 1.0f / (size - 1.0f);
				float nY = yy * 1.0f / (size - 1.0f);
				float angle = data.GetSteepness(nY, nX);
				float slope = Clamp01(angle / slopeAngle);
				slope = slope * slope * slope;

				splats[xx, yy, 0] = slope;
				// Apply normalization and slope
				for (int layer = 1; layer < layers; layer++) {
					float val = splats[xx,yy,layer];
					val *= val;
					
					splats[xx, yy, layer] = (val * (1-slope)) / sum;
				}

			}
		}
		

		data.terrainLayers = terrainLayers;
		data.RefreshPrototypes();
		
		data.SetAlphamaps(0, 0, splats);
		data.RefreshPrototypes();
	}

	public TerrainData CloneTerrain(TerrainData d) {
		TerrainData data = new TerrainData();
		data.size = d.size;
		data.thickness = d.thickness;
		data.wavingGrassAmount = d.wavingGrassAmount;
		data.wavingGrassSpeed = d.wavingGrassSpeed;
		data.wavingGrassStrength = d.wavingGrassStrength;
		data.wavingGrassTint = d.wavingGrassTint;
		data.alphamapResolution = d.alphamapResolution;
		data.baseMapResolution = d.baseMapResolution;
		data.heightmapResolution = d.heightmapResolution;
		data.detailPrototypes = d.detailPrototypes;
		data.treePrototypes = d.treePrototypes;

		data.terrainLayers = CloneLayers(d.terrainLayers);
		data.SetAlphamaps(0,0, d.GetAlphamaps(0, 0, d.alphamapWidth, d.alphamapHeight));
		data.SetHeights(0, 0, d.GetHeights(0, 0, d.heightmapWidth, d.heightmapHeight));
		
		data.RefreshPrototypes();

		return data;
	}

	public TerrainLayer[] CloneLayers() { return CloneLayers(terrainLayers); }
	public TerrainLayer[] CloneLayers(TerrainLayer[] layers) {
		
		TerrainLayer[] copy = new TerrainLayer[layers.Length];

		for (int i = 0; i < layers.Length; i++) {
			copy[i] = new TerrainLayer();
			copy[i].tileOffset = layers[i].tileOffset;
			copy[i].tileSize = layers[i].tileSize;
			copy[i].diffuseTexture = layers[i].diffuseTexture;
			copy[i].normalMapTexture = layers[i].normalMapTexture;
			copy[i].maskMapTexture= layers[i].maskMapTexture;
			copy[i].name = "UNFUCKED_" + layers[i].name;
			
		}

		return copy;
	}

	public SplatPrototype[] ClonePrototypes() { return ClonePrototypes(terrainLayers); }
	public SplatPrototype[] ClonePrototypes(TerrainLayer[] layers) {
		SplatPrototype[] copy = new SplatPrototype[layers.Length];
		for (int i = 0; i < copy.Length; i++) {
			copy[i] = new SplatPrototype();
			copy[i].tileOffset = layers[i].tileOffset;
			copy[i].tileSize = layers[i].tileSize;
			copy[i].texture = layers[i].diffuseTexture;
			copy[i].normalMap = layers[i].normalMapTexture;
		}


		return copy;

	}

	public string DumpJson() {
		JsonObject obj = new JsonObject();
		obj["type"] = gameObject.name;
		obj["global"] = true;
		JsonArray components = new JsonArray();
		obj["components"] = components;

		JsonObject terrain = new JsonObject("type", "Ex.Terrain");
		components.Add(terrain);
		JsonObject data = new JsonObject();
		terrain["data"] = data;

		data["tileSize"] = Helpers.Pack(tileSize);
		data["viewDist"] = viewDist;
		data["meshSamples"] = meshSamples;
		data["splatSamples"] = splatSamples;
		data["slopeAngle"] = slopeAngle;
		data["seed"] = seed;
		data["chunk"] = objects[0].name;
		data["shader"] = shader.name;
		data["heightmapKernelName"] = heightmapKernelName;
		data["splatmapKernelName"] = splatmapKernelName;
		data["terrainBaseLayer"] = terrainLayers[0].name;
		data["terrainCliffLayer"] = terrainLayers[1].name;
		for (int i = 2; i < terrainLayers.Length; i++) {
			data["terrainLayer"+(i-1)] = terrainLayers[i].name;
		}
		data["objectNoise"] = Helpers.Pack(objectNoise);
		data["heightmapNoise"] = Helpers.Pack(heightmapNoise);
		data["splatmapNoise"] = Helpers.Pack(splatmapNoise);
		data["objectUberNoise"] = Helpers.Pack(objectUberNoise);
		data["heightmapUberNoise"] = Helpers.Pack(heightmapUberNoise);
		data["splatmapUberNoise"] = Helpers.Pack(splatmapUberNoise);
		data["extra"] = Helpers.Pack(craterData);

		return obj.PrettyPrint();
	}

	private static class Helpers {
		public static JsonArray Pack(Vector2 v) { return new JsonArray(v.x, v.y); }
		public static JsonArray Pack(Vector3 v) { return new JsonArray(v.x, v.y, v.z); }
		public static JsonArray Pack(Vector4 v) { return new JsonArray(v.x, v.y, v.z, v.w); }

		public static JsonArray Pack(SimplexNoise n) {
			return new JsonArray(n.octaves, 
				n.persistence, n.scale, n.octaveScale,
				n.noiseOffset.x, n.noiseOffset.y, n.noiseOffset.z);
		}
		public static JsonArray Pack(UberData u) {
			return new JsonArray(u.octaves,
				u.perturb, u.sharpness, u.amplify,
				u.altitudeErosion, u.ridgeErosion, u.slopeErosion,
				u.lacunarity, u.gain, u.startAmplitude, u.scale);
		}
		public static JsonArray Pack(CraterData c) {
			JsonArray a = new JsonArray(c.craterEffect, 
				c.craterPoint, 
				c.warpAmount);
			a.AddAll(Pack(c.noise));
			a.AddAll(Pack(c.comp));
			a.Add(c.craterMin);
			a.Add(c.craterMax);
			a.Add(c.craterLip);
			return a;
		}
	}

}










