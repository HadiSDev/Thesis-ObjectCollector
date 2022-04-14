using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgentsExamples;
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
    public bool m_EnableStations = true;
    private IList<GameObject> m_Objectives = new List<GameObject>();
    private IList<GameObject> m_Stations = new List<GameObject>();

    void CreateObjectives(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 0.5f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.identity);
            f.GetComponent<ObjectLogic>().myArea = this;
            m_Objectives.Add(f);
        }
    }
    
    void CreateStations(int num)
    {
        var stationPositions = new List<(int, int)>()
        {
            (45, 45), (-45, 45), (-45, -45), (45, -45)
        };

        stationPositions = stationPositions.OrderBy(_ => Random.Range(0, 100)).ToList();

        for (int i = 0; i < num; i++)
        {
            var index = Random.Range(0, stationPositions.Count);
            var pos = stationPositions[index];
            GameObject f = Instantiate(m_StationType, new Vector3(pos.Item1, 0.5f,
                    pos.Item2) + transform.position, Quaternion.identity);
            m_Stations.Add(f);
            stationPositions.RemoveAt(index);
        }
    }

    public void ResetObjectives()
    {
        foreach (var obj in m_Objectives)
        {
            obj.transform.position = new Vector3(
                Random.Range(-range, range),
                0.5f,
                Random.Range(-range, range)) + transform.position;
            obj.SetActive(true);
            
        }
    }
    
    public void ResetStations()
    {
        var stationPositions = new List<(int, int)>()
        {
            (45, 45), (-45, 45), (-45, -45), (45, -45)
        };
        

        stationPositions = stationPositions.OrderBy(_ => Random.Range(0, 100)).ToList();
        foreach (var obj in m_Stations)
        {
            var index = Random.Range(0, stationPositions.Count);
            var pos = stationPositions[index];
            obj.transform.position = new Vector3(
                pos.Item1
                ,
                0.5f, pos.Item2
                ) + transform.position;
            
            stationPositions.RemoveAt(index);
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
        if (m_EnableStations)
        {
            if (m_Stations.Count > 0)
            {
                ResetStations();
            }
            else
            {
                var numStations = Random.Range(1, 4);
                CreateStations(numStations);
            }
        }
        
        foreach (var agent in agents)
        {
            agent.gameObject.SetActive(true);
            if (agent.transform.parent != gameObject.transform)
            {
                continue;
            }

            var station = m_Stations.Count > 0 ?  m_Stations[Random.Range(0, m_Stations.Count)] : null;
            
            if (station != null)
            {
                var offsetX = Random.Range(-2, 2);
                var offsetY = Random.Range(-2, 2);
                agent.transform.position = station.transform.position + new Vector3(offsetX, 1f, offsetY);
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
