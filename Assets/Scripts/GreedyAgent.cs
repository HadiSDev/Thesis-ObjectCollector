using System.Collections;
using CustomDetectableObjects;
using Interfaces;
using MBaske.Sensors.Grid;
using UnityEngine;
using UnityEngine.AI;

public class GreedyAgent : MonoBehaviour, IStats
{
    private NavMeshAgent m_Agent;
    private Transform m_Target;
    private GridSensorComponent3D grid;
    
    public float dist_travelled;
    private Vector3 previous_pos;

    public float checkEvery; // check every x second
    float m_Time;

    private ObjectCollectorSettings m_ObjectCollectorSettings;

    void Start()
    {
        StartCoroutine(ExampleCoroutine());
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_Agent = GetComponent<NavMeshAgent>();
        grid = GetComponent<GridSensorComponent3D>();
        previous_pos = m_Agent.transform.position;

    }

    IEnumerator ExampleCoroutine()
    {
        yield return new WaitForSeconds(5);
        m_Target = FindClosestObject().transform;
        m_Agent.destination = m_Target.position;
    }
    
    public GameObject FindClosestObject()
    {
        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag("objective");
        GameObject closest = null;
        float distance = Mathf.Infinity;
        Vector3 position = transform.position;
        foreach (GameObject go in gos)
        {
            Vector3 diff = go.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closest = go;
                distance = curDistance;
            }
        }
        return closest;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            //Satiate();
            collision.gameObject.GetComponent<ObjectLogic>().OnEaten();
        }
    }
    
    void Update()
    {
        var detected = grid.GetDetectedGameObjects("objective");
        foreach (var det in detected)
        {
            det.GetComponent<DetectableVisibleObject>().isNotDetected = false;
            det.GetComponent<DetectableVisibleObject>().isDetected = true;
        }

        var cur_position = m_Agent.transform.position;
        dist_travelled+= Vector3.Distance(previous_pos, cur_position);
        previous_pos = cur_position;
        
        m_Time += Time.deltaTime;
        if (!(m_Time >= checkEvery)) return;
        m_Target = FindClosestObject()?.transform;
        if (m_Target == null)
        {
            m_ObjectCollectorSettings.EnvironmentReset();
            dist_travelled = 0f;
        }
        else
        {
            m_Agent.SetDestination(m_Target.position);
        }
        m_Time = 0;
    }

    public void ResetStats()
    {
        dist_travelled = 0;
    }

    public float GetAgentCumulativeDistance()
    {
        return dist_travelled;
    }

    public int GetAgentStepCount()
    {
        return 0;
    }

    public float GetAgentCumulativeReward()
    {
        return 0f;
    }
}
