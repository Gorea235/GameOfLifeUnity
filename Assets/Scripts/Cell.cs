using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
	public SpriteRenderer spriteRenderer;

	void Awake ()
	{
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
	}

	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}
}
