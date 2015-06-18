using UnityEngine;
using System.Collections;

public class PerlinShake : MonoBehaviour
{

		//variables for shaking the camera during cut scenes
		public float duration = 0.5f;
		public float speed = 1.0f;
		public float magnitude = 0.1f;

		public bool test = false;
		private bool isShaking = false;
	
		public GameObject camObject;
		private float x;
		private float y;
		private float z;
	



		void Start ()
		{
			if (camObject == null) {
				camObject = GameObject.FindGameObjectWithTag ("MainCamera");			// We're storing our tags for easy reference
			}
		}
	
		void Update ()
		{

		
				if (test == true) {
					PlayShake ();
					test = false;
					Debug.Log("Shaking");
				}
		
		//		Debug.Log (camObject);
				if (!isShaking)
						return;

				x = camObject.transform.localPosition.x;
				y = camObject.transform.localPosition.y;
				z = camObject.transform.localPosition.z;

	
		}


		public void PlayShake ()
		{
				isShaking = true;
				StartCoroutine ("Shake");
		}

		public IEnumerator Shake ()
		{
				float elapsed = 0.0f;
				Vector3 originalCamPos = new Vector3 (x, y, z);
				float randomStart = Random.Range (-1000.0f, 1000.0f);
		
				while (elapsed < duration) {
			
						elapsed += Time.deltaTime;			
						float percentComplete = elapsed / duration;			
			
						// Reduce the shake from full power to 0 starting half way through
						float damper = 1.0f - Mathf.Clamp (2.0f * percentComplete - 1.0f, 0.0f, 1.0f);
			
						// Calculate the noise parameter starting randomly and going as fast as speed allows
						float alpha = randomStart + speed * percentComplete;

						x = Util.Noise.GetNoise (alpha, 0.0f, 0.0f) * 2.0f - 1.0f;
						y = Util.Noise.GetNoise (0.0f, alpha, 0.0f) * 2.0f - 1.0f;
			
						x *= magnitude * damper;
						y *= magnitude * damper;
			
						camObject.transform.localPosition = new Vector3 (x, y, z);
				
						yield return null;
				}
				camObject.transform.localPosition = originalCamPos;
				isShaking = false;
		}

}

//void OnTriggerEnter2D (Collider2D other)
//{
//	
//	if (other.gameObject.tag == GameTags.Player) {
//		if (isShakey && shakeRig != null) {
//			PerlinShake p = shakeRig.GetComponent<PerlinShake> () as PerlinShake;
//			if (p)
//				p.PlayDeathShake ();
//		}
//		
//	}
//}
