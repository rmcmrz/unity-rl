 using UnityEngine;
 
 public class bounceAngle : MonoBehaviour
 {


 	private Collider c;

 	void Start()
 	{


 		c = gameObject.GetComponent<Collider>();

 	}

     void OnCollisionEnter(Collision collision)
    {
         if (collision.gameObject.tag == "agent")
        {
            Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), c);
        }
 	}

}