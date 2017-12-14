﻿using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 10.0f; // 速度

	[SerializeField]
	GameObject hoge;

    void Start()
    {
    }

    // 固定フレームレートで呼び出されるハンドラ
    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        var rb = GetComponent<Rigidbody>();

        // 速度の設定
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        rb.AddForce(movement * speed);
    }

	private void OnCollisionEnter(Collision collision)
	{
		Instantiate(hoge, transform.position, transform.rotation);
	}
}
