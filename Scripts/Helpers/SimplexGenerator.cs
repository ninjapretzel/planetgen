using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ex.Data; // Note: in ExServer repository

using static UnityEngine.Mathf;

public class SimplexGenerator : MonoBehaviour {
	
	public string group = "Terrain";
	public Transform[] objects;
	public Vector3 repeating = new Vector3(3, 1, 3);
	public Vector3 noiseOffset = new Vector3(0, 55, 0);
	public float offset = 40;
	
	public Dictionary<Vector3, Transform> map;
	public HashSet<Vector3> toCull;
	public HashSet<Vector3> toFill;

	public bool delayFill = true;
	public int maxPerFrame = 1;
	public bool lockY = false;

	public bool cullRadial = true;
	public bool destroyDistant = false;
	public SimplexNoise noise = SimplexNoise.Defaults;
	
	public bool fillOnStart = false;
	
	Transform objectsDump;
	
	Vector3 lastCenter;
	Vector3 lastRepeat;
	Vector3 lastExtents;
	float area;
	float initialYPosition;
	//public static TerrainGenerator main;
	
	void Awake() {
		
		map = new Dictionary<Vector3, Transform>();
		toCull = new HashSet<Vector3>();
		
		objectsDump = new GameObject(group).transform;
		
	}
	
	void Start() {

		initialYPosition = transform.position.y;

		if (fillOnStart) {
			Fill(transform.position, repeating);
			
		}
		
	}
	
	void LateUpdate() {
		if (objects == null || objects.Length == 0) { return; }
		PositionSelfOnGrid();

		if (delayFill) {
			DelayFill(transform.position, repeating, maxPerFrame);
		} else {
			Fill(transform.position, repeating);
		}
		
		CheckCull();
		Cull();
	}

	public void Clear() {
		foreach (var pair in map) {
			Destroy(pair.Value.gameObject);
		}
		toCull.Clear();
		map.Clear();
	}

	void DelayFill(Vector3 center, Vector3 repeat, int maxPerFrame) {
		Vector3 pos = center;
		Vector3 extents = (repeat - Vector3.one) * offset / 2;
		pos -= extents;
		Vector3 start = pos;

		lastCenter = center;
		lastRepeat = repeat;
		lastExtents = extents;
		if (cullRadial) {
			area = Max(Max(extents.x, extents.y), extents.z);
			area *= area;
		} else {
			area = extents.sqrMagnitude;
		}

		Transform t;
		int created = 0;
		List<Vector3> vs = new List<Vector3>((int)(repeat.x * repeat.y * repeat.z));
		for (int xx = 0; xx < repeat.x; xx++) {
			pos.x = Mathf.Floor(start.x + offset * xx);
			if (lockY) { repeat.y = 1; }
			for (int yy = 0; yy < repeat.y; yy++) {
				pos.y = Mathf.Floor(start.y + offset * yy);
				for (int zz = 0; zz < repeat.z; zz++) {
					pos.z = Mathf.Floor(start.z + offset * zz);
					vs.Add(pos);
				}
			}
		}
		vs.Sort((a,b) => { 
			float aDist = (a-center).sqrMagnitude;
			float bDist = (b-center).sqrMagnitude;
			if (aDist < bDist) { return -1; }
			if (bDist < aDist) { return 1; }
			return 0;
		});

		for (int i = 0; i < vs.Count; i++) {
			pos = vs[i];
			if (ShouldBeCulled(pos)) {
				continue;
			}

			if (created >= maxPerFrame) {

			} else { 
				if (map.ContainsKey(pos)) {
					t = map[pos];
					if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); }
				} else {
					t = Instantiate(Choose(pos), pos, Quaternion.identity) as Transform;
					t.parent = objectsDump;
					t.gameObject.SetActive(true);
					map.Add(pos, t);
					created++;
				}
			}

		}

		

	}

	void Fill(Vector3 center, Vector3 repeat) {
		Vector3 pos = center;
		Vector3 extents = (repeat - Vector3.one) * offset / 2;
		pos -= extents;
		Vector3 start = pos;
		
		lastCenter = center;
		lastRepeat = repeat;
		lastExtents = extents;
		
		if (cullRadial) {
			area = Max(Max(extents.x, extents.y), extents.z);
			area *= area;
		} else {
			area = extents.sqrMagnitude;
		}

		Transform t;
		for (int xx = 0; xx < repeat.x; xx++) {
			pos.x = Mathf.Floor(start.x + offset * xx);
			if (lockY) { repeat.y = 1; }
			for (int yy = 0; yy < repeat.y; yy++) {
				pos.y = Mathf.Floor(start.y + offset * yy);

				for (int zz = 0; zz < repeat.z; zz++) {
					pos.z = Mathf.Floor(start.z + offset * zz);
				

					if (ShouldBeCulled(pos)) {
						continue; 
					} else if (map.ContainsKey(pos)) {
						t = map[pos];
						if (!t.gameObject.activeSelf) { t.gameObject.SetActive(true); }
					} else {
						t = Instantiate(Choose(pos), pos, Quaternion.identity) as Transform;
						t.parent = objectsDump;
						t.gameObject.SetActive(true);
						map.Add(pos, t);
					}
				
				}
			}
			
		}
		
	}
	
	
	void PositionSelfOnGrid() {
		Vector3 pos = transform.position;
		
		pos.x = Mathf.Round(pos.x / offset) * offset;
		if (!lockY) { pos.y = Mathf.Round(pos.y / offset) * offset; }
		else { pos.y = initialYPosition; }
		pos.z = Mathf.Round(pos.z / offset) * offset;
		
		transform.position = pos;
	}
	
	Transform Choose(Vector3 pos) {
		// If we don't need to choose, don't. ez.
		if (objects.Length == 1) { return objects[0]; }

		float val = noise.FBM3(pos.x, pos.y, pos.z, 0, .99999f);
		return objects[(int)(val * objects.Length)];
	}
	
	void CheckCull() {
		toCull = toCull ?? new HashSet<Vector3>();
		toCull.Clear();
		foreach (Vector3 v in map.Keys) {
			if (ShouldBeCulled(v) && !toCull.Contains(v)) {
				toCull.Add(v);
			}
		}
	}
	
	void Cull() {
		if (toCull.Count > 0) {
			foreach (Vector3 v in toCull) {
				if (map.ContainsKey(v)) {
					Transform t = map[v];
					if (destroyDistant) {
						
						Destroy(t.gameObject);
						map.Remove(v);
						
					} else {
					
						if (t.gameObject.activeSelf) {
							t.gameObject.SetActive(false);
						}
						
					}
				}
				
			}
			
		}
		
	}
	
	bool ShouldBeCulled(Vector3 pos) {
		
		float length = (pos - lastCenter).sqrMagnitude;
		return length > area;
		
	}
	
}














































