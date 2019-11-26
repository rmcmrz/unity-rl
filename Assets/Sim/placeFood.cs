using UnityEngine;
using System.Collections.Generic;
using Apex.WorldGeometry;
using System.Linq;
using Apex.Units;


    public class placeFood : MonoBehaviour
    {

        private IGrid mainGrid;

        private Object foodSource;

        private float rate = 0.2f;

        private List<GameObject> generatedFood;

        private List<Cell> foodCells;

        public enum terrainType { medium, good, bad };

        public terrainType selectedTerrain;

        private int range;

        private infopointDynamics infopointScript;

        public float foodRatio = 10f;

        private int totalCells;

        private List<Cell> cellList, cellsClose;

        private IUnitFacade _unit;

        public int score;

    // Use this for initialization
    void Start()
        {

            generatedFood = new List<GameObject>();

            foodCells = new List<Cell>();

            foodSource = Resources.Load("Prefabs/food");

            mainGrid = GridManager.instance.GetGrid(new Vector3(0, 0, 0));

            IEnumerable<Cell> allCells = mainGrid.cells;

            totalCells = mainGrid.sizeX * mainGrid.sizeZ;

            int totalFood = Mathf.RoundToInt((totalCells) / foodRatio);

            Debug.Log("Total " + totalFood + " out of " + totalCells);


            cellsClose = new List<Cell>();

            cellList = allCells.ToList();

            while (cellList.Count > totalFood)
            {
                //Debug.Log(cellList.Count);

                Cell current = cellList[Mathf.RoundToInt(Random.Range(0, cellList.Count))];

                if (generateFood(current))
                {

                    cellList.Remove(current);

                    cellList.Remove(current.GetNeighbour(0, 1));

                    cellList.Remove(current.GetNeighbour(0, -1));

                    cellList.Remove(current.GetNeighbour(-1, 0));

                    cellList.Remove(current.GetNeighbour(1, 0));

                }


            }

            int quarter = Mathf.RoundToInt(generatedFood.Count / 4);

            if (selectedTerrain == terrainType.bad)
            {

                range = quarter;

            }
            else if (selectedTerrain == terrainType.medium)
            {

                range = quarter * 2;

            }
            else if (selectedTerrain == terrainType.good)
            {

                range = quarter * 3;
            }


            for (int i = 0; i < generatedFood.Count; i++)
            {

                generatedFood[i].layer = 8;
                generatedFood[i].tag = "info";
                generatedFood[i].name = "info" + i;

            }

            int total = generatedFood.Count;

            range = generatedFood.Count;

            while (generatedFood.Count > range)
            {


                GameObject current = generatedFood[Mathf.RoundToInt(Random.Range(0, generatedFood.Count))];

                infopointScript = current.GetComponent<infopointDynamics>();

                infopointScript.disposition = infopointDynamics.possibleDispositions.Bad;

                generatedFood.Remove(current);


            }


            Debug.Log("Generated " + generatedFood.Count + " good out of " + total);


        }

        bool generateFood(Cell cell)
        {
            /*
            if (cellsClose.Contains(cell))
                return false;
            */

            Vector3 position = cell.position;

            position.y = 0.05f;

            generatedFood.Add(Instantiate(foodSource, position, Quaternion.identity) as GameObject);

            //foodCells.Add(cell);

            /*

             cellsClose.Add(cell.GetNeighbour(0, 1));

             cellsClose.Add(cell.GetNeighbour(0, -1));

             cellsClose.Add(cell.GetNeighbour(-1, 0));

             cellsClose.Add(cell.GetNeighbour(1, 0));

             cellsClose.Add(cell);

             */

            return true;

        }

        // Update is called once per frame
        void Update()
        {

        }
    }