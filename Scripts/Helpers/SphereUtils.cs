using UnityEngine;
using static UnityEngine.Mathf;

public static class FHelpers {
	public static bool IsInfinity(this float f) {
		return (f == Infinity) || (f == -Infinity);
	}
}

public static class SphereUtils {

	/// <summary> Gets a position on the unit cube for the given direction. 
	/// This allows positions on a sphere to be mapped to a cube for storage purposes. </summary>
	/// <param name="dir"> Direction to map </param>
	/// <returns> Vector containing normalized components of the given direction, 
	/// and the component for the 'side' of the cube the vector is on set to MathF.Infinity or MathF.NegativeInfinity
	/// for clarity (since edge cases can end up with two sides </returns>
	public static Vector3 UnitCube(Vector3 dir) {
		dir = dir.normalized;
		Vector3 adir = new Vector3(Abs(dir.x), Abs(dir.y), Abs(dir.z));

		Vector3 side = (adir.x > adir.y)
			? (adir.x > adir.z ? Vector3.right : Vector3.forward)
			: (adir.y > adir.z ? Vector3.up : Vector3.forward);

		if (side.x > 0) {
			return new Vector3(dir.x > 0 ? Infinity : -Infinity, dir.y, dir.z);
		} else if (side.y > 0) {
			return new Vector3(dir.x, dir.y > 0 ? Infinity : -Infinity, dir.z);
		} else if (side.z > 0) {
			return new Vector3(dir.x, dir.y, dir.z > 0 ? Infinity : -Infinity);
		}
		return Vector3.zero;
	}

	/// <summary> Restores an infinity cube coordinate to a unit cube coordinate, 
	/// by replacing any Infinities with 1 or -1 </summary>
	/// <param name="p"> Vector to restore </param>
	/// <returns> Restored vector with any infinities set to 1 or -1 </returns>
	public static Vector3 Uninfinify(Vector3 p) {
		if (p.x == Infinity) { p.x = 1; }
		if (p.x == -Infinity) { p.x = -1; }

		if (p.y == Infinity) { p.y = 1; }
		if (p.y == -Infinity) { p.y = -1; }

		if (p.z == Infinity) { p.z = 1; }
		if (p.z == -Infinity) { p.z = -1; }
		return p;
	}

	/// <summary> Gets the UnitCube cell position of a position/direction on a Sphere 
	/// With a given number of divisions (Default 10x10x10) </summary>
	/// <param name="dir"> Direction to get a coordinate for </param>
	/// <param name="divisions"> Number of divisions on the UnitCube </param>
	/// <returns> Lower end of coordinate on unit cube. </returns>
	public static Vector3 UnitCell(Vector3 dir, int divisions = 10) {

		float per = 1.0f / divisions;
		Vector3 unitPos = UnitCube(dir);

		if (unitPos.x == Infinity || unitPos.x == -Infinity) {
			int divy = (int)(unitPos.y * divisions);
			int divz = (int)(unitPos.z * divisions);
			return new Vector3(unitPos.x, divy * per, divz * per);
		} else if (unitPos.y == Infinity || unitPos.y == -Infinity) {
			int divx = (int)(unitPos.x * divisions);
			int divz = (int)(unitPos.z * divisions);
			return new Vector3(divx * per, unitPos.y, divz * per);
		} else if (unitPos.z == Infinity || unitPos.z == -Infinity) {
			int divx = (int)(unitPos.x * divisions);
			int divy = (int)(unitPos.y * divisions);
			return new Vector3(divx * per, divy * per, unitPos.z);
		}

		return Vector3.zero;
	}

	/// <summary> Normalized point for a given angle on a unit sphere </summary>
	/// <param name="angle"> Angle, x is azimuth, y is altitude </param>
	/// <returns> Point for angle on unit sphere </returns>
	public static Vector3 SpherePoint(Vector2 angle) {
		float azimuth = angle.x;
		float altitude = angle.y;

		float s0 = Sin(azimuth * Deg2Rad);
		float c0 = Cos(azimuth * Deg2Rad);

		float s1 = Sin(altitude * Deg2Rad);
		float c1 = Cos(altitude * Deg2Rad);

		return new Vector3(c0, 0, s0) * c1
			+ new Vector3(0, s1, 0);
	}


	public static Mesh MakePlanetaryMesh(Vector2 start, Vector2 sweep, float minRadius, int meshSegments = 32) {

		int neededVerts = (int)Pow(meshSegments * 2, 2);
		int neededTris = 2 * (int)Pow(meshSegments, 2);

		Vector3[] vertices = new Vector3[neededVerts];
		Vector3[] normals = new Vector3[neededVerts];
		Vector2[] uv = new Vector2[neededVerts];
		int[] triangles = new int[neededTris * 3];

		Vector2 sweepPer = sweep / meshSegments;
		int cnt = meshSegments * meshSegments;
		float xx, yy;
		for (int i = 0; i < cnt; i++) {

			xx = Floor(i % meshSegments) / meshSegments;
			yy = Floor(i / meshSegments) / meshSegments;




		}



		return MeshHelpers.MakeMesh(vertices, uv, triangles, normals);
	}



	public static int step = 10;
	public static void OnDrawGizmos() {
		Color[] colors = new Color[] {
			Color.green, Color.blue, Color.yellow, Color.red
		};
		Plane xP = new Plane(Vector3.right, 1);
		Plane xN = new Plane(Vector3.left, -1);
		Plane yP = new Plane(Vector3.up, 1);
		Plane yN = new Plane(Vector3.down, -1);
		Plane zP = new Plane(Vector3.forward, 1);
		Plane zN = new Plane(Vector3.back, -1);

		// Extent is -1, 1
		Bounds b = new Bounds(Vector3.zero, Vector3.one * 2);

		for (float yy = -90; yy < 90; yy += step) {


			for (float xx = 0; xx < 360; xx += step) {

				Vector3 p00 = SpherePoint(new Vector2(xx - 45, yy));
				Vector3 p01 = SpherePoint(new Vector2(xx - 45, yy + step));
				Vector3 p10 = SpherePoint(new Vector2(xx - 45 + step, yy));

				int p = (int)(xx / 90.0f);
				if (yy > -45 && yy < 45) {
					Gizmos.color = colors[p % 4];
				} else {
					Gizmos.color = Color.gray;
				}
				Color c = Gizmos.color;
				c.a = .3f;
				Gizmos.color = c;
				Gizmos.DrawLine(p00, p01);
				Gizmos.DrawLine(p00, p10);
				c.a = .8f;
				Gizmos.color = c;

				Vector3 dc00 = UnitCell(p00);
				Vector3 stepA = dc00.x.IsInfinity() ? Vector3.up : Vector3.right;
				Vector3 stepB = dc00.z.IsInfinity() ? Vector3.up : Vector3.forward;
				stepA *= .1f;
				stepB *= .1f;

				Vector3 dc10 = dc00 + stepA;
				Vector3 dc01 = dc00 + stepB;
				Vector3 dc11 = dc00 + stepA + stepB;
				dc00 = Uninfinify(dc00);
				dc10 = Uninfinify(dc10);
				dc01 = Uninfinify(dc01);
				dc11 = Uninfinify(dc11);

				Gizmos.DrawLine(dc00, dc10);
				Gizmos.DrawLine(dc00, dc01);
				Gizmos.DrawLine(dc10, dc11);
				Gizmos.DrawLine(dc01, dc11);



				Vector3 udc = Uninfinify(dc00);
				Gizmos.color = new Color(1,1,1,.3f);
				Gizmos.DrawLine(p00, udc);
			}
		}
	}


}
