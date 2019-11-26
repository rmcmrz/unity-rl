using UnityEngine;
using System.Collections;

public class infoPointsGenerator : MonoBehaviour {

	// Use this for initialization
	void Start () {

		RandomGenerate ();
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}



	void RandomGenerate() {
		Vector3 v3T = Vector3.zero;
		Vector3[] arv3 = new Vector3[60];
		float fMinDist = 10.0f;   // Minimum distance they need to be apart
		int iMaxTries = 1000;    // Number of times to try to generate a position
		float fMinTry = Mathf.Infinity;
		
		int i;
		for (i = 0; i < 20; i++) {
			
			for (int j = 0; j < iMaxTries; j++) {
				v3T.x = Random.Range (-200.0f, 200.0f);
				v3T.z = Random.Range (-200.0f, 200.0f);
				
				fMinTry = Mathf.Infinity; 
				for (int k = 0; k < i; k++)
					fMinTry = Mathf.Min ((v3T - arv3[k]).magnitude, fMinTry);
				
				if (fMinTry > fMinDist) // Far enough apart
					break;
			}
			if (fMinTry < fMinDist) {
				Debug.Log ("Generation failed -- only found " + i + " points");
				break;
			}
			arv3[i] = v3T;
		}
		
		for (int j = 0; j < i; j++) {
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
			go.transform.position = arv3[j];
			go.layer = 8;
		}
		
	}
}
