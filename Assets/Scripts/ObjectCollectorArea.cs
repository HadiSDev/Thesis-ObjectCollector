
using System;
using System.Collections.Generic;
using System.Linq;
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
    public GameObject m_StationType;
    public int maxSpawnAttemptsPerObstacle = 10;
    
    private IList<GameObject> m_Objectives = new List<GameObject>();
    private IList<GameObject> m_Stations = new List<GameObject>();

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
    
    void CreateStations(int num)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(m_StationType, new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0, Random.Range(0f, 360f), 0)));
            m_Stations.Add(f);
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
    
    public void ResetStations()
    {
        foreach (var obj in m_Stations)
        {
            obj.transform.position = new Vector3(
                Random.Range(-range, range),
                1f,
                Random.Range(-range, range)) + transform.position;
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

    public void ResetObjectiveArea(Agent[] agents)
    {
        var firstStation = m_Stations.FirstOrDefault();
        foreach (var agent in agents)
        {
            agent.gameObject.SetActive(true);
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }
            
            if (firstStation != null)
            {
                firstStation.transform.position = new Vector3(Random.Range(-range, range), 0.5f,
                    Random.Range(-range, range))  + transform.position;
                var offsetX = Random.Range(-2, 2);
                var offsetY = Random.Range(-2, 2);
                agent.transform.position = firstStation.transform.position + new Vector3(offsetX, 1f, offsetY);
            }
            else
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range))  + transform.position;
            }
            
                
            agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
        }

        if (m_Stations.Count > 0)
        {
            ResetStations();
        }
        else
        {
            CreateStations(3);
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
