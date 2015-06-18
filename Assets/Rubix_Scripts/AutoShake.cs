using UnityEngine;
using System.Collections;

public class AutoShake : MonoBehaviour {
	private GameObject shakeRig;
	private bool playShakeing = true;
	
	void Start(){
		//shakeRig = GameObject.FindGameObjectWithTag (GameTags.MainCamera);

		if (playShakeing == true) {
			//shakeRig.GetComponent<PerlinShake> ().PlayDeathShake ();
			playShakeing = false;
		}
	}
}
