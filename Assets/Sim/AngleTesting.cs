    
    using UnityEngine;

    public class AngleTesting : MonoBehaviour
    {

        public Transform target;

        float calculateAngle(Vector3 target)
        {

        Vector3 targetDir = target - transform.position;

        float angle = Vector3.Angle(targetDir, transform.forward);

        float sign = Mathf.Sign(Vector3.Dot(targetDir, transform.right));

        float totalAngle = sign * angle;

        int action = 4;

        if(totalAngle >= 45 && totalAngle < 135)
        {

            action = 0;

        }
        else if(totalAngle < -45 && totalAngle >= -135)
        {

            action = 1;
        }
        else if(totalAngle >= -45 && totalAngle < 45)
        {

            action = 2;
        }
        else if( (totalAngle >= 135 && totalAngle <= 180 ) || ( totalAngle < -135 && totalAngle >= -180 ) )
        {

            action = 3;
        }

        Debug.Log(action+"  b "+totalAngle);

        return totalAngle;


        }



        void Update()
        {


            float angle = calculateAngle(target.transform.position);
           


            //Debug.Log(angle);


        }




    }