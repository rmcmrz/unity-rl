namespace Apex.Steering.Behaviours
{

    using UnityEngine;
    using System.Collections;
    using Apex.WorldGeometry;
    using Apex.Units;

    public class testMove : MonoBehaviour
    {

        IUnitFacade _unit;

        // Use this for initialization
        void Start()
        {

            IGrid  mainGrid = GridManager.instance.GetGrid(new Vector3(0, 0, 0));

            _unit = this.GetUnitFacade();
            if (_unit == null)
            {
                Debug.LogError("WanderBehaviour requires a component that implements IMovable.");
                this.enabled = false;
            }

            Cell destination = mainGrid.GetCell(new Vector3(-10, 0, 10));


            _unit.MoveTo(destination.position, false);

            Debug.Log(destination.matrixPosX + " " + destination.matrixPosZ);

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}