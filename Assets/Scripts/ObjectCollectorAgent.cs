using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class ObjectCollectorAgent : Agent
{
    ObjectCollectorSettings m_ObjectCollectorSettings;
    public GameObject area;
    ObjectCollectorArea m_MyArea;
    Rigidbody m_AgentRb;
    // Speed of agent rotation.
    public float turnSpeed = 300;

    // Speed of agent movement.
    public float moveSpeed = 2;
    public bool contribute;
    public bool useVectorObs;

    // Punishment Settings
    public float stepCost = -0.001f;

    // Capacity Settings
    public bool enableCapacity = false;
    public float maxCapacity = 25;
    private float collectedCapacity = 0;
    public float depositRewardPerObjective = 0.1f;

    // Station

    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();
        m_MyArea = area.GetComponent<ObjectCollectorArea>();
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
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
            var capacityRatio = collectedCapacity / maxCapacity;
            sensor.AddObservation(capacityRatio);
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
        EndEpsiodeIfNoObjectives();

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var continuousActions = actionBuffers.ContinuousActions;

        var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        var rotate = Mathf.Clamp(continuousActions[2], -1f, 1f);

        dirToGo = transform.forward * forward;
        dirToGo += transform.right * right;
        rotateDir = -transform.up * rotate;

        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        if (m_AgentRb.velocity.sqrMagnitude > 25f) // slow it down
        {
            m_AgentRb.velocity *= 0.95f;
        }

        AddReward(stepCost);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
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
        collectedCapacity = 0;
        SetResetParameters();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            if (!enableCapacity || collectedCapacity + 1f <= maxCapacity)
            {
                collision.gameObject.GetComponent<ObjectLogic>().OnEaten();
                collectedCapacity += 1f;

                AddReward(1f);
                if (contribute)
                {
                    m_ObjectCollectorSettings.totalScore += 1;
                }
            }
        }

        EndEpsiodeIfNoObjectives();
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("station") && enableCapacity)
        {
            var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
            if (collectedCapacity > 0)
            {
                var reward = depositRewardPerObjective * collectedCapacity;
                AddReward(reward);
                collectedCapacity = 0;
            }
            else if (n_objects == 0)
            {
                EndEpisode();
            }
        }
    }

    private void EndEpsiodeIfNoObjectives()
    {
        var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
        if (n_objects == 0 && !enableCapacity || n_objects == 0  && collectedCapacity == 0)
        {
            Debug.Log("Epsiode Ended");
            EndEpisode();
        }
    }

    public void SetAgentScale()
    {
        float agentScale = m_ResetParams.GetWithDefault("agent_scale", 1.0f);
        gameObject.transform.localScale = new Vector3(agentScale, agentScale, agentScale);
    }

    public void SetResetParameters()
    {
        SetAgentScale();
    }
}
