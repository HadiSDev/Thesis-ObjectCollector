using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GreedyAgent : MonoBehaviour
{
    private NavMeshAgent m_Agent;
    private Transform m_Target;

    private ObjectCollectorSettings m_ObjectCollectorSettings;

    void Start()
    {
        StartCoroutine(ExampleCoroutine());
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_Agent = GetComponent<NavMeshAgent>();
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

    public float checkEvery; // check every x second
    float time;

    void Update()
    {
        time += Time.deltaTime;
        if (!(time >= checkEvery)) return;
        m_Target = FindClosestObject()?.transform;
        if (m_Target == null)
        {
            m_ObjectCollectorSettings.EnvironmentReset();
        }
        else
        {
            m_Agent.SetDestination(m_Target.position);
        }
        time = 0;
    }

}
