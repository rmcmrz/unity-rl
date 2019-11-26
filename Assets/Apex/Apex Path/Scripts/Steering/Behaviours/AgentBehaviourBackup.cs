using System.Linq;


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

    /// <summary>
    /// A steering behaviour that will make the unit to which it is attached, wander around within a certain radius.
    /// </summary>
    [AddComponentMenu("Apex/Navigation/Behaviours/Wander")]
    [ApexComponent("Behaviours")]
    public class AgentBehaviourBackup : ExtendedMonoBehaviour, IHandleMessage<UnitNavigationEventMessage>
    {
        public float fieldOFView = 25f; //field of view

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


		private GameObject[] objectsClose;

		private GameObject nearestAgent;

		private GameObject nearestInfopoint;


		private infopointDynamics infopointDynamicsScript;

		private AgentBehaviour AgentBehaviourScript;

		private AgentBehaviour AgentBehaviourScriptCommunicating;

		private simulationAgentEncapsulator agentEncapsulatorScript;

		public enum agentsActionStatus { Wandering , MovingToCommunicate , MovingToInfo, Communicating, GettingInfo };
		public agentsActionStatus currentActionStatus;

		public enum possibleActions { Up, Down, Left, Right };


		private int rayAngle;



		private int range,interactRange,numberOfDirections;

		private GameObject[] directionsState;

		private float[] directionsVector;

		private float[,] directionsMatrix;

		private float[,] stateMatrix;

		private float[] stateMatrixFeatures;

		private float[] onedimensionalFeatures;

		public float[] previousfeaturesVector;

		public double[] weightsVector;

		private float step;

		private int numberOfAgents;

		private int numberOfInfopoints;

		private int numberOfInfopointsVisited;

		private int vectorSize;

		private int matrixLength;

		private int matrixSize;

		private float consumeReward;

		private float socialReward;

		private float visitedReward;

		private float refuseReward;

		public List<GameObject> infopointsVisited;

		private float socialEnergyRecharge = 0.02f;

		IGrid mainGrid;

		private GameObject goalInfopoint;

		private bool goalFlag;


		// Learning Rate
		private float alpha =.1f;
		// Percent Chance for Random Action
		private float epsilon =.2f;
		// Discount Factor
		private float gamma =.7f;

		// Previous Data
		public float prevQVal = 0;


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

		bool TestRange (float numberToCheck, float bottom, float top)
		{
			return (numberToCheck > bottom && numberToCheck <= top);
		}
	

		float SignedAngleBetween(Vector3 a, Vector3 b){

			float angle = Mathf.Atan2 (b.z - a.z, b.x - a.x) / Mathf.PI;

			//Debug.Log ("a: " + angle);

			return angle;
		}

		float[] getState(Vector3 position, int direction)
		{
			
			float[] featuresVector = new float[vectorSize];

			numberOfAgents = 0;

			numberOfInfopoints = 0;

			numberOfInfopointsVisited = 0;

			float distance = 0f;


			//Debug.Log ("range: " + range);

			directionsState = new GameObject[numberOfDirections];

			Collider[] cols = Physics.OverlapSphere(position, range);

			foreach (var colider in cols) {

				if (colider.tag == "agent") {

					numberOfAgents++;

				} else if (colider.tag == "info") {

					numberOfInfopoints++;

					if (infopointsVisited.Contains (colider.transform.gameObject)) {


						numberOfInfopointsVisited++;

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

			numberOfAgents --;


			int c = 0;

			foreach (var item in directionsState) {

				//Debug.Log (item);

				if (item != null) {

					float itemDistance = Vector3.Distance (position, item.transform.position);

					if (item.tag == "agent") {

						var AgentScript = item.GetComponent<simulationAgentEncapsulator>();

						if (AgentScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Extroverted) {

							featuresVector [matrixLength * c + 2] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						} else {

							featuresVector [matrixLength * c + 3] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						}

					} else if (item.tag == "info") {

						if (infopointsVisited.Contains (item)) {

							featuresVector [matrixLength * c] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						} else {

							featuresVector [matrixLength * c + 1] = 1 - (itemDistance - 0.2f) / (range - 0.2f);

						}

					}
					

				}


				c++;
			}
				

			featuresVector [matrixSize] = numberOfAgents;

			featuresVector [matrixSize+1] = numberOfInfopointsVisited;


			featuresVector [matrixSize+2] = numberOfInfopoints - numberOfInfopointsVisited;

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

			featuresVector [vectorSize-1] = 1f;  //bias

			return featuresVector;

		}


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

        private void Awake()
        {

            this.WarnIfMultipleInstances();

            _unit = this.GetUnitFacade();
            if (_unit == null)
            {
                Debug.LogError("WanderBehaviour requires a component that implements IMovable.");
                this.enabled = false;
            }
				
			agentEncapsulatorScript = GetComponent<simulationAgentEncapsulator>();

			minimumDistance = 60.0f;

			rayAngle = 3;


			fieldOFView = 25f;

			goalFlag = false;



			mainGrid = GridManager.instance.GetGrid (new Vector3(0,0,0));


			//state function



			range = 400;

			interactRange = 20;


			consumeReward = 0.5f;

			socialReward = consumeReward;

			visitedReward = (float) - consumeReward / 3;

			refuseReward = (float) - socialReward / 3;


			infopointsVisited = new List<GameObject>();
		

			numberOfDirections = 8;

			directionsState = new GameObject[numberOfDirections];

			directionsVector = new float[numberOfDirections + 2];

			directionsMatrix = new float[numberOfDirections,2];

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
			 * 0 - visited infopoints close  - 0 - 1
			 * 
			 * 1 - unvisited infopoints close - 0 -1
			 * 
			 * 2 - extraverted agents close - 0 - 1
			 * 
			 * 3 - intraverted agents close - 0 - 1
			 * 
			 */
		
			stateMatrixFeatures = new float[4];



			/* additional features
			 * 
			 * 0 - total number of agents detected
			 * 
			 * 1 - total number of visited infopoints detected
			 * 
			 * 2 - total number of unvisited infopoints detected
			 * 
			 * 3 - detecting agents personality type - 0 - introverted, 1 -extroverted
			 * 
			 * 4 - detecting agent mood, positive 1, neutral 0.5, negative 0
			 * 
			 */


			onedimensionalFeatures = new float[3];


			vectorSize = numberOfDirections * stateMatrixFeatures.Length + onedimensionalFeatures.Length + 1;  // + bias

			previousfeaturesVector = new float[vectorSize];

			weightsVector = new double[vectorSize];

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
					float s = directionsMatrix[i, j];

					//Debug.Log (i + "-"+ j + " " + s);
				}
			}


			foreach (float item in directionsVector) {

				//Debug.Log (item);

			}




        }

        /// <summary>
        /// Called on Start and OnEnable, but only one of the two, i.e. at startup it is only called once.
        /// </summary>
        protected override void OnStartAndEnable()
        {
            GameServices.messageBus.Subscribe(this);
            //_startPos = _unit.position;

            MoveNext(false);
            if (this.lingerForSeconds == 0.0f)
            {
                MoveNext(true);
            }

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

			if (message.eventCode == UnitNavigationEventMessage.Event.WaypointReached) {
				message.isHandled = true;

				Debug.Log ("waypoint------------------");

				MoveNext (false);


			} else if (message.eventCode == UnitNavigationEventMessage.Event.DestinationReached || message.eventCode == UnitNavigationEventMessage.Event.StoppedNoRouteExists) {
				message.isHandled = true;

				MoveNext (false);

			} else {

				MoveNext (false);
			}
        }

		private IEnumerator DelayedMove(agentsActionStatus status)
        {
            yield return new WaitForSeconds(this.lingerForSeconds);
			currentActionStatus = agentsActionStatus.Wandering; // continue to wander...
			Debug.Log("continue to wander...");
            MoveNext(false);
        }

		private IEnumerator gettingInfo()
		{
			yield return new WaitForSeconds(this.aquireInfoDuration);
			currentActionStatus = agentsActionStatus.Wandering; // continue to wander...
			Debug.Log("got info, continue to wander...");
			MoveNext(false);
		}

        private void MoveNext(bool append)
        {
            var unitMask = _unit.attributes;

            Vector3 pos = Vector3.zero;
            bool pointFound = false;
            int attempts = 0;

			pos = transform.position;

			var grid = GridManager.instance.GetGrid(pos);
			if (grid != null)
			{
				var cell = grid.GetCell(pos, true);
				pointFound = cell.isWalkable(unitMask);

				//Debug.Log(cell.matrixPosX + "-" + cell.matrixPosZ );

				List<IGridCell> futureActions = new List<IGridCell>();

				// Up - down - left - right - up left - up right - down left - down riht

				futureActions.Add(cell.GetNeighbour (0, 1));

				futureActions.Add(cell.GetNeighbour (0, -1));

				futureActions.Add(cell.GetNeighbour (-1, 0));

				futureActions.Add(cell.GetNeighbour (1, 0));


				futureActions.Add(cell.GetNeighbour (-1, 1));

				futureActions.Add(cell.GetNeighbour (1, 1));

				futureActions.Add(cell.GetNeighbour (-1, -1));

				futureActions.Add(cell.GetNeighbour (1, -1));


				List<int> futureDirections = new List<int>();

				futureDirections.Add (0);

				futureDirections.Add (180);

				futureDirections.Add (270);

				futureDirections.Add (90);


				futureDirections.Add (315);

				futureDirections.Add (45);

				futureDirections.Add (225);

				futureDirections.Add (135);






				//futureActions.RemoveAll (item => item == null);


				for (int i = futureActions.Count - 1; i >= 0; i--)
				{
					if (futureActions [i] == null) {
						
						futureActions.RemoveAt (i);

						futureDirections.RemoveAt (i);

						//Debug.Log ("count" + futureActions.Count);
					}
				}


				updateWeights (futureActions,futureDirections);

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
					takeRandomAction(futureActions,futureDirections);
				}
				else
				{
					takeBestAction(futureActions,futureDirections);
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
				
				if (curDistance < distance)           
				{ 
					closest = go;                     
					distance = curDistance;         
					//Debug.LogFormat("found {0} at: {1}",tag,distance);
				} 

				}
			}

			return closest;    
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
	

		public string communicationRequest(GameObject requestingAgent) //communication request received, decide and do response (mood/social energy level)

		{


			if (infopointsVisited.Count == 0) {

				Debug.LogFormat ("<color=red>{0} refused because it has not visited any infopoints</color>",gameObject.name);

				return "refuse";

			}


			AgentBehaviourScriptCommunicating = requestingAgent.GetComponent<AgentBehaviour>();

			Debug.LogFormat ("{0} sent a request to {1}",requestingAgent.name,gameObject.name);

			if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyTreshold) {

				List<GameObject> possibleInfopoints = infopointsVisited.Except (AgentBehaviourScriptCommunicating.infopointsVisited).ToList();

				if (possibleInfopoints.Count != 0) {

					GameObject closestInfopoint = FindClosestItem (possibleInfopoints);

					Debug.LogFormat ("<color=green>{0} had visited infopoints that {1} havent visited and its sending the information about {2}</color>",gameObject.name,requestingAgent.name,closestInfopoint.name);

					AgentBehaviourScriptCommunicating.infopointRequest (closestInfopoint);

					currentActionStatus = agentsActionStatus.Communicating;

					updateSocial ();

					return "accept";

				} else {


					Debug.LogFormat ("<color=yellow>{0} had visited infopoints but {1} has already visited them</color>",gameObject.name,requestingAgent.name);

					return "refuse";

				}

			} else {

				Debug.LogFormat ("<color=red>{0} refused because of low social energy</color>",gameObject.name);

				return "refuse";

			}

		}


		private string communicateWithAgent(GameObject targetAgent)

		{

			string responseMessage = "";

			AgentBehaviourScript = targetAgent.GetComponent<AgentBehaviour>();

			currentActionStatus = agentsActionStatus.Communicating;

			responseMessage = AgentBehaviourScript.communicationRequest (gameObject);

			//targetAgent.transform.localScale = new Vector3 (5,5,5);

			//_unit.transform.localScale = new Vector3 (5,5,5);

			return responseMessage;

		}



		public void getRaycast(int maxDistance)
		{

			int numberOfRays = (int)360/rayAngle;

			GameObject[] arrayOfHits;

			arrayOfHits = new GameObject [numberOfRays];

			RaycastHit hit;

			int index = 0;

			while (index < numberOfRays) {

				//cast a ray in the current direction.
				if (Physics.Raycast (transform.position, transform.forward, out hit, maxDistance)) {
					Debug.DrawLine (transform.position, hit.point, Color.cyan);
					//Distance To the wall
					//distanceToWalls [index] = Vector3.Distance (transform.position, hit.point);

					arrayOfHits [index] = hit.transform.gameObject;

				} else {

					arrayOfHits [index] = null;
				}

				transform.Rotate(0,rayAngle,0);

				index += 1;

			}





			foreach (var item in arrayOfHits) {

				if (item!= null) {

					Debug.Log ("----------   " +item.transform.tag);
				}
			}

			Debug.Log ("------");




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



		private float getQvalue( float[] featuresVector ) {


			float Qvalue = 0;

			int c = 0;

			foreach (var item in featuresVector) {


				Qvalue += (float) (item * weightsVector [c]);

				c++;

			}

			return Qvalue;

		}
			

		private void takeRandomAction(List<IGridCell> futureStates,List<int> futureDirections)
		{

			int randomAction = Random.Range (0, futureStates.Count); //select random action

			float[] featureValues = getState (futureStates [randomAction].position,futureDirections[randomAction]);

			float Qvalue = getQvalue(featureValues);

			previousfeaturesVector = featureValues;

			prevQVal = Qvalue;

			_unit.MoveTo(futureStates[randomAction].position, false);

		}


		private void takeBestAction(List<IGridCell> futureStates,List<int> futureDirections)
		{
			float maxQVal = -float.MaxValue;

			float [] allFutureValues = new float[futureStates.Count];

			float [] bestStateVector = new float[vectorSize];

			float [] currentStateVector = new float[vectorSize];

			float bestQvalue = -float.MaxValue;

			int bestQvalueIndex = 0;

			for (int i = 0; i < allFutureValues.Length; i++) {

				currentStateVector = getState (futureStates [i].position,futureDirections[i]);

				allFutureValues [i] = getQvalue (currentStateVector);


				if (allFutureValues [i] > bestQvalue) {

					bestQvalue = allFutureValues [i];

					bestQvalueIndex = i;

					bestStateVector = currentStateVector;

				}

			}

		/*	string row = "";

			foreach (var item in allFutureValues) {

				row += "[" + item.ToString() + "]";

			}

		*/

			//Debug.Log (row);

			//Debug.Log ("value: " + bestQvalue + " index: " + bestQvalueIndex);


			previousfeaturesVector = bestStateVector;

			prevQVal = bestQvalue;

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




		private void updateWeights(List<IGridCell> futureStates,List<int> futureDirections)
		{
			
			float maxQVal = -float.MaxValue;

			float[] futureQValues = new float[futureStates.Count];

			float reward = getReward ();

			for (int i = 0; i < futureQValues.Length; i++) {


				futureQValues [i] = getQvalue (getState (futureStates [i].position,futureDirections[i]));

			}


			foreach (var futureQValue in futureQValues)
			{
				float qVal = -float.MaxValue;

				qVal = futureQValue;

				if (qVal > maxQVal)
				{
					maxQVal = qVal;
				}
			}
			float diff = reward + gamma * maxQVal - prevQVal;
			//Debug.Log("UPDATE WEIGHTS diff=" + diff + "\tbestQVal=" + maxQVal + "\tprevQ" + prevQVal + "reward: " + reward);

			//Debug.Log ("Old weight:");
			//printArray (weightsVector);

			for (int i = 0; i < weightsVector.Length; i++)
			{
					weightsVector [i] = weightsVector [i] + alpha * diff * previousfeaturesVector [i];
			}

			//Debug.Log ("updated: " + count);


			//printArrayDouble (weightsVector);

			normalize (ref weightsVector);

			//printArrayDouble (weightsVector);

			//Debug.Log ("New weights:");
			//printArray (weightsVector);

		}




		private float getReward()
		{


			float reward = 0;

			objectsClose = FindObjectsInsideFieldOfView (interactRange).ToArray();

			//Debug.Log ("obj " + objectsClose.Length);


			if (objectsClose.Length > 0) {   //found objects with tags

				if(goalFlag)
				{
					if (objectsClose.Contains (goalInfopoint)) {

						goalFlag = false;

						Debug.LogFormat("<color=cyan>{0} reached {1}</color>",gameObject.name,goalInfopoint);

					}

				}

				string communicationResponse = "";

				nearestAgent = FindClosestItemWithTag (objectsClose, "agent");

				nearestInfopoint = FindClosestItemWithTag (objectsClose, "info");


				if (nearestAgent != null) {       // object of type agent found among detected objects and it's the closest - check if isEmpty

					if (agentEncapsulatorScript.socialEnergy > agentEncapsulatorScript.socialEnergyTreshold) { //agent is found, if the socialEnergy is high enough, initiate communication

						//Debug.Log ("Found agent, trying to communicate: " + nearestAgent.tag.ToString ());

						communicationResponse = communicateWithAgent (nearestAgent);

						Debug.Log ("agents response: " + communicationResponse);

						if (communicationResponse == "accept") {      //agent accepted the communication, notify the encapsulators of both agents and update their social energy


							updateSocial ();

							reward = getSocialReward ();


						} else if (communicationResponse == "refuse") {

							if (nearestInfopoint != null) {  //agent refused communication, but infopoints are found, go to nearestInfopoint

								reward = getExplorationReward (nearestInfopoint);

								//Debug.Log ("Getting info");

							}

							reward += refuseReward;



						}


					}


				} else {



					energyRecharge ();	//no agents are found, recharge energy


					if (nearestInfopoint != null) {  //no agent, but infopoints are found, go to nearestInfopoint

						reward = getExplorationReward (nearestInfopoint);

						//Debug.Log ("Getting info");
				
					}





				}



			} else {

				energyRecharge ();	//no objects detected reduce/increase energy
			}


			return reward;

		}


		void updateSocial()
		{

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Introverted) {

		//		agentEncapsulatorScript.socialEnergy -= agentEncapsulatorScript.socialEnergyDecrement;


			} else {

		//		agentEncapsulatorScript.socialEnergy += agentEncapsulatorScript.socialEnergyIncrement;
			}


		}


		void energyRecharge()
		{

			if (agentEncapsulatorScript.assignedPersonality == simulationAgentEncapsulator.personalityType.Introverted) {

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

				Debug.LogFormat ("<color=red>infopoint already visited</color>");

				return visitedReward;

			}

			string sucess;

			infopointsVisited.Add (infopoint);

			infopointDynamicsScript = infopoint.GetComponent<infopointDynamics>();


			sucess = infopointDynamicsScript.eatFood (gameObject.name.Substring(5));


			Debug.LogFormat("<color=blue>{0} trying to eat {1} {2}</color>",gameObject.name,infopoint.name,sucess);

			if (sucess == "positive") {

				return consumeReward;

			} else if (sucess == "negative") {

				return -consumeReward;

			} else {

				return 0;

			}

		}


    }
}