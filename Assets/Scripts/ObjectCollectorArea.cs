using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class ObjectCollectorArea : Area
{
    public GameObject objective;
    public int numObjectives;
    public float rangeX = 147f;
    public float rangeZ = 13.4f;
    private IList<GameObject> m_Objectives = new List<GameObject>();
    
    [HideInInspector]
    public IList<GameObject> m_exploredObjectives = new List<GameObject>();
    
    private GameObject Station { get; set; }
    private void Start()
    {
        Station = GameObject.FindGameObjectWithTag("station");
    }

    void CreateObjectives(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-rangeX, rangeX), 1f,
                Random.Range(-rangeZ, rangeZ)) + transform.position,
                Quaternion.identity);
            f.GetComponent<ObjectLogic>().myArea = this;
            m_Objectives.Add(f);
        }
    }

    public void ResetObjectives()
    {
        foreach (var obj in m_Objectives)
        {
            obj.transform.position = new Vector3(
                Random.Range(-rangeX, rangeX),
                1f,
                Random.Range(-rangeZ, rangeZ)) + transform.position;
            obj.SetActive(true);
            
        }
    }

    public void ResetObjectiveArea(ObjectCollectorAgent[] agents)
    {
        m_exploredObjectives = new List<GameObject>();
        foreach (var agent in agents)
        {
            agent.gameObject.SetActive(true);
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }

            if (Station != null)
            {
                var offsetX = Random.Range(-50, 50);
                var offsetZ = Random.Range(-rangeZ, rangeZ);
                agent.transform.position = new Vector3(offsetX, 2f, offsetZ);
            }
            else
            {
                agent.transform.position = new Vector3(Random.Range(-rangeX, rangeX), 1f,
                    Random.Range(-rangeZ, rangeZ))  + transform.position;
            }
            
            agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        }
        
        if (m_Objectives.Count > 0)
        {
            ResetObjectives();
        }
        else
        {
            CreateObjectives(numObjectives, objective);
        }
    }

    public override void ResetArea()
    {
    }
}
