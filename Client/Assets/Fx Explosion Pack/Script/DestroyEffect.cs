using UnityEngine;
using System.Collections;

public class DestroyEffect : MonoBehaviour {

	[SerializeField]
	private ParticleSystem ps;

	void Update ()
	{
		if (ps.IsAlive() == false)
		{
			Destroy(transform.gameObject);
		}
	}
}
