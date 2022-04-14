using System;
using System.Collections;
using System.Linq;
using System.Threading;
using CustomDetectableObjects;
using Interfaces;
using MBaske.Sensors.Grid;
using Statistics;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.AI;

public class GreedyAgent : MonoBehaviour, IStats
{
    private NavMeshAgent m_Agent;
    private Transform m_Target;
    private GridSensorComponent3D grid;

    public DateTime sTime;
    public float dist_travelled;
    private Vector3 previous_pos;

    public float checkEvery; // check every x second
    float m_Time;

    private ObjectCollectorSettings m_ObjectCollectorSettings;

    void Start()
    {
        StartCoroutine(ExampleCoroutine());
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_ObjectCollectorSettings.EnvironmentReset();
        m_Agent = GetComponent<NavMeshAgent>();
        grid = GetComponent<GridSensorComponent3D>();
        previous_pos = m_Agent.transform.position;
        sTime = DateTime.Now;
    }

    /*
    // FOR DEBUGGING
    private void OnDrawGizmos()
    {
        var pos = transform.position;
        var tmp = new GameObject();
        tmp.transform.SetPositionAndRotation(transform.position, transform.rotation);

        var max = 20f;
        var lat = 24.4f;
        var lon = 62.2f;
        var limit = 10;

        tmp.transform.Rotate(Vector3.down, lon / 2);
        var v1 = tmp.transform.forward * max;

        for (int i = 0; i <= limit; i++)
        {
            Debug.DrawRay(pos, tmp.transform.forward * max, Color.yellow, 2f);
            if (i < limit) tmp.transform.Rotate(Vector3.up, lon / limit);
        }

        var v2 = tmp.transform.forward * max;

        float[] z = new[] { v1.z, v2.z, pos.z };
        float[] x = new[] { v1.x + pos.x, v2.x + pos.x, pos.x };

        var z_min = z.Min();
        var z_max = z.Max();

        var x_min = x.Min();
        var x_max = x.Max();

        Debug.Log($"Z - min {z_min}  max {z_max}");
        Debug.Log($"X - min {x_min}  max {x_max}");



        Debug.DrawRay(v1 + pos, (v2 - v1), Color.cyan, .5f);
        DestroyImmediate(tmp);

        var tst = GameObject.Find("tst");

        Debug.Log($"POS : {pos}");
        Debug.Log($"TST (POS): {tst.transform.position}");

        Debug.DrawRay(pos, tst.transform.position - pos, Color.cyan, .5f);

        Debug.Log($"V1 : {v1}");
        Debug.Log($"V2 : {v2}");

        Debug.Log($"Angle (outer): {Vector3.Angle(v1, v2)}");
        var angle1 = Vector3.Angle(v1, tst.transform.position-pos);
        var angle2 = Vector3.Angle(v2, tst.transform.position-pos);
        var rdist = Vector3.Distance(pos, tst.transform.position);

        Debug.Log($"Angle {angle1}  Angle2 {angle2} dist: {rdist}");
        
        //Debug.Log($"Angle (Point): {Vector3.Angle(v1, tst.transform.position - pos)} Dist: {Vector3.Distance(pos, tst.transform.position)}");


        //DestroyImmediate(tst);

    }
    */

    /*

    Debug.DrawRay(pos, tmp.transform.forward * max, Color.red, 10f);
        tmp.transform.Rotate(Vector3.down, lon);
        Debug.DrawRay(pos, tmp.transform.forward * max, Color.red, 10f);
        
        tmp.transform.Rotate(Vector3.up, (2*lon));
        Debug.DrawRay(pos, tmp.transform.forward * max, Color.red, 10f);

        transform.Rotate(Vector3.up, lon);
        Debug.DrawRay(pos, transform. * max, Color.red, 10f);
        transform.Rotate(Vector3.down, -lon*2);
        Debug.DrawRay(pos, transform.forward * max, Color.red, 10f);
        transform.Rotate(Vector3.up, lon);
    }
    */

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
            StatisticsWriter.AppendAgentStatsMaxStep(0f, dist_travelled, 0, 0, DateTime.Now-sTime);
            m_ObjectCollectorSettings.EnvironmentReset();
            dist_travelled = 0f;
            sTime = DateTime.Now;
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
