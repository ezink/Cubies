using UnityEngine;
using System.Collections;

public class PolygonRenderOrder : MonoBehaviour {

	public int sortingLayer;

	void Start()
	{
		transform.GetComponent<Renderer>().sortingLayerName = "character";
		transform.GetComponent<Renderer>().sortingOrder = sortingLayer;
	}
}
