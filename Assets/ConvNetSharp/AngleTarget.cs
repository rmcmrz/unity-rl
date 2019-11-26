    
    using UnityEngine;

    public class AngleTarget : MonoBehaviour
    {

        public Transform target;

        private float CalculateAngle(Vector3 from, Vector3 to)
        {


            return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.z;

        }



        void Update()
        {


            float angle = CalculateAngle(transform.position,target.position);


            Debug.Log(angle);


        }




    }