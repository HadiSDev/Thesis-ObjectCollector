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
using UnityEngine.UI;

public class ObjectCollectorAgent : Agent, IStats
{
    ObjectCollectorSettings m_ObjectCollectorSettings;
    Rigidbody m_AgentRb;

    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 1;
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
    private BufferSensorComponent _bufferSensorObjectives;
    private BufferSensorComponent _bufferSensorAgents;
    
    // Station
    EnvironmentParameters m_ResetParams;
    private ObjectCollectorArea m_ObjectCollectorArea;
    

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        prev_pos = transform.position; 
        
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_ObjectCollectorArea = FindObjectOfType<ObjectCollectorArea>();
        var bufferSensors = GetComponents<BufferSensorComponent>();

        if (bufferSensors != null)
        {
            _bufferSensorObjectives = GetComponents<BufferSensorComponent>().FirstOrDefault(b => b.SensorName == "ObjectivesSensor");
            _bufferSensorAgents = GetComponents<BufferSensorComponent>().FirstOrDefault(b => b.SensorName == "AgentsSensor");
        }
        
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        if (markDetectedObjects)
        {
            POVGrid = GetComponent<GridSensorComponent3D>();
        }
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var localVelocity = transform.InverseTransformDirection(m_AgentRb.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        if (enableCapacity)
        {
            var capacityRatio = m_CollectedCapacity / maxCapacity;
            sensor.AddObservation(capacityRatio);
        }
        
        var progress = m_ObjectCollectorSettings.totalCollected / m_ObjectCollectorArea.numObjectives;
        
        sensor.AddObservation(progress);

        var agentPos = transform.position;
        var areaPos = m_ObjectCollectorArea.transform.position;
        sensor.AddObservation((agentPos.x - areaPos.x) / 50f);
        sensor.AddObservation((agentPos.z - areaPos.z) / 50f);

        var forward = transform.forward;
        sensor.AddObservation(forward.x);
        sensor.AddObservation(forward.z);
        
        var station = GameObject.FindGameObjectWithTag("station").transform.localPosition;
        var distance = Vector3.Distance(agentPos, station) / 50f;
        sensor.AddObservation(distance);
        
        if (_bufferSensorObjectives != null)
        {
            var objectives = GameObject.FindGameObjectsWithTag("objective");
            
            foreach (var objective in objectives)
            {
                var pos = objective.transform.localPosition;
                var x = (pos.x - agentPos.x) / 50f;
                var z = (pos.z - agentPos.z) / 50f;
                _bufferSensorObjectives.AppendObservation(new []{x, z});
            }
        }

        if (_bufferSensorAgents != null)
        {
            var agents = GameObject.FindGameObjectsWithTag("agent").Where(a => a != gameObject);

            foreach (var agent in agents)
            {
                var pos = agent.transform.localPosition;
                var x = (pos.x - agentPos.x) / 50f;
                var z = (pos.z - agentPos.z) / 50f;
                _bufferSensorAgents.AppendObservation(new []{x, z});
            }
        }
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

    private void MoveAgent(ActionBuffers actionBuffers)
    {
        if (!enabled)
        {
            return;
        }
        
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;
        

        var forward = Mathf.Clamp(continuousActions[0], 0, 1f);
        var rotate = Mathf.Clamp(continuousActions[1], -1f, 1f);
        //Debug.Log($"forward: {forward}, rotate: {rotate}");
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

        m_ObjectCollectorSettings.m_AgentGroup.AddGroupReward(stepCost);
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
        if (!enabled)
        {
            return;
        }
        
        var continuousActionsOut = actionsOut.ContinuousActions;
        if (Input.GetKey(KeyCode.D))
        {
            continuousActionsOut[1] = -1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            continuousActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            continuousActionsOut[1] = 1;
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
        m_CollectedCapacity = 0;
        ResetStats();
        SetResetParameters();
    }

    void OnTriggerObjective(Collider collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            if (!enableCapacity || m_CollectedCapacity + 1f <= maxCapacity)
            {
                collision.gameObject.GetComponent<ObjectLogic>().OnEaten();
                m_CollectedCapacity += 1f;
                m_ObjectCollectorSettings.totalCollected += 1f;
                m_ObjectCollectorSettings.m_AgentGroup.AddGroupReward(1f);
                if (contribute)
                {
                    m_ObjectCollectorSettings.totalScore += 1;
                }
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
                m_ObjectCollectorSettings.m_AgentGroup.AddGroupReward(reward);
                m_CollectedCapacity = 0;
            }
            
            if (n_objects == 0 && m_CollectedCapacity == 0)
            {
                DisableAgentAndTerminateEpisodeIfDone();
            }
        }

        OnTriggerObjective(collision);
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
            m_ObjectCollectorSettings.m_AgentGroup.EndGroupEpisode();
            m_ObjectCollectorSettings.EnvironmentReset();

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
