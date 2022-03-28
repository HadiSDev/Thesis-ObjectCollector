
using System;
using System.Collections.Generic;
using MBaske.Sensors.Grid;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class ObjectCollectorArea : Area
{
    public GameObject objective;
    public int numObjectives;
    public float range;
    public NavMeshObstacle obstacle;
    public int numObstacles;
    public GameObject[] stations;
    public int maxSpawnAttemptsPerObstacle = 10;
    
    private IList<GameObject> m_Objectives = new List<GameObject>();

    void CreateObjectives(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
            f.GetComponent<ObjectLogic>().myArea = this;
            m_Objectives.Add(f);
        }
    }

    public void ResetObjectives()
    {
        foreach (var obj in m_Objectives)
        {
            obj.transform.position = new Vector3(
                Random.Range(-range, range),
                1f,
                Random.Range(-range, range)) + transform.position;
            obj.SetActive(true);
            
        }
    }

    void CreateObstacles()
    {
        for (int i = 0; i < numObstacles; i++)
        {

            bool validPosition = false;
            int spawnAttempts = 0;

            Vector3 position = new Vector3();
            Quaternion rotation = new Quaternion();

            while (!validPosition && spawnAttempts < maxSpawnAttemptsPerObstacle)
            {
                spawnAttempts++;
                obstacle.transform.localScale = new Vector3(1f, Random.Range(0.8f, 10f), Random.Range(0.8f, 10f));
                position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + transform.position;
                rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f));

                validPosition = true;

                Collider[] colliders = Physics.OverlapBox(position, obstacle.transform.localScale / 2f, rotation);

                if (colliders.Length > 0)
                {
                    validPosition = false;
                }
            }
            if (validPosition)
            {
                NavMeshObstacle f = Instantiate(obstacle, position, rotation);
            }
        }
    }

    public void ResetObjectiveArea(GameObject[] agents)
    {
        var firstStation = stations.Length == 0 ? null : stations[0];
        foreach (GameObject agent in agents)
        {
            agent.SetActive(true);
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }
            
            if (firstStation != null)
            {
                firstStation.transform.position = new Vector3(Random.Range(-range, range), 0.5f,
                    Random.Range(-range, range))  + transform.position;
                agent.transform.position = firstStation.transform.position;
            }
            else
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range))  + transform.position;
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
        CreateObstacles();
    }

    public override void ResetArea()
    {
    }
}
