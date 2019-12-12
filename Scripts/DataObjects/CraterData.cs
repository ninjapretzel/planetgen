using UnityEngine;
using Ex.Data;  // Note: in ExServer repository
using System.Runtime.InteropServices;

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct CraterData {

	public float craterEffect;
	public float craterPoint;
	public float warpAmount;

	public SimplexNoise noise;

	public Vector4 comp;
	public float craterMin;
	public float craterMax;
	public float craterLip;

}
