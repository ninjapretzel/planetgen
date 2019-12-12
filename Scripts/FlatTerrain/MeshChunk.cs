using UnityEngine;
using System.Collections;

public class MeshChunk : MonoBehaviour {
	
	Vector3 center { get { return new Vector3(transform.position.x, 0, transform.position.z); } }
	
	Texture2D splatmap;
	
	public TerrainGenerator terrainGenerator;
	
	public TerrainData baseData;

	
	void Start() {
		
		if (terrainGenerator == null) {
			var gen = GameObject.Find("TerrainGenerator");
			if (gen != null) { terrainGenerator = gen.GetComponent<TerrainGenerator>(); }
			if (terrainGenerator == null) {
				Debug.LogWarning("No terrain generator found!");
			}
		}

		TerrainData data = terrainGenerator.CloneTerrain(baseData);
		data.name = "Terrain at " + center;
		terrainGenerator.ProcessHeights(data, center);

		GameObject terrainObj = Terrain.CreateTerrainGameObject(data);
		Terrain terrain = terrainObj.GetComponent<Terrain>();
		//terrain.castShadows = false;
		terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		terrain.allowAutoConnect = false;
		terrainGenerator.ProcessSplats(terrain, center);

		Transform tt = terrainObj.transform;
		tt.SetParent(transform);
		tt.localPosition = -terrainGenerator.tileSize;
		
	}
	
}
