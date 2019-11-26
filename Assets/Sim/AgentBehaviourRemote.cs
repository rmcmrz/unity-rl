
namespace Apex.Steering.Behaviours
{
	using System.Collections;
    using Apex.Messages;
    using Apex.Services;
    using Apex.Steering.Components;
    using Apex.Units;
    using Apex.WorldGeometry;
    using UnityEngine;
	using System.Collections.Generic;
    using System.Linq;
    using Accord.Neuro;
    using Accord.Neuro.ActivationFunctions;
    using Accord.Neuro.Networks;
    using AForge.Neuro.Learning;
    using ConvNetSharp;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// A steering behaviour that will make the unit to which it is attached, wander around within a certain radius.
    /// </summary>
    [AddComponentMenu("Apex/Navigation/Behaviours/Wander")]
    [ApexComponent("Behaviours")]
    public class AgentBehaviourRemote : ExtendedMonoBehaviour, IHandleMessage<UnitNavigationEventMessage>
    {
        public float fieldOFView = 45f; //field of view

		public float radiusWander = 10.0f;

        /// <summary>
        /// The minimum distance of a wander route
        /// </summary>
        public float minimumDistance = 90.0f;

        /// <summary>
        /// The time in seconds that the unit will linger after each wander route before moving on.
        /// </summary>
        public float lingerForSeconds = 25.0f;


		public float aquireInfoDuration = 10.0f;

        /// <summary>
        /// If unable to find a spot to wander to after having tried <see cref="bailAfterFailedAttempts"/> no more attempts will be made.
        /// </summary>
        public int bailAfterFailedAttempts = 100000000;

        private IUnitFacade _unit;
        //private Vector3 _startPos;


		public GameObject[] objectsClose;

		private GameObject nearestAgent;

		private GameObject nearestInfopoint;


		private infopointDynamics infopointDynamicsScript;

		private AgentBehaviourRemote AgentBehaviourScript;

		private AgentBehaviourRemote AgentBehaviourScriptCommunicating;

		private List<AgentBehaviourRemote> behaviourScriptList;

		private simulationAgentEncapsulator agentEncapsulatorScript;

		public enum agentsActionStatus { Wandering , MovingToCommunicate , MovingToInfo, Communicating, GettingInfo };
		public agentsActionStatus currentActionStatus;

		public enum possibleActions { Up, Down, Left, Right };


		private int rayAngle = 12;



		private int range,interactRange;

        public int numberOfDirections;

		private GameObject[] directionsState;

		private double[] directionsVector;

		private double[,] directionsMatrix;

		private double[,] stateMatrix;

		public double[] stateMatrixFeatures;

		public double[] onedimensionalFeatures;

		public double[] previousfeaturesVector;

		private double[] weightsVector;

		private float step;

		private int numberOfAgents;

		private int numberOfAgentse;

		private int numberOfAgentsi;

		private int numberOfAgentsc;

		private int numberOfAgentss;

		private int numberofGoodInfopoints;

		private int numberofBadInfopoints;

		private int vectorSize;

		private int matrixLength;

		private int matrixSize;

		private float consumeReward;

		private float consumeRewardNegative;

		private float socialReward;

		private float visitedReward;

		private float refuseReward;

		public float totalScore;

        public float totalScoreHalf;

		public List<GameObject> infopointsVisited;

		private float socialEnergyRecharge = 0.05f;

		IGrid mainGrid;

		public GameObject goalInfopoint;

		public bool goalFlag;

        private placeFood generatorScript;

        private DeepBeliefNetwork Network;

        private Vector3 previousStatePosition;

        public double[] previousStatePositionVector;

        public int previousActionIndex;

        public double error;

        BackPropagationLearning teacher;

        private float LearningRate = 0.5f;

        private float Momentum = 0.9f;

        private double[] inputs, outputs;

        bool startMove = false;

        private networkLearn networkLearnScript;


        public int moveCount;

        public string rewardString;

        public string socialStringGood;

        public string socialStringBad;

        public string socialStringGoodZero;

        public string socialStringBadZero;

        public string socialStringDifference;

        public string socialStringDifferenceZero;

        public string scoreExplorationSocialDifferenceZero;

        public string socialStringDifferenceSub;

        public string socialStringDifferenceZeroSub;

        public string scoreExplorationSocialDifferenceZeroSub;

        public string scoreExplorationSocialDifferenceSub;

        public string socialscoreString;

        public string expolorationscoreString;

        public string socialscoreStringZero;

        public string expolorationscoreStringZero;

        public string totalScoreGoodString;

        public string totalScoreBadString;

        private GameObject ground;

        private int agentSpeed = 500;

        private movingFood movingFoodScript;


        // Learning Rate
        private float alpha =.001f;
		// Percent Chance for Random Action
		public float epsilon = 0.2f;
		// Discount Factor
		private float gamma =.9f;

		// Previous Data
		public double prevQVal = 0;

        public bool useNeuralNetworks;

        public float sendReward = 0f;

        private QLearning qlearner;

        private bool actedFlag = false;

        private int latestAngle = 4;

        public float maxSpeed = 0;

        private bool socialFlag = false;


        public enum possibleDispositions { Good , Bad };

		public possibleDispositions disposition;

        private Rigidbody rb;


        public Cell GetNearestWalkableCellFromRelation(IGrid grid, Vector3 position, Vector3 inRelationTo,int maxCellDistance)
		{
			var cell = grid.GetCell(position);
			if (cell == null)
			{
				return null;
			}

			var relationCell = grid.GetCell(inRelationTo);

			if (relationCell == null)
			{
				return null;
			}

			int dist = 1;

			var candidates = new List<Cell>();
			while (candidates.Count == 0 && dist <= maxCellDistance)
			{
				foreach (var c in grid.GetConcentricNeighbours(cell, dist++))
				{
					
						candidates.Add(c);

				}
			}

			Cell winner = null;
			float lowestDist = float.MaxValue;
			for (int i = 0; i < candidates.Count; i++)
			{
				var distSqr = (candidates[i].position - inRelationTo).sqrMagnitude;
				if (distSqr < lowestDist)
				{
					winner = candidates[i];
					lowestDist = distSqr;
				}
			}

			return winner;
		}

		bool TestRange (float numberToCheck, double bottom, double top)
		{
			return (numberToCheck > bottom && numberToCheck <= top);
		}
	

		float SignedAngleBetween(Vector3 a, Vector3 b){

			float angle = Mathf.Atan2(b.z-a.z, b.x-a.x) / Mathf.PI;

			//Debug.Log ("a: " + angle);

			return angle;
		}

        float SignedAngleBetween2(Vector3 a, Vector3 b)
        {

            float rotationAngle = transform.rotation.eulerAngles.y;

            float angle = ((Mathf.Atan2(b.z - a.z, b.x - a.x) / Mathf.PI) + (rotationAngle * Mathf.PI / 180) / Mathf.PI);

            if (angle > 1)
            {

                angle -= 2f;

            }


            //Debug.Log("a: " + angle + " rotation: " + rotationAngle * Mathf.PI / 180 + " - " + Mathf.Atan2(b.z - a.z, b.x - a.x));


            return angle;
        }

