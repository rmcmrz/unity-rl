using UnityEngine;
using System.Collections;
using UnityEditor;

public class statisticsEditor : Editor {


	[CustomEditor(typeof(statisticsExport))]
	public override void OnInspectorGUI()
	{


		//DrawDefaultInspector();

		statisticsExport stat = (statisticsExport)target;

		if(GUILayout.Button("Statistics"))
		{

			stat.statistics ();
			
		}


	}

}
