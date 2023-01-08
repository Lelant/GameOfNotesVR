using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiateNotes : MonoBehaviour {

	public Transform prefab;
	public int amount;
	public float radius;

	// Use this for initialization
	void Awake () {
		for(int i=0; i<amount; i++){
            Vector3 pos = new Vector3(Random.Range(-radius, radius), Random.Range(0, radius * 1.5f), Random.Range(-radius, radius));
			Instantiate(prefab, pos, Quaternion.identity);
		}
	}
}
