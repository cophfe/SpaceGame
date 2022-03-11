using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JankBody : MonoBehaviour
{
	public BoxCollider2D coffin;
	public LayerMask player;

	bool floppy = false;

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (!floppy && ((1 << collision.gameObject.layer) & player) != 0)
		{
			Destroy(coffin.gameObject);
			floppy = true;
			var rbs = GetComponentsInChildren<Rigidbody2D>();
			foreach (var rb in rbs)
			{
				rb.simulated = true; 
			}
		}
	}
}
