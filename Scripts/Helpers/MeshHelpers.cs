using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class MeshHelpers {

	public static Mesh Make(Vector3[] vertices, Vector2[] uv, int[] triangles) {
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh MakeMesh(Vector3[] vertices, Vector2[] uv, int[] triangles, Vector3[] normals) {
		Mesh mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		return mesh;
	}

	public static void RecalculateTangents(Mesh mesh) {
		Vector4[] tans = new Vector4[mesh.normals.Length];

		Vector4 t1;
		Vector4 t2;

		for (int i = 0; i < tans.Length; i++) {
			t1 = Vector3.Cross(mesh.normals[i], Vector3.forward);
			t2 = Vector3.Cross(mesh.normals[i], Vector3.up);

			if (t1.magnitude > t2.magnitude) { tans[i] = t1; } else { tans[i] = t2; }

			tans[i].w = 0;
			tans[i].Normalize();
			tans[i].w = 1;
		}

		mesh.tangents = tans;
	}

	public static T DeepCopy<T>(T obj) {
		MemoryStream ms = new MemoryStream();
		BinaryFormatter bf = new BinaryFormatter();

		bf.Serialize(ms, obj);
		ms.Seek(0, SeekOrigin.Begin);
		T retval = (T)bf.Deserialize(ms);

		ms.Close();
		return retval;
	}

}

