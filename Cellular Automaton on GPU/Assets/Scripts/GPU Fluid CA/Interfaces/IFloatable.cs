﻿using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IFloatable : MonoBehaviour {

    public float density;

    public float floatHeight = 0;
    public float bounceDamp = 0.05f;
    public Vector3 buoyancyOffset;

    public float waterLevel = 0;
    private Rigidbody rb;

    private bool floating;

	void Start ()
    {
        rb = GetComponent<Rigidbody>();
	}
	
    private float forceFactor;
    private Vector3 floatingPoint;
    private Vector3 upLift;

    void FixedUpdate ()
    {
        if (floatHeight > 0)
        {
            floatingPoint = transform.position + transform.TransformDirection(buoyancyOffset);
            forceFactor = 1f - ((floatingPoint.y - waterLevel) / floatHeight);
            forceFactor *= (1.5f + Time.deltaTime * 50f);
            if (forceFactor > 0f)
            {
                upLift = -Physics.gravity * (forceFactor - rb.velocity.y * bounceDamp);
                rb.AddForceAtPosition(upLift, floatingPoint);
            }
        }
        //maybe not the best solution to calculate if floating
        if (waterLevel + ((2f*floatHeight)/3f) > transform.position.y) floating = true;
        else floating = false;
    }

    public void SetWaterLevel(float waterLevel)
    {
        this.waterLevel = waterLevel;
    }

    public bool isFloating()
    {
        return floating;
    }

}
