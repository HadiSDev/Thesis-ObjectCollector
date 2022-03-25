using System;
using System.Linq;
using CustomDetectableObjects;
using Interfaces;
using MBaske.Sensors.Grid;
using Statistics;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;

public class ObjectCollectorAgent : Agent, IStats
{
    ObjectCollectorSettings m_ObjectCollectorSettings;
    Rigidbody m_AgentRb;

    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public bool contribute;
    public bool useVectorObs;

    // Statistics
    private Vector3 prev_pos;
    private DateTime m_Start;
    private int m_Count;
    public float AgentTravelledDist;
    public int AgentStepCount;
    public float AgentCumulativeReward;

    // Punishment Settings
    public float stepCost = -0.001f;

    // Capacity Settings
    public bool enableCapacity;
    public float maxCapacity = 25;
    private float m_CollectedCapacity;

    // Dynamically mark detected objectives
    public bool markDetectedObjects;
    private GridSensorComponent3D POVGrid;
    
    // Station
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        prev_pos = transform.position; 
        
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if (markDetectedObjects)
        {
            POVGrid = GetComponent<GridSensorComponent3D>();
        }
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
            sensor.AddObservation(localVelocity.x);
            sensor.AddObservation(localVelocity.z);
        }

        if (enableCapacity)
        {
            var capacityRatio = m_CollectedCapacity / maxCapacity;
            sensor.AddObservation(capacityRatio);
        }
        
        sensor.AddObservation(m_AgentRb.position.x);
        sensor.AddObservation(m_AgentRb.position.z);
        sensor.AddObservation(m_AgentRb.rotation.x);
        sensor.AddObservation(m_AgentRb.rotation.z);
    }

    public void MarkDetectedObjectives(string tag)
    {
        var detectedObjects = POVGrid.GetDetectedGameObjects(tag);
        foreach (var detected in detectedObjects)
        {
            // Toggle switches in custom detectableobject class to display objectives correctly in global view
            detected.GetComponent<DetectableVisibleObject>().isNotDetected = false;
            detected.GetComponent<DetectableVisibleObject>().isDetected = true;
        }
    }

    public Color32 ToColor(int hexVal)
    {
        var r = (byte)((hexVal >> 16) & 0xFF);
        var g = (byte)((hexVal >> 8) & 0xFF);
        var b = (byte)(hexVal & 0xFF);
        return new Color32(r, g, b, 255);
    }

    public void MoveAgent(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[1], -1f, 1f);

        dirToGo = transform.forward * forward;
        rotateDir = -transform.up * rotate;

        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        // Update statisics
        AgentTravelledDist += Vector3.Distance(prev_pos, m_AgentRb.position);
        AgentStepCount = StepCount;
        AgentCumulativeReward = GetCumulativeReward();
        prev_pos = m_AgentRb.position;


        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
        }

        AddReward(stepCost);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
        if (markDetectedObjects)
        {
            MarkDetectedObjectives("objective");
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[2] = -1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            continuousActionsOut[0] = -1;
        }
    }

    public override void OnEpisodeBegin()
    {
        m_ObjectCollectorSettings.EnvironmentReset();
        m_AgentRb.velocity = Vector3.zero;
        /*transform.position = new Vector3(Random.Range(-m_MyArea.range, m_MyArea.range),
            2f, Random.Range(-m_MyArea.range, m_MyArea.range))
            + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));*/
        m_CollectedCapacity = 0;
        ResetStats();
        SetResetParameters();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            if (!enableCapacity || m_CollectedCapacity + 1f <= maxCapacity)
            {
                collision.gameObject.GetComponent<ObjectLogic>().OnEaten();
                m_CollectedCapacity += 1f;

                AddReward(1f);
                if (contribute)
                {
                    m_ObjectCollectorSettings.totalScore += 1;
                }
            }
            else if (Math.Abs(m_CollectedCapacity - maxCapacity) < 0.2)
            {
                AddReward(-1f);
            }
        }

        if (!enableCapacity)
        {
            EndEpsiodeIfNoObjectives();
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("station") && enableCapacity)
        {
            var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
            if (m_CollectedCapacity > 0)
            {
                var reward = m_CollectedCapacity / maxCapacity;
                AddReward(reward);
                m_CollectedCapacity = 0;
            }
            
            if (n_objects == 0 && m_CollectedCapacity == 0)
            {
                DisableAgentAndTerminateEpisodeIfDone();
            }
        }
    }

    private void EndEpsiodeIfNoObjectives()
    {
        var nObjects = GameObject.FindGameObjectsWithTag("objective").Length;
        if (nObjects == 0 && !enableCapacity || nObjects == 0  && m_CollectedCapacity == 0)
        {
            Debug.Log("Agent Epsiode Ended");
            DisableAgentAndTerminateEpisodeIfDone();
        }
    }

    public void DisableAgentAndTerminateEpisodeIfDone()
    {
        StatisticsWriter.AppendAgentStats(AgentCumulativeReward, AgentTravelledDist, AgentStepCount);
        gameObject.SetActive(false);
        // End episode if all agents are disabled
        var num_active_agents = GameObject.FindGameObjectsWithTag("agent").Length;
        if (num_active_agents == 0)
        {
            StatisticsWriter.AppendStatToRecordList(m_Count, DateTime.Now-m_Start); // Add record to list
            StatisticsWriter.PrepareEpisodeStats();                                         // Reset lists
            EndEpisode();
        }
    }
    
    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        //gameObject.transform.localScale = new Vector3(agentScale, agentScale/8, agentScale);
    }

    public void SetResetParameters()
    {
        SetAgentScale();
    }

    public float GetAgentCumulativeDistance()
    {
        return AgentTravelledDist;
    }
    
    public int GetAgentStepCount()
    {
        return AgentStepCount;
    }

    public float GetAgentCumulativeReward()
    {
        return AgentCumulativeReward;
    }

    public void ResetStats()
    {
        if (AgentStepCount == MaxStep)
        {
            StatisticsWriter.AppendAgentStatsMaxStep(AgentCumulativeReward,
                AgentTravelledDist,
                AgentStepCount,
                m_Count,
                (DateTime.Now-m_Start));
        }

        m_Start = DateTime.Now;
        m_Count++;
        AgentCumulativeReward = 0f;
        AgentStepCount = 0;
        AgentTravelledDist = 0f;
    }
}
