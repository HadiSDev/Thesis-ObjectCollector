
using System;
using System.Collections.Generic;
using CustomDetectableObjects;
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
    public float rangeX;
    public float rangeZ;

    public NavMeshObstacle obstacle;
    public int numObstacles;
    public GameObject[] stations;
    public int maxSpawnAttemptsPerObstacle = 10;
    private IList<GameObject> m_Objectives = new List<GameObject>();
    private GridTracking m_GridTracking;
    private List<Vector3> SpawnPositions = new List<Vector3>(){ new Vector3(-3f, 1f, -11.5f), new Vector3(3f, 1f, -11.5f), new Vector3(-3f, 1f, -7.5f), new Vector3(3f, 1f, -7.5f), new Vector3(0f, 1f, -4.5f)};


    public void Awake()
    {
        m_GridTracking = FindObjectOfType<GridTracking>();
        stations = GameObject.FindGameObjectsWithTag("station");
    }
    

    public void UpdateGridWorld(GameObject agent)
    {
        var grid = agent.GetComponent<GridSensorComponent3D>();
        var afagent = agent.GetComponent<AuctionFrontierAgent>();
        var num_discovered_cells = m_GridTracking.UpdateGridWithSensor(agent.transform, grid.LonAngle*2, grid.MaxDistance, 1, afagent.checkEvery);
        afagent.AddExplorationScore(num_discovered_cells);
        afagent.DiscoveredCellsUpdate(num_discovered_cells);
    }
    
    void CreateObjectives(int num, GameObject type)
    {
        //Debug.Log("Creating objectives");
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-rangeX, rangeX), 1f,
                Random.Range(-rangeZ, rangeZ)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 0f)));
            f.GetComponent<AuctionFrontierObjectLogic>().myArea = this;
            m_Objectives.Add(f);
        }
    }

    public void ResetObjectives()
    {
        Debug.Log("Reseting objectives...");
        foreach (var obj in m_Objectives)
        {
            var detection = obj.GetComponent<DetectableVisibleObject>();
            detection.isDetected = false;
            detection.isNotDetected = true;
            obj.transform.position = new Vector3(
                                         Random.Range(-rangeX, rangeX),
                                         1f, 
                                         Random.Range(-rangeZ, rangeZ)) 
                                     + transform.position;
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
                position = new Vector3(Random.Range(-rangeX, rangeX), 1f,
                    Random.Range(-rangeZ, rangeZ))  + transform.position;
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
            var afa = agent.GetComponent<AuctionFrontierAgent>();
            if (agent.activeSelf) afa.DisableAgent();
            agent.SetActive(true);
            
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }
            
            if (HasStatioon)
            {
                agent.transform.position = SpawnPositions[idx % SpawnPositions.Count];
            }
            else
            {
                agent.transform.position = new Vector3(Random.Range(-rangeX, rangeX), 1f,
                    Random.Range(-rangeZ, rangeZ))  + transform.position;
            }

            agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 180)));
            idx++;
        }

        if (m_Objectives.Count > 0)
        {
            foreach (var obj in m_Objectives)
            {
                Destroy(obj);
            }
            CreateObjectives(numObjectives, objective);
            //ResetObjectives();
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