        double[] getState(Vector3 position)
		{
			
			double[] featuresVector = new double[vectorSize];

			numberOfAgents = 0;

			numberofGoodInfopoints = 0;

            numberofBadInfopoints = 0;

			double distance = 0f;

			numberOfAgentse = 0;

			numberOfAgentsi = 0;

			numberOfAgentsc = 0;

			numberOfAgentss= 0;


			//Debug.Log ("range: " + range);

			directionsState = new GameObject[numberOfDirections];
        
			Collider[] cols = Physics.OverlapSphere(position, 15f);

            //getRaycast(range);

            //Time.timeScale = 0;

			foreach (var colider in cols) {

				if (colider.tag == "agent") {

					float itemDistance = Vector3.Distance (transform.position, colider.transform.position);

					if (itemDistance > 0.1f) {

						var AgentScript = colider.GetComponent<simulationAgentEncapsulator>();

						if (AgentScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

							numberOfAgentse++;

						} else {

							numberOfAgentsi++;
						}

						if (AgentScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

							numberOfAgentsc++;

						} else {

							numberOfAgentss++;
						}


					}

				} else if (colider.tag == "info") {

                    infopointDynamicsScript = colider.GetComponent<infopointDynamics>();

                    if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                    {

                        numberofGoodInfopoints++;

                    }
                    else
                    {

                        numberofBadInfopoints++;

                    }

				}


				distance = Vector3.Distance (position, colider.transform.position);

				if (distance > 0.2f && distance < range) {


					for (int i = 0; i < directionsMatrix.GetLength (0); i++) {

						if (TestRange (SignedAngleBetween (position, colider.transform.position), directionsMatrix [i, 0], directionsMatrix [i, 1])) {


							//Debug.Log ("f: " + i);


							if (directionsState [i] != null) {



								if (distance < Vector3.Distance (transform.position, directionsState [i].transform.position)) {


									//Debug.Log (distance + " ---- " + Vector3.Distance (position, directionsState [i].transform.position));

									directionsState [i] = colider.transform.gameObject;

								}

							} else {

								directionsState [i] = colider.transform.gameObject;


							}

						}

					}







					//Debug.Log (distance);

					//SignedAngleBetween (transform.position, colider.transform.position);


				}

			}


			int c = 0;

            foreach (var item in directionsState) {

                //Debug.Log (item);

                if (item != null) {

                    float itemDistance = Vector3.Distance(position, item.transform.position);

                    if (item.tag == "agent") {

                        // featuresVector[matrixLength * c + 2] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                        /*

						var AgentScript = item.GetComponent<simulationAgentEncapsulator>();

						if (AgentScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

							featuresVector [matrixLength * c + 2] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						} else {

							featuresVector [matrixLength * c + 3] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						}

						if (AgentScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

							featuresVector [matrixLength * c + 4] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						} else {

							featuresVector [matrixLength * c + 5] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						}

                        */

                    } else if (item.tag == "info") {

                        infopointDynamicsScript = item.GetComponent<infopointDynamics>();

                        if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good) {

                            featuresVector[matrixLength * c] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                        } else {

                            featuresVector[matrixLength * c + 1] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                        }

                    }
                    else if(item.tag == "wall")
                    {

                        featuresVector[matrixLength * c + 4] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

                    }

                    if (item.tag == "agent" || item.tag == "info")
                    {

                        featuresVector[matrixLength * c + 2] = item.GetComponent<Rigidbody>().velocity.x / 10;

                        featuresVector[matrixLength * c + 3] = item.GetComponent<Rigidbody>().velocity.z / 10;

                    }


                }


				c++;
			}
				
			var divideNormalize = 10;


            /*
			featuresVector [matrixSize] = (float) numberOfAgentse / divideNormalize;

			featuresVector [matrixSize + 1] = (float) numberOfAgentsi / divideNormalize;

			featuresVector [matrixSize + 2] = (float) numberOfAgentse / divideNormalize;

			featuresVector [matrixSize + 3] = (float) numberOfAgentsi / divideNormalize;




			featuresVector [matrixSize + 4] = (float) numberofGoodInfopoints / divideNormalize;


			featuresVector [matrixSize + 5] = (float) numberofBadInfopoints / divideNormalize;

            */


			/*

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

				featuresVector [matrixSize + 3] = 1f;

			} else {

				featuresVector [matrixSize + 3] = 0.1f;

			}

			if (agentEncapsulatorScript.currentMood == simulationAgentEncapsulator.agentsMood.Positive) {

				featuresVector [matrixSize + 4] = 1f;

			} else if (agentEncapsulatorScript.currentMood == simulationAgentEncapsulator.agentsMood.Neutral) {


				featuresVector [matrixSize + 4] = 0.5f;

			} else {


				featuresVector [matrixSize + 4] = 0.1f;

			}

	*/
	


			//Debug.Log (q);

            if(!useNeuralNetworks)
			featuresVector [vectorSize-1] = 1f;  //bias


            //List<double> featuresFront = new List<double>();

            //featuresFront = featuresVector.ToList().GetRange(4, 24);

            //printArrayDouble(featuresFront.ToArray());

            return featuresVector;

            //return featuresFront.ToArray();

		}

        /*
		private void OnDrawGizmos() {
			if (currentActionStatus == agentsActionStatus.Wandering)
				Gizmos.color = Color.green;
			else if (currentActionStatus == agentsActionStatus.GettingInfo)
				Gizmos.color = Color.blue;
			else if (currentActionStatus == agentsActionStatus.Communicating)
				Gizmos.color = Color.red;
			else if (goalFlag)
				Gizmos.color = Color.blue;
			
			Gizmos.DrawWireSphere (_unit.transform.position , interactRange);

            
		}

        */

