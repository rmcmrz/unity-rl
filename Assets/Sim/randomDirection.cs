using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomDirection : MonoBehaviour
{

    private Rigidbody rb;

    private int speed = 150;

    private int direction;

    private Vector3 directionv;

    GameObject camera;

    // Use this for initialization
    void Start()
    {
        camera = GameObject.Find("ExampleGround");

        transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);

        directionv = transform.forward * Random.Range(150,200);

        rb = GetComponent<Rigidbody>();

        rb.AddForce(directionv);

        InvokeRepeating("kill", 300f, 99999999999f);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.velocity = Random.Range(1, 10) * (rb.velocity.normalized);

    }

    void kill()
    {

        infopointDynamics infopointDynamicsscript = gameObject.GetComponent<infopointDynamics>();

        if(!infopointDynamicsscript.isGoal)
        {

        Destroy(gameObject);

        movingFood movingFoodScript = camera.GetComponent<movingFood>();

        movingFoodScript.foodCount--;

        }

    }
}
