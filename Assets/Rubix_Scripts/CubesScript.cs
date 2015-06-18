using UnityEngine;
using System.Collections;

public class CubesScript : MonoBehaviour {
    //To store this piece location inside cube array in Main script
    //public Vector3 _thisCubeLocation, = new Vector3(-1, -1, -1);

	public Vector3 _thisCubeLocation;

	void Awake(){

		_thisCubeLocation = new Vector3(-1, -1, -1);
	}

}