        private void Start()
        {

            qlearner = new QLearning();

            ground = GameObject.Find("ExampleGround");

            movingFoodScript = ground.GetComponent<movingFood>();

            //generatorScript = camera.GetComponent<placeFood>();

            networkLearnScript = ground.GetComponent<networkLearn>();

            useNeuralNetworks = true;

            agentEncapsulatorScript = GetComponent<simulationAgentEncapsulator>();

			minimumDistance = 60.0f;

			rayAngle = 12;


			fieldOFView = 25f;

			goalFlag = false;

            /*
			Renderer rend = GetComponent<Renderer>();

			if(disposition == possibleDispositions.Good)
			rend.material.color = Color.HSVToRGB (0.8f, 1, 0.8f);
        	else
        	rend.material.color = Color.HSVToRGB(0.95f, 1, 0.8f);

            */


			mainGrid = GridManager.instance.GetGrid (new Vector3(0,0,0));


			//state function

			rb = GetComponent<Rigidbody>();

            totalScoreHalf = 0f;



			range = 25;

			interactRange = 5;

			consumeReward = 1f;

			socialReward = 0.99f;

			visitedReward = (float) - consumeReward / 3;

			refuseReward = (float) - socialReward / 3;

			consumeRewardNegative =  - consumeReward;


			infopointsVisited = new List<GameObject>();
		

			numberOfDirections = 16;

			directionsState = new GameObject[numberOfDirections];

			directionsVector = new double[numberOfDirections + 2];

			directionsMatrix = new double[numberOfDirections,2];

			step = (float) 1 / ( numberOfDirections / 2 ) ;

			//Debug.Log ("step: " + step);



			/* rows of matrix - attributes that are perceived for each direction
			 * 
			 * 0 - distance of the detected object - 0-1
			 * 
			 * 1 - type of the detected object 1 - agent , 0 - info
			 * 
			 * 2 - extraverted agent - 1, infopoint - 0
			 * 
			 * 3 - introverted agent - 1 , infopoint - 0
			 * 
			 * 
			 * 0 - good infopoints close  - 0 - 1
			 * 
			 * 1 - bad infopoints close - 0 -1
			 * 
			 * 2 - extraverted agents close - 0 - 1
			 * 
			 * 3 - intraverted agents close - 0 - 1
			 * 
			 * 4 - cooperative agents close - 0 - 1
			 * 
			 * 5 - suspicious agents close - 0 - 1
			 * 
			 */
		
			stateMatrixFeatures = new double[4];



			/* additional features
			 * 
			 * 0 - total number of extraverted agents detected
			 * 
			 * 1 - total number of intraverted agents detected
			 * 
			 * 2 - total number of cooperative agents detected
			 * 
			 * 3 - total number of suspicious agents detected
			 * 
			 * 4 - total number of good infopoints detected
			 * 
			 * 5 - total number of bad infopoints detected
			 * 
			 * 6 - detecting agents personality type - 0 - introverted, 1 -extroverted
			 * 
			 * 7 - detecting agent mood, positive 1, neutral 0.5, negative 0
			 * 
			 */


			onedimensionalFeatures = new double[0];


			vectorSize = numberOfDirections * stateMatrixFeatures.Length + onedimensionalFeatures.Length + 1;  // + bias

            if(useNeuralNetworks)
            {

                vectorSize--; //no need for bias


                //init neural net


                Network = new DeepBeliefNetwork(new BernoulliFunction(), vectorSize , 50, 8);

                new GaussianWeights(Network).Randomize();
                Network.UpdateVisibleWeights();

                teacher = new BackPropagationLearning(Network)
                {
                    LearningRate = LearningRate,
                    Momentum = Momentum
                };

            }

			previousfeaturesVector = new double[40];

            previousStatePositionVector = new double[40];

            weightsVector = new double[40];

			matrixLength = stateMatrixFeatures.Length;

			matrixSize = numberOfDirections * matrixLength;


			int counter;

			for (counter = 0; counter < weightsVector.Length; counter++) {

				weightsVector [counter] = 0f;

			}



			//Debug.Log (TestRange (-0.5f, -0.5f, -0.25f ));


			for (int i = 0; i < numberOfDirections + 2; i++) {



				if (i <= numberOfDirections / 2) {

					//Debug.Log ("number " + i * step);

					directionsVector [i] = i * step;

				} else {

					//Debug.Log ("number2 " + (i * step - ( 2 + step )));

					directionsVector [i] = i * step - (2 + step);

				}


				//Debug.Log (i);

			}



			for (int i = 0; i < directionsMatrix.GetLength(0); i++)
			{

				if (i < numberOfDirections / 2) {
					directionsMatrix [i, 0] = directionsVector [i];

					directionsMatrix [i, 1] = directionsVector [i + 1];
				} else {

					directionsMatrix [i, 0] = directionsVector [i + 1];

					directionsMatrix [i, 1] = directionsVector [i + 2];

				}


				for (int j = 0; j < directionsMatrix.GetLength(1); j++)
				{
					double s = directionsMatrix[i, j];

					//Debug.Log (i + "-"+ j + " " + s);
				}
			}


			foreach (float item in directionsVector) {

				//Debug.Log (item);

			}


            InvokeRepeating("nextActionDelay", 2f, 0.3f);


        }


        /// <summary>
        /// Called on Start and OnEnable, but only one of the two, i.e. at startup it is only called once.
        /// </summary>
        protected override void OnStartAndEnable()
        {

            GameServices.messageBus.Subscribe(this);
            //_startPos = _unit.position;


            previousStatePosition = transform.position; //get the position

            previousStatePositionVector = getState(transform.position); //get the state for the previous position

            //MoveNext(false);

			currentActionStatus = agentsActionStatus.Wandering; //Inital state is wander
        }

        private void OnDisable()
        {
            GameServices.messageBus.Unsubscribe(this);
        }

        void IHandleMessage<UnitNavigationEventMessage>.Handle(UnitNavigationEventMessage message)
        {
            if (message.entity != this.gameObject || message.isHandled)
            {
                return;
            }

			if (message.eventCode == UnitNavigationEventMessage.Event.DestinationReached || message.eventCode == UnitNavigationEventMessage.Event.StoppedNoRouteExists || message.eventCode == UnitNavigationEventMessage.Event.StoppedDestinationBlocked) {
				message.isHandled = true;

                //Debug.Log("destination reached" + message.eventCode.ToString());

                //Debug.Log("Moved from: " + previousStatePosition + " to " + transform.position);


                /*
                previousStatePosition = transform.position;

                previousStatePositionVector = getState(transform.position); //get the state for the previous position

                MoveNext (true);

                */

                networkLearnScript.sendReward(sendReward,gameObject.name.Substring(5));

                moveCount++;

                if(moveCount%200 == 0)
                {

                    rewardString += totalScore + ",";

                } 

              
			}
            else
            {

                networkLearnScript.sendReward(sendReward, gameObject.name.Substring(5));

            }

  


        }

        public double[] getStateVector()
        {
            //printArrayDouble(getState(transform.position));

            //return getState(transform.position);

            //printArrayDouble(getRaycast(10, 5));

            return getRaycast(14, 3);

        }


        int calculateAngle(Vector3 target)
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
        if(totalAngle < -45 && totalAngle >= -135)
        {

            action = 1;
        }
        if(totalAngle >= -45 && totalAngle < 45)
        {

            action = 2;
        }
        if( (totalAngle >= 135 && totalAngle <= 180 ) || ( totalAngle < -135 && totalAngle >= -180 ) )
        {

            action = 3;
        }

        //Debug.Log(action+" a "+totalAngle);

