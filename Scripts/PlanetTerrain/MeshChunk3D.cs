using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MeshChunk3D : MonoBehaviour {

	Vector3 center { get { return transform.position; } }
	public Vector3 extents = Vector3.one;
	
	public PlanetaryVoxelGenerator generator;
	public List<GameObject> children;
	public int lod = 64;
	public int lastLod = -1;

	void Start() {

	}

	void Update() {

		if (generator != null) {

			lod = generator.maxLOD;
			float distance = (transform.position - generator.tracked.position).magnitude;
			if (distance > generator.lodDist) {
				lod = 0;
			} else {
				float falloffAmount = distance * generator.lodFalloff / (generator.cubeSize * 2);
				int halves = (int)falloffAmount;
				if (halves < 0) { halves = 0; }
				lod = lod >> halves;

			}
			if (lod < 4) { lod = 4; }

			extents = Vector3.one * generator.cubeSize;

			if (lod != lastLod) {
				Regen();
			}
			lastLod = lod;

		}

		

	}

	public void Regen() {
		if (children == null) {
			children = new List<GameObject>();
		}

		var meshes = generator.ProcessMesh(center, extents, lod);
		var mat = GetComponent<MeshRenderer>().sharedMaterial;

		int max = Mathf.Max(meshes.Count, children.Count);
		for (int i = 0; i < max; i++) {

			if (children.Count == i) {
				var child = new GameObject("MeshChunk3d Child " + children.Count);
				child.transform.parent = transform;
				children.Add(child);
			}

			var filter = children[i].GetComponent<MeshFilter>();
			var collider = children[i].GetComponent<MeshCollider>();
			var renderer = children[i].GetComponent<MeshRenderer>();

			if (filter == null) { filter = children[i].AddComponent<MeshFilter>(); }
			if (collider == null) { collider = children[i].AddComponent<MeshCollider>(); }
			if (renderer == null) { renderer = children[i].AddComponent<MeshRenderer>(); }

			if (i < meshes.Count) {

				children[i].SetActive(true);
				filter.sharedMesh = meshes[i];
				collider.sharedMesh = meshes[i];
				renderer.sharedMaterial = mat;

			} else {

				children[i].SetActive(false);
				filter.sharedMesh = null;
				collider.sharedMesh = null;

			}

		}
			
			
	}
}
