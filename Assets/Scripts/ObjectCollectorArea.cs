
using Unity.MLAgentsExamples;
using UnityEngine;
using UnityEngine.AI;

public class ObjectCollectorArea : Area
{
    public GameObject objective;
    public int numObjectives;
    public float range;
    public NavMeshObstacle obstacle;
    public int numObstacles;
    public GameObject[] stations;
    public int maxSpawnAttemptsPerObstacle = 10;

    void CreateObjectives(int num, GameObject type)
    {
        for (int i = 0; i < num; i++)
        {
            GameObject f = Instantiate(type, new Vector3(Random.Range(-range, range), 1f,
                Random.Range(-range, range)) + transform.position,
                Quaternion.Euler(new Vector3(0f, Random.Range(0f, 360f), 90f)));
            f.GetComponent<ObjectLogic>().myArea = this;
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
        var firstStation = stations[0];
        foreach (GameObject agent in agents)
        {
            if (agent.transform.parent == gameObject.transform)
            {
                /*firstStation.transform.position = new Vector3(Random.Range(-range, range), 1f,
                    Random.Range(-range, range))  + transform.position;*/
                agent.transform.position = firstStation.transform.position;
                agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
            }
        }

        CreateObjectives(numObjectives, objective);
        CreateObstacles();
    }

    public override void ResetArea()
    {
    }
}