        return action;


        }


        public void doAction(int action)
        {

            //sendReward = 0f;


            if(action == 0)
            {
                rb.AddForce(new Vector3(agentSpeed,0,0));
            }
            else if(action == 1)
            {

                rb.AddForce(new Vector3(-agentSpeed, 0, 0));

            }
            else if (action == 2)
            {

                rb.AddForce(new Vector3(0, 0, agentSpeed));

            }
            else if (action == 3)
            {

                rb.AddForce(new Vector3(0, 0, -agentSpeed));

            }

            //Debug.Log("moving to " + action);


            //StartCoroutine(nextActionDelay());


        }


        private void doActionSlow(int action)
        {

            //sendReward = 0f;


            if(action == 0)
            {
                rb.AddForce(new Vector3(agentSpeed/4,0,0));
            }
            else if(action == 1)
            {

                rb.AddForce(new Vector3(-agentSpeed/4, 0, 0));

            }
            else if (action == 2)
            {

                rb.AddForce(new Vector3(0, 0, agentSpeed/4));

            }
            else if (action == 3)
            {

                rb.AddForce(new Vector3(0, 0, -agentSpeed/4));

            }

            //Debug.Log("moving to " + action);


            //StartCoroutine(nextActionDelay());


        }



        private void MoveNext(bool destinationReached)
        {


            

            var unitMask = _unit.attributes;

            Vector3 pos = Vector3.zero;
            bool pointFound = false;

			pos = transform.position;

			var grid = GridManager.instance.GetGrid(pos);
            if (grid != null)
            {
                var cell = grid.GetCell(pos, true);
                pointFound = cell.isWalkable(unitMask);

                //Debug.Log(cell.matrixPosX + "-" + cell.matrixPosZ );

                List<IGridCell> futureActions = new List<IGridCell>();

                // Up - down - left - right - up left - up right - down left - down riht

                futureActions.Add(cell.GetNeighbour(0, 1));

                futureActions.Add(cell.GetNeighbour(0, -1));

                futureActions.Add(cell.GetNeighbour(-1, 0));

                futureActions.Add(cell.GetNeighbour(1, 0));
                
                

                futureActions.Add(cell.GetNeighbour(-1, 1));

                futureActions.Add(cell.GetNeighbour(1, 1));

                futureActions.Add(cell.GetNeighbour(-1, -1));

                futureActions.Add(cell.GetNeighbour(1, -1));

                


                //futureActions.RemoveAll(item => item == null);


                if (destinationReached)
                {
                    if (goalFlag)
                    {

                        objectsClose = FindObjectsInsideFieldOfView(interactRange).ToArray();

                        if (objectsClose.Contains(goalInfopoint))
                        {

                            goalFlag = false;

                            Debug.LogFormat("<color=cyan>{0} reached {1}</color>", gameObject.name, goalInfopoint);

                            getExplorationReward(goalInfopoint);

                        }

                    }
                    else
                    {
                        updateWeights(futureActions);
                    }

                }

				/*

				string row = "";

				foreach (var item in weightsVector) {

					row += "[" + item.ToString() + "]";
				}
				
				Debug.Log (row);

				*/

				//Debug.Log(leftCell.matrixPosX + " down " + leftCell.matrixPosZ );

				/*

				float[] array = getState (transform.position);

				float[] array2 = getState (futureActions[0].position);

				string row = "";

				foreach (var item in array) {
					
					row += "[" + item.ToString() + "]";

				}



				string weights = "";


				foreach (var item in weightsVector) {

					weights += "[" + item.ToString() + "]";

				}

				//Debug.Log (weights);




				Debug.Log (" q value " + getQvalue (array));

				Debug.Log (" q value " + getQvalue (array2));
*/

				//Debug.Log (getState (numberOfDirections, range, transform.position));

				//SignedAngleBetween (agent.transform.position, detect.transform.position);

				foreach (var item in directionsState) {

						//if(item != null)
						//Debug.Log (item.transform.tag);
				}


			 	// Debug.Log ("agents: " + numberOfAgents + " infopoints: " + numberOfInfopoints);




				if (Random.Range(0f,1f) < epsilon)
				{
                    takeRandomAction(futureActions); //random
				}
				else
				{
					takeBestAction(futureActions);
				}



				//Debug.Log ("random " + (possibleActions)Random.Range (0, 4));


				//Debug.Log ((float)Random.Range (0f, 1f));



			}



        }

		private GameObject FindClosestItemWithTag(GameObject[] gos, string tag) 
		{
			GameObject closest = null;                                 
			float distance= Mathf.Infinity;
			float curDistance = 0;
			
			foreach(GameObject go in gos)                         
			{ 

				if(go.tag == tag)
				{
				
					curDistance = Vector3.Distance(go.transform.position,transform.position);
				

						if (curDistance < distance) {

                        if (curDistance < 0.6f)
                        {
                            closest = go;
                            distance = curDistance;
                            //Debug.LogFormat("found {0} at: {1}",tag,distance);
                        }

					}

				}
			}

			return closest;    
		}

		private GameObject FindRandomItemWithTag(GameObject[] gos, string tag) 
		{
			List<GameObject> selectedObjects = new List<GameObject> ();

			foreach(GameObject go in gos)                         
			{ 

				if(go.tag == tag)
				{

					selectedObjects.Add(go);

				}
			}

			if (selectedObjects.Count == 0)
				return null;

			//Debug.Log (selectedObjects.Count + " " + Random.Range (0, selectedObjects.Count));

			return selectedObjects[Random.Range (0, selectedObjects.Count)];
		}


		private GameObject FindClosestItem(List<GameObject> gos)  
		{
			GameObject closest = null;                                 
			float distance= Mathf.Infinity;
			float curDistance = 0;

			foreach(GameObject go in gos)                         
			{ 

					curDistance = Vector3.Distance(go.transform.position,transform.position);

					if (curDistance < distance)           
					{ 
						closest = go;                     
						distance = curDistance;
					} 

			}

			return closest;    
		}

		List<GameObject> FindObjectsInsideFieldOfView ( int actionRange  ){

			Vector3 position= transform.position;

			Collider[] cols = Physics.OverlapSphere(position, actionRange);
			int q= cols.Length; // q = how many colliders were found

			//Debug.Log ("q l" + q);
			int lastCol = 0;
			int i;

			List<GameObject> gos = new List<GameObject>();
			// pack the objects inside range in the beginning of array cols:
			for (i= 0; i < q; i++){
				float dist= Vector3.Distance(position, cols[i].transform.position);

				if (dist > 0.2f && dist < actionRange) {
					gos.Add (cols [i].gameObject);
				}

				}

			return gos; // return the GameObject array
		}


		public void infopointRequest(GameObject infopoint)
		{

			goalInfopoint = infopoint;

			goalFlag = true;

			Debug.LogFormat ("<color=magenta>{0} received {1} as goal</color>",gameObject.name,infopoint.name);

		}

		public void badinfopointsRequest(List<GameObject> badInfopoints)
		{

			Debug.Log (infopointsVisited.Count + " - " + badInfopoints.Count);

			infopointsVisited = infopointsVisited.Union (badInfopoints).ToList();

			Debug.LogFormat ("<color=magenta>{0} received {1} of bad infopoints and now has {2} visited.</color>",gameObject.name,badInfopoints.Count,infopointsVisited.Count);

		}
	

		public string communicationRequest(GameObject requestingAgent) //communication request received, decide and do response (mood/social energy level)

		{

            return "accept";

			if (goalFlag) {

				Debug.LogFormat ("<color=magenta>{0} refused because it has a goal</color>",gameObject.name);

				return "refuse";

			}


			if (infopointsVisited.Count == 0) {

				Debug.LogFormat ("<color=red>{0} refused because it has not visited any infopoints</color>",gameObject.name);

				return "refuse";

			}


			AgentBehaviourScriptCommunicating = requestingAgent.GetComponent<AgentBehaviourRemote>();

			//Debug.LogFormat ("{0} sent a request to {1}",requestingAgent.name,gameObject.name);

			if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyTreshold) {

				//start send

				if (agentEncapsulatorScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Suspicious) {

					if (trySendSuspicuios(false)) { //agent have info to send and its suspicious, check if other agent has info

						if (tryReceive ()) {

							trySendSuspicuios (true); //send

							return "accept";


						} else {

							return "refuse";

						}


					} else {

						//agent doesn't have info to send, check if the other agent is cooperative and receive if he has info

						simulationAgentEncapsulator requestingScript = requestingAgent.GetComponent<simulationAgentEncapsulator> ();

						if (requestingScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

							//receive only good

							Debug.LogFormat ("<color=yellow> {0} has nothing to offer to  {1} but {1} is cooperative</color>", gameObject.name, requestingAgent.name);

							if (tryReceiveSuspicious ()) {

								return "accept";

							} else {

								return "refuse";

							}


						} else {



							Debug.LogFormat ("<color=yellow>{0} doesn't have anything to offer to {1} and {1} is not cooaperative. Communication is not made.</color>", gameObject.name, requestingAgent.name);

							return "refuse";

						}


					}


				} else { //agent is cooperative

					if (trySend(true)) { //send info regardless


						tryReceive ();

						return "accept";


					} else {

						//agent doesn't have info to send, check if the other agent is cooperative and receive if he has info

						Debug.LogFormat ("<color=yellow>Cooperative {0} had visited infopoints but {1} has already visited them</color>", gameObject.name, requestingAgent.name);

						simulationAgentEncapsulator requestingScript = requestingAgent.GetComponent<simulationAgentEncapsulator> ();

						if (requestingScript.assignedPersonalitySecond == simulationAgentEncapsulator.personalityTypeSecond.Cooperative) {

							//receive only good

							if (tryReceiveSuspicious ()) {

								return "accept";

							} else {

								return "refuse";

							}


						} else {

							Debug.LogFormat ("<color=yellow>{0} doesn't have anything to offer to {1} and {1} is not cooaperative. Communication is not made.</color>", gameObject.name, requestingAgent.name);

                            return "refuse";

                        }


					}



				}




			} //end social check
			else
			{

				Debug.LogFormat ("<color=red>{0} refused because of low social energy</color>",gameObject.name);

				return "refuse";

			}

			return "refuse";

		} //end communication request



		bool tryReceive()
		{
		//receive possible

		List<GameObject> possibleInfopointsReceive = AgentBehaviourScriptCommunicating.infopointsVisited.Except (infopointsVisited).ToList ();


		List<GameObject> goodInfoReceive = new List<GameObject> ();

		List<GameObject> badInfoReceive = new List<GameObject> ();

		foreach (var item in possibleInfopointsReceive) {

			infopointDynamicsScript = item.GetComponent<infopointDynamics> ();

			if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good) {

				goodInfoReceive.Add (item);

			} else {

				badInfoReceive.Add (item);

			}

		}


			if (goodInfoReceive.Count + badInfoReceive.Count > 0) { //agent have info to send

				if (goodInfoReceive.Count > 0) {

					GameObject closestGoodInfopoint = FindClosestItem (goodInfoReceive);

					infopointRequest (closestGoodInfopoint);

					Debug.LogFormat ("<color=green>Received good {0}</color>", closestGoodInfopoint.name);

				}

				if (badInfoReceive.Count > 0) {

					AgentBehaviourScriptCommunicating.badinfopointsRequest (badInfoReceive);

					Debug.LogFormat ("<color=green>Received {0} bad info</color>", badInfoReceive.Count);

				}

				return true;

			} else {

				return false;
			}

		}


		bool tryReceiveSuspicious()
		{
			//receive possible

			List<GameObject> possibleInfopointsReceive = AgentBehaviourScriptCommunicating.infopointsVisited.Except (infopointsVisited).ToList ();


			List<GameObject> goodInfoReceive = new List<GameObject> ();

			foreach (var item in possibleInfopointsReceive) {

				infopointDynamicsScript = item.GetComponent<infopointDynamics> ();

				if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good) {

					goodInfoReceive.Add (item);

				}

			}


			if (goodInfoReceive.Count > 0) { //agent have info to send

					GameObject closestGoodInfopoint = FindClosestItem (goodInfoReceive);

					infopointRequest (closestGoodInfopoint);

					Debug.LogFormat ("<color=green>Received good {0}</color>", closestGoodInfopoint.name);

				return true;

			} else {

				return false;
			}

		}


		bool trySend(bool action) {


			List<GameObject> possibleInfopoints = infopointsVisited.Except (AgentBehaviourScriptCommunicating.infopointsVisited).ToList();


			List<GameObject> goodInfoSend = new List<GameObject> ();

			List<GameObject> badInfoSend = new List<GameObject> ();

			foreach (var item in possibleInfopoints) {

				infopointDynamicsScript = item.GetComponent<infopointDynamics> ();

				if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good) {

					goodInfoSend.Add (item);

				} else {

					badInfoSend.Add (item);

				}

			}

			//start send

			if (goodInfoSend.Count + badInfoSend.Count > 0) { //agent have info to send

				if (goodInfoSend.Count > 0) {

					if (action) {

						GameObject closestGoodInfopoint = FindClosestItem (goodInfoSend);

						AgentBehaviourScriptCommunicating.infopointRequest (closestGoodInfopoint);

						Debug.LogFormat ("<color=green>Sent good {0}</color>", closestGoodInfopoint.name);

					}

				}

				if (badInfoSend.Count > 0) {

					if (action) {

						AgentBehaviourScriptCommunicating.badinfopointsRequest (badInfoSend);

						Debug.LogFormat ("<color=green>Sent {0} bad info</color>", badInfoSend.Count);

					}

				}

				return true;

			}
			else
			{

				return false;

			}

		}


		bool trySendSuspicuios(bool action) {


			List<GameObject> possibleInfopoints = infopointsVisited.Except (AgentBehaviourScriptCommunicating.infopointsVisited).ToList();


			List<GameObject> goodInfoSend = new List<GameObject> ();

			foreach (var item in possibleInfopoints) {

				infopointDynamicsScript = item.GetComponent<infopointDynamics> ();

				if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good) {

					goodInfoSend.Add (item);

				}

			}

			//start send

			if (goodInfoSend.Count > 0) { //agent have info to send

				if (action) {
				
					GameObject closestGoodInfopoint = FindClosestItem (goodInfoSend);

					AgentBehaviourScriptCommunicating.infopointRequest (closestGoodInfopoint);

					Debug.LogFormat ("<color=green>Sent good with no bad {0}</color>", closestGoodInfopoint.name);

				}

				return true;

			}
			else
			{

				return false;

			}

		}



		private string communicateWithAgent(GameObject targetAgent)

		{

			string responseMessage = "";

			AgentBehaviourScript = targetAgent.GetComponent<AgentBehaviourRemote>();

			currentActionStatus = agentsActionStatus.Communicating;

			responseMessage = AgentBehaviourScript.communicationRequest (gameObject);

			//targetAgent.transform.localScale = new Vector3 (5,5,5);

			//_unit.transform.localScale = new Vector3 (5,5,5);

			return responseMessage;

		}



        public double[] getRaycast(int maxDistance, int variables)
        {

            int numberOfRays = (int)360 / rayAngle;

            GameObject[] arrayOfHits;

            double[] arrayOfDistance = new double[numberOfRays];

            arrayOfHits = new GameObject[numberOfRays];

            double[] stateSpace = new double[numberOfRays * variables +2];

            int numberMatrix = numberOfRays * variables;

            //Debug.Log(stateSpace.Length);

            for (int i = 0; i < stateSpace.Length; i++)
            {

                stateSpace[i] = 0f;

            }


            /*
       
            for (int i = 0; i < numberOfRays; i++)
            {

                stateSpace[i * variables] = 1f;
                stateSpace[i * variables + 1] = 1f;
                stateSpace[i * variables + 2] = 1f;

            }

            */


            RaycastHit hit;

            int index = 0;

            while (index < numberOfRays)
            {

                //cast a ray in the current direction.
                if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance))
                {
                    Debug.DrawLine(transform.position, hit.point, Color.cyan);
                    //Distance To the wall
                    //distanceToWalls [index] = Vector3.Distance (transform.position, hit.point);

                    arrayOfHits[index] = hit.transform.gameObject;

                    arrayOfDistance[index] = hit.distance;

                }
                else
                {

                    arrayOfHits[index] = null;
                }

                transform.Rotate(0, rayAngle, 0);

                index += 1;

            }


            int count = 0;

            int hits = 0;


            foreach (var item in arrayOfHits)
            {

                if (item != null)
                {
                    hits++;

                    //double itemDistance = Vector3.Distance(transform.position, item.transform.position);

                    double itemDistance = arrayOfDistance[count];

                    if (item.tag == "info" || item.tag == "agent")
                    {

                        if (item.tag == "info")
                        {
                            infopointDynamicsScript = item.GetComponent<infopointDynamics>();

                            if (infopointDynamicsScript.disposition == infopointDynamics.possibleDispositions.Good)
                            {

                                stateSpace[variables * count] = 1f - (itemDistance / maxDistance);

                            }
                            else
                            {

                                stateSpace[variables * count + 1] =  1f - (itemDistance / maxDistance);

                            }

                        }

                        stateSpace[variables * count + 3] = item.GetComponent<Rigidbody>().velocity.x / 10;

                        stateSpace[variables * count + 4] = item.GetComponent<Rigidbody>().velocity.z / 10;

                    }
                    else if (item.tag == "wall")
                    {
                        stateSpace[variables * count + 2] = 1f - (itemDistance / maxDistance);

                    }


                    // Debug.Log ("----------   " +item.transform.tag + " " + count + " " + distance);
                }

                count++;
            }


            //Debug.Log ("next");


            //printArrayDouble(stateSpace);

            stateSpace[numberMatrix] = gameObject.GetComponent<Rigidbody>().velocity.x / 5;

            stateSpace[numberMatrix + 1] = gameObject.GetComponent<Rigidbody>().velocity.z / 5;

            //Debug.Log("hits " + hits);


            return stateSpace;

        }

        private void printArray(float[] array)

		{

		string row = "";

		foreach (var item in array) {

				row += "[" + System.Math.Round((decimal)item,2).ToString() + "]";

		}

		
		
			Debug.Log (row);

		}

		private void printArrayDouble(double[] array)

		{

			string row = "";

			foreach (var item in array) {

				row += "[" + System.Math.Round((decimal)item,2).ToString() + "]";

			}



			Debug.Log (row);

		}



		private double getQvalue( double[] featuresVector ) {


			double Qvalue = 0;

			int c = 0;

			foreach (var item in featuresVector) {


				Qvalue += (double) (item * weightsVector [c]);

				c++;

			}

			return Qvalue;

		}

        private double[] getQvaluesNeuralNetwork(double[] featuresVector)
        {

            return Network.Compute(featuresVector);

        }


        private void takeRandomAction(List<IGridCell> futureStates)
		{

            List<int> futureIndexes = new List<int>();

            for (int i = 0; i < futureStates.Count; i++)
            {
                if (futureStates[i] != null)
                    futureIndexes.Add(i);
            }

			int randomAction = futureIndexes[Random.Range (0, futureIndexes.Count)]; //select random action

            /*
			double[] featureValues = getState (futureStates [randomAction].position);

			double Qvalue = getQvalue(featureValues);

			previousfeaturesVector = featureValues;

			prevQVal = Qvalue;

            */

            previousActionIndex = randomAction;

			_unit.MoveTo(futureStates[randomAction].position, false);

		}


		private void takeBestAction(List<IGridCell> futureStates)
		{

			double [] allFutureValues = new double[futureStates.Count];

			double bestQvalue = -double.MaxValue;

			int bestQvalueIndex = 0;

            if(useNeuralNetworks)
            {

                allFutureValues = getQvaluesNeuralNetwork(getState(transform.position));

            }

			for (int i = 0; i < allFutureValues.Length; i++) {

                if (futureStates[i] != null)
                {
                    if (allFutureValues[i] > bestQvalue)
                    {

                        bestQvalue = allFutureValues[i];

                        bestQvalueIndex = i;

                    }
                }

			}

            //printArrayDouble(allFutureValues);

            //Debug.Log("max value: " + bestQvalue + " " + bestQvalueIndex + " action taken:" + previousActionIndex);

		/*	string row = "";

			foreach (var item in allFutureValues) {

				row += "[" + item.ToString() + "]";

			}

		*/

			//Debug.Log (row);

			//Debug.Log ("value: " + bestQvalue + " index: " + bestQvalueIndex);


			//previousfeaturesVector = bestStateVector;

			prevQVal = bestQvalue;

            previousActionIndex = bestQvalueIndex;

			if (!goalFlag) {
				
				_unit.MoveTo (futureStates [bestQvalueIndex].position, false);

			} else {

				_unit.MoveTo (GetNearestWalkableCellFromRelation (mainGrid, transform.position, goalInfopoint.transform.position, 10000).position, false);

			}

		
		}


		void normalize(ref double[] v)
		{

			double valueMin = double.MaxValue;

			double valueMax = double.MinValue;


			double scaleMin = -1;
			double scaleMax = 1;


			for (int i = 0; i < v.Length; i++) {

				if (v [i] < valueMin) {

					valueMin = v [i];

				}

				if (v [i] > valueMax) {

					valueMax = v [i];

				}
				

			}

			double valueRange = valueMax - valueMin;
			double scaleRange = scaleMax - scaleMin;
				
			if (valueRange != 0) {

				for (int i = 0; i < v.Length; i++) {


					v [i] = ((scaleRange * (v [i] - valueMin)) / valueRange) + scaleMin;


				}


				//Debug.Log ("max:" + valueMax + " min: " + valueMin);

				//Debug.Log ("value:" + valueRange + " scale: " + scaleRange);

			}
			

		}




		private void updateWeights(List<IGridCell> futureStates)
		{
			
			double maxQVal = -float.MaxValue;

			double[] futureQValues = new double[futureStates.Count];

			float reward = getReward ();

            Debug.Log("reward: " + reward);

            if (useNeuralNetworks)
            {

                //Forward st+1 to evaluate the(fixed) target y = maxafθ(st + 1).This quantity is interpreted to be maxaQ(st + 1).

                futureQValues = getQvaluesNeuralNetwork(getState(transform.position));

                //printArrayDouble(futureQValues);

            }
            else
            {

                for (int i = 0; i < futureQValues.Length; i++)
                {


                    futureQValues[i] = getQvalue(getState(futureStates[i].position));

                }

            }

            


            foreach (var futureQValue in futureQValues)
			{
				double qVal = -double.MaxValue;

				qVal = futureQValue;

				if (qVal > maxQVal)
				{
					maxQVal = qVal;
				}
			}


            //Forward fθ(st) and apply a simple L2 regression loss on the dimension at of the output, to be y.
            //This gradient will have a very simple form where the predicted value is simply subtracted from y.
            //The other output dimensions have zero gradient.

            if (useNeuralNetworks)
            {
                double[] predictionValues = getQvaluesNeuralNetwork(previousStatePositionVector);

                double[] backPropagationValues = new double[predictionValues.Length];

                double predictedQforAction = predictionValues[previousActionIndex]; //agent took at

                double difference = reward + gamma * maxQVal - predictedQforAction;


                for (int i = 0; i < backPropagationValues.Length; i++)
                {

                    if(i==previousActionIndex)
                    {

                        backPropagationValues[i] = difference;

                    }
                    else
                    {

                        backPropagationValues[i] =  0f;
                    }

                }

                //Debug.Log("back propagation:");
                //printArrayDouble(backPropagationValues);

                //Backpropagate the gradient and perform a parameter update.

                //double[][] inputs = new double[1][];

                //double[][] outputs = new double[1][];

                //double[] inputs = new double[previousStatePositionVector.Length];

                //double[] outputs = new double[backPropagationValues.Length];

                inputs = getState(transform.position);

                //inputs = previousStatePositionVector;

                outputs = backPropagationValues;

                //printArrayDouble(inputs);
                //printArrayDouble(outputs);

                error = teacher.Run(inputs, outputs);

                Network.UpdateVisibleWeights();

                //Debug.Log("error: " + error);



            }
            else
            {

                double diff = reward + gamma * maxQVal - prevQVal;
                //Debug.Log("UPDATE WEIGHTS diff=" + diff + "\tbestQVal=" + maxQVal + "\tprevQ" + prevQVal + "reward: " + reward);

                //Debug.Log ("Old weight:");
                //printArray (weightsVector);

                for (int i = 0; i < weightsVector.Length; i++)
                {
                    weightsVector[i] = weightsVector[i] + alpha * diff * previousfeaturesVector[i];
                }

                //Debug.Log ("updated: " + count);


                //printArrayDouble (weightsVector);

                normalize(ref weightsVector);

                //printArrayDouble (weightsVector);

                //Debug.Log ("New weights:");
                //printArray (weightsVector);

            }

		}



		private float getReward()
		{

			float reward = 0;

			objectsClose = FindObjectsInsideFieldOfView (interactRange).ToArray();

			//Debug.Log ("obj " + objectsClose.Length);


			//objectsClose = objectsClose.Except (infopointsVisited).ToArray();


			if (objectsClose.Length > 0) {   //found objects with tags

				string communicationResponse = "";

				nearestAgent = FindRandomItemWithTag (objectsClose, "agent");

				nearestInfopoint = FindClosestItemWithTag (objectsClose, "info");


				if (nearestInfopoint == null) {       // object of type agent found among detected objects and it's the closest - check if isEmpty

                    if (nearestAgent != null)
                    {

                        if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyTreshold)
                        { //agent is found, if the socialEnergy is high enough, initiate communication

                            //Debug.Log ("Found agent, trying to communicate: " + nearestAgent.tag.ToString ());

                            communicationResponse = communicateWithAgent(nearestAgent);

                            Debug.Log("agents response: " + communicationResponse);

                            if (communicationResponse == "accept")
                            {      //agent accepted the communication, notify the encapsulators of both agents and update their social energy


                                updateSocial();

                                reward = getSocialReward();


                            }
                            else if (communicationResponse == "refuse")
                            {

                                if (nearestInfopoint != null)
                                {  //agent refused communication, but infopoints are found, go to nearestInfopoint

                                    reward = getExplorationReward(nearestInfopoint);

                                    //Debug.Log ("Getting info");

                                }

                                reward += refuseReward;



                            }


                        }

                    }


				} else {



					energyRecharge ();	//no agents are found, recharge energy


					if (nearestInfopoint != null) {  //no agent, but infopoints are found, go to nearestInfopoint

						reward = getExplorationReward (nearestInfopoint);

                        Destroy(nearestInfopoint);

						//Debug.Log ("Getting info");

					}





				}



			} else {

				//energyRecharge ();	//no objects detected reduce/increase energy
			}


			return reward;

		}
					


		void updateSocial()
		{

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Introverted) {

				agentEncapsulatorScript.socialEnergy -= agentEncapsulatorScript.socialEnergyStepHigh;

				if (agentEncapsulatorScript.socialEnergy < -agentEncapsulatorScript.socialEnergyMax)
					agentEncapsulatorScript.socialEnergy = -agentEncapsulatorScript.socialEnergyMax;


			} else {
				
				agentEncapsulatorScript.socialEnergy -= agentEncapsulatorScript.socialEnergyStepLow;

				if (agentEncapsulatorScript.socialEnergy < -agentEncapsulatorScript.socialEnergyMax)
					agentEncapsulatorScript.socialEnergy = -agentEncapsulatorScript.socialEnergyMax;
				
			}


		}

		void updateExploration()
		{

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Introverted) {

				agentEncapsulatorScript.socialEnergy += agentEncapsulatorScript.socialEnergyStepLow;

				if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyMax)
					agentEncapsulatorScript.socialEnergy = agentEncapsulatorScript.socialEnergyMax;


			} else {

				agentEncapsulatorScript.socialEnergy += agentEncapsulatorScript.socialEnergyStepHigh;

				if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyMax)
					agentEncapsulatorScript.socialEnergy = agentEncapsulatorScript.socialEnergyMax;

			}


		}


		void energyRecharge()
		{

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Introverted) {

				if(agentEncapsulatorScript.socialEnergy<agentEncapsulatorScript.socialEnergyMax)
				agentEncapsulatorScript.socialEnergy += socialEnergyRecharge;


			} else {

				agentEncapsulatorScript.socialEnergy -= socialEnergyRecharge;
			}


		}

		float getSocialReward()
		{

			return socialReward;

		}

		float getExplorationReward(GameObject infopoint)
		{

			if (infopointsVisited.Contains (infopoint)) {

				//Debug.LogFormat ("<color=red>infopoint already visited</color>");

				return 0;

			}

			string sucess;

			//infopointsVisited.Add (infopoint); //removed

			infopointDynamicsScript = infopoint.GetComponent<infopointDynamics>();


			sucess = infopointDynamicsScript.eatFood (gameObject.name.Substring(5));


			Debug.LogFormat("<color=blue>{0} trying to eat {1} {2}</color>",gameObject.name,infopoint.name,sucess.ToString());

			updateExploration ();

			if (sucess == "positive") {

				//totalScore += consumeReward;

				return consumeReward;

			} else if (sucess == "negative") {

				//totalScore += consumeRewardNegative;

				return consumeRewardNegative;

			} else {

				return 0;

			}


		}


        void OnTriggerEnter(Collider other)
        {


        	    if(other.gameObject.GetInstanceID() == goalInfopoint.GetInstanceID())
            	{

            		goalFlag = false;

            		if(goalInfopoint.GetComponent<infopointDynamics>().disposition == infopointDynamics.possibleDispositions.Good)
            		{

            		sendReward += 0.99999f;
                    totalScore++;
                    movingFoodScript.goodfoodCount--;
                    socialFlag = true;
            		}
            		else
            		{

            		sendReward += -0.99999f;
                    totalScore--;
                    movingFoodScript.badfoodCount--;
                    socialFlag = true;
                }

                	Destroy(other.gameObject);
                	movingFoodScript.foodCount--;


            		//Debug.Log("goal reached "+sendReward);

            	}
            	else if (other.tag == "info")
            	{

                //Debug.Log("x " + other.GetComponent<Rigidbody>().velocity.x/10 + " z "+other.GetComponent<Rigidbody>().velocity.z/10)

                infopointDynamics infopointScript = other.GetComponent<infopointDynamics>();

                if (infopointScript.disposition == infopointDynamics.possibleDispositions.Good)
                {
                    sendReward += 1f;
                    totalScore++;
                    movingFoodScript.goodfoodCount--;
                    socialFlag = true;
                }
                else
                {

                    sendReward += -1f;
                    totalScore--;
                    movingFoodScript.badfoodCount--;
                    socialFlag = true;

                }
                Destroy(other.gameObject);
                movingFoodScript.foodCount--;
            }
        }

        void wait()
        {

            for (int i = 0; i < 999999999; i++)
            {
                double x = 99.8f / 21.0f / 112 * 221;
            }
            Debug.Log("update  ");
        }

        void Update()
        {
            /*
            if (Time.frameCount % 30 == 0)
            {
                if (!actedFlag)
                {
                    previousfeaturesVector = getRaycast(12, 5);

                    int action = qlearner.act(previousfeaturesVector);

                    doAction(action);

                    actedFlag = true;

                }
                else
                {
                    qlearner.learn(sendReward);

                    sendReward = 0f;

                    actedFlag = false;
                }


            }

    */

/*
            if(rb.velocity.magnitude > maxSpeed)
            {

            maxSpeed = rb.velocity.magnitude;

        	}

*/
        	if(transform.position.y < 0)
        	{

        		transform.position = new Vector3(0,0.95f,0);
        	}
            
        }


        void nextActionDelay()
        {
            //yield return new WaitForSeconds(0.5f);


            if (!actedFlag)
            {

                if (moveCount == 0)
                {

                    movingFoodScript.loading();
                }


                if (goalInfopoint == null)
            	{

            		goalFlag = false;
            	}

            	if(!goalFlag || goalInfopoint == null)
            	{
                previousfeaturesVector = getRaycast(14, 5);

                int action = qlearner.act(previousfeaturesVector);

                doAction(action);

                actedFlag = true;

            	}
            	else
            	{

            	int actionAngle = calculateAngle(goalInfopoint.transform.position);

            	if(actionAngle != latestAngle)
            	{

            		Rigidbody rb;

            		rb = GetComponent<Rigidbody>();

            		rb.velocity = Vector3.zero;

            		rb.angularVelocity = Vector3.zero;


            	}

            	latestAngle = actionAngle;

            	previousfeaturesVector = getRaycast(14, 5);

            	int action = qlearner.act(previousfeaturesVector);

            	if(Vector3.Distance(transform.position,goalInfopoint.transform.position) < 4)
            	{
            	if (Random.Range(0f,1f) < epsilon*2)
            	{

            		doActionSlow(Random.Range (0, 3));

            	}
            	else
            	{

            	doAction(actionAngle);

            	}
            	}
            	else
            	{

            	doAction(actionAngle);

            	}

                actedFlag = true;

                //Debug.Log(Vector3.Distance(transform.position,goalInfopoint.transform.position));

            	}

            }
            else
            {
                qlearner.learn(sendReward,socialFlag);

                socialFlag = false;

                sendReward = 0f;

                actedFlag = false;

                moveCount++;

                if (moveCount % 100 == 0)
                {

                    rewardString += totalScore + ",";

                    //socialStringGood += socialGood + ",";

                    //socialStringBad += socialBad + ",";

                    //socialStringGoodZero += socialGoodZero + ",";

                    //socialStringBadZero += socialBadZero + ",";

                    //totalScoreBadString += totalScoreBad + ",";

                    //totalScoreGoodString += totalScoreGood + ",";


                    //socialStringDifference += System.Math.Round((decimal)( socialGood / socialBad ), 3).ToString() + ",";

                    //socialStringDifferenceZero += System.Math.Round((decimal)( socialGoodZero / socialBadZero ), 3).ToString() + ",";


                    //socialStringDifferenceSub += ( socialGood - socialBad ) + ",";

                    //socialStringDifferenceZeroSub += ( socialGoodZero - socialBadZero ) + ",";

                    //expolorationscoreString += explorationScore + ",";

                    //socialscoreString += socialScore + ",";

                    //expolorationscoreStringZero += explorationScoreZero + ",";

                    //socialscoreStringZero += socialScoreZero + ",";

                    //scoreExplorationSocialDifferenceZero += System.Math.Round((decimal)( explorationScoreZero / socialScoreZero ), 3).ToString() + ",";

                    //scoreExplorationSocialDifferenceZeroSub += ( explorationScoreZero - socialScoreZero ) + ",";

                    //scoreExplorationSocialDifferenceSub += ( explorationScore - socialScore ) + ",";


                }

                if(moveCount == 8000)
                {

                qlearner.epsilon = 0.1f;

                totalScoreHalf = totalScore;

                }

                if(moveCount == 16001)
                {

                if (gameObject.name == "agent0")
                    {

                    
                    movingFoodScript.statistics();

                    //Scene loadedLevel = SceneManager.GetActiveScene();

                    //SceneManager.LoadScene(loadedLevel.buildIndex);

                    }
                }
            }

            //networkLearnScript.sendReward(sendReward, gameObject.name.Substring(5));

        }

        public void loadNet(List<double> weightsArray)
        {

            qlearner.loadNet(weightsArray);
        }

        public List<double> downloadNet()
        {

            return qlearner.downloadNet();
        }

        public void resetNet()
        {

            qlearner = new QLearning();

            Debug.Log("network reset");
        }

        public double getReplayEntropy()
        {

            return qlearner.getReplayEntropy();
        }

        public double getReplayCuriosity()
        {

            return qlearner.getReplayCuriosity();
        }

        public double getFitness()
        {

            return (double)(totalScoreHalf * 0.5f) + totalScore;

        }

    }
}