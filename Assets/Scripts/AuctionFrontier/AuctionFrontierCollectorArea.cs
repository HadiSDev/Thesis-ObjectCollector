
using System;
using System.Collections.Generic;
using DefaultNamespace;
using MBaske.Sensors.Grid;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
public class AuctionFrontierCollectorArea : Area
{
    public GameObject objective;
    public int numObjectives;
    public float range;
    public NavMeshObstacle obstacle;
    public int numObstacles;
    public GameObject[] stations;
    public int maxSpawnAttemptsPerObstacle = 10;
    private IList<GameObject> m_Objectives = new List<GameObject>();
    private GridTracking m_GridTracking;


    public void Awake()
    {
        m_GridTracking = FindObjectOfType<GridTracking>();
        stations = GameObject.FindGameObjectsWithTag("station");
    }

    public void SetGridWorldValue(Vector3 worldPosition, int value)
    {
        m_GridTracking.SetValue(worldPosition, 1);
    }

    public int GetGridWorldValue(Vector3 worldPosition)
    {
        return m_GridTracking.GetGridValue(worldPosition);
    }

    public void UpdateGridWorld(GameObject agent)
    {
        var grid = agent.GetComponent<GridSensorComponent3D>();
        var afagent = agent.GetComponent<AuctionFrontierAgent>();
        var num_discovered_cells = m_GridTracking.UpdateGridWithSensor(agent.transform, grid.LonAngle*2, grid.MaxDistance, 1);
        afagent.AddExplorationScore(num_discovered_cells);
    }

    public void ScanGridWorld()
    {
        // DEBUG.log("Hello");
    }

    void CreateObjectives(int num, GameObject type)
    {
        Debug.Log("Creating objectives");
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
            f.GetComponent<AuctionFrontierObjectLogic>().myArea = this;
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
        var HasStatioon = stations.Length > 0 ;
        int idx = 0;
        foreach (GameObject agent in agents)
        {
            agent.SetActive(true);
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }
            
            if (HasStatioon)
            {
                agent.transform.position = stations[idx % stations.Length].transform.position;
            }
            else
            {
                agent.transform.position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range))  + transform.position;
            }

            agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            idx++;
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
