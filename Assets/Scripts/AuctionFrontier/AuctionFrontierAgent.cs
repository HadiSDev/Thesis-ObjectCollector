using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CustomDetectableObjects;
using DefaultNamespace;
using Interfaces;
using MBaske.Sensors.Grid;
using Statistics;
using Unity.MLAgents;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

[RequireComponent(typeof(GridSensorComponent3D))]
public class AuctionFrontierAgent : MonoBehaviour, IStats
{
    [HideInInspector]
    public int Id;
    
    public bool enableCapacity;
    public float maxCapacity = 25;
    private float m_CollectedCapacity;
    
    private NavMeshAgent m_Agent;
    private GridSensorComponent3D grid;
    private ObjectCollectorSettings m_ObjectCollectorSettings;

    public DateTime sTime;
    public float dist_travelled;
    private Vector3 previous_pos;
    public float checkEvery; // check every x second
    float m_Time;
    
    // Auction-Frontier variables
    private Dictionary<int, AuctionFrontierAgent> m_Network;
    private Queue<AuctionFrontierUtil.Message> m_Messages;
    private Dictionary<int, float> m_Bids;

    
    // Auctioneer
    private AuctionFrontierUtil.AuctionStage m_AuctionStage;
    private AuctionFrontierUtil.AuctionFrontierRole m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
    private Queue<GameObject> m_AuctionItems;
    
    // Bidder
    private int m_AuctioneerId;
    private GameObject m_AuctionItem;
    
    // Explorer & Worker
    public GameObject m_Target;

    public void Awake()
    {
        Id = AuctionFrontierUtil.GetNextId();
        m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
        m_AuctioneerId = -1;
        m_AuctionItems = new Queue<GameObject>();
        m_Messages = new Queue<AuctionFrontierUtil.Message>();
        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
        m_Network = new Dictionary<int, AuctionFrontierAgent>();
        m_Bids = new Dictionary<int, float>();
        m_Target =  new GameObject("Target");
    }

    void Start()
    {
        Debug.Log("Start...");
        InitAgentNetwork();
        StartCoroutine(RotateCoroutine());
        m_ObjectCollectorSettings = FindObjectOfType<ObjectCollectorSettings>();
        m_ObjectCollectorSettings.EnvironmentReset();
        m_Agent = GetComponent<NavMeshAgent>();
        grid = GetComponent<GridSensorComponent3D>();
        previous_pos = m_Agent.transform.position;
        sTime = DateTime.Now;
    }

    
    IEnumerator RotateCoroutine()
    {
        yield return new WaitForSeconds(2);
        m_Target.transform.position = m_Agent.transform.position;
        m_Agent.SetDestination(m_Agent.transform.position);
    }
    
    public Vector3 FindClosetsFrontier()
    {
        Vector3 closets = Vector3.zero;
        float distance = Mathf.Infinity;
        foreach (Vector3 coord in GridTracking.FRONTIERS)
        {
            var curdist = Vector3.Distance(transform.position, coord);
            
            if (curdist < distance)
            {
                distance = curdist;
                closets = coord;
            }
        }

        GridTracking.FRONTIERS.Remove(closets);
        return closets;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            //Satiate();
            collision.gameObject.GetComponent<ObjectLogic>().OnEaten();
            m_CollectedCapacity++;
        }
    }
    
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("station") && enableCapacity)
        {
            var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
            if (m_CollectedCapacity > 0)
            {
                m_CollectedCapacity = 0;
            }
        }
    }
    
    void Update()
    {
        /* DEBUG - DRAW STORED FRONTIER POINTS*/
        foreach (var frontier in GridTracking.FRONTIERS)
        {
            Debug.DrawRay(frontier, Vector3.up * 10f, Color.blue, checkEvery);
        }
        
        AuctionFrontierUtil.Message msg;
        switch (m_Role)
        {
            // Initiate auction for discovered objectives
            case AuctionFrontierUtil.AuctionFrontierRole.Auctioneer:
                if (m_AuctionItems.Count > 0 && m_AuctionItem == null)
                {
                    m_AuctionItem = m_AuctionItems.Dequeue();
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                }
                else if (m_AuctionItems.Count == 0 && m_AuctionItem == null)
                {
                    Debug.Log("Auctioneer going back to being explorer");
                    m_Agent.isStopped = false;
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    m_AuctioneerId = -1;
                    break;
                }

                switch (m_AuctionStage)
                {
                    // Broadcast objective location to all agents
                    case AuctionFrontierUtil.AuctionStage.TaskAnnouncement:
                        msg = new AuctionFrontierUtil.Message(this, AuctionFrontierUtil.MessageType.AuctionStart, m_AuctionItem);
                        BroadcastMessage(msg);
                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.Bidding;
                        m_Bids[Id] = AuctionFrontierUtil.CalculateAgentBid(transform.position,
                            m_AuctionItem.transform.position, 1f, 1f); // TODO: Update args later
                        break;
                
                    // Receive biddings from all agent TODO: Move on when enough time have passed (to be implemented)
                    case AuctionFrontierUtil.AuctionStage.Bidding:
                        if (m_Messages.TryDequeue(out msg))
                        {
                            TryProcessMessage(msg);
                            if (!m_Bids.Values.Contains(0f))
                            {
                                m_AuctionStage = AuctionFrontierUtil.AuctionStage.NotifyWinner;
                            }
                        }
                        break;
                
                    // Broadcast winner
                    case AuctionFrontierUtil.AuctionStage.NotifyWinner:
                        var winnerId = m_Bids.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                        var winner = m_Network[winnerId];
                        
                        // Notify bidders who won
                        msg = new AuctionFrontierUtil.Message(this, AuctionFrontierUtil.MessageType.Winner, winner.gameObject);
                        BroadcastMessage(msg);
                        
                        // If auctioneer was elected
                        if (winner.Id == Id)
                        {
                            m_Agent.SetDestination(m_AuctionItem.transform.position);
                            m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
                            
                            // TODO: Case when there are more items to be auctioned but none available agents
                            GameObject item;
                            while (m_AuctionItems.TryDequeue(out item))
                            {
                                Debug.Log("Emptying discovered items...");
                            }

                            m_Agent.isStopped = false;
                            break;
                        }
                        else
                        {
                            // DEBUG: Remove later
                            m_Agent.SetDestination(winner.gameObject.transform.position);
                        }


                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                        m_AuctionItem = null;
                        ResetBiddingTable();
                        break;
                }
                break;
            
            // Start bidding on auction item 
            case AuctionFrontierUtil.AuctionFrontierRole.Bidder:
                switch (m_AuctionStage)
                {
                    case AuctionFrontierUtil.AuctionStage.Bidding:
                        var curr_pos = m_Agent.transform.position;
                        var target_pos = m_AuctionItem.transform.position;
                        var explorerRate = 1f;
                        var capacityRatio = m_CollectedCapacity / maxCapacity;
                        var bid = AuctionFrontierUtil.CalculateAgentBid(curr_pos, target_pos, explorerRate, 1);
                        msg = new AuctionFrontierUtil.Message(this,
                            m_Network[m_AuctioneerId],
                            AuctionFrontierUtil.MessageType.Bid,
                            null,
                            bid);
                        SendMessage(msg);
                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.NotifyWinner;
                        break;
                    
                    case AuctionFrontierUtil.AuctionStage.NotifyWinner:
                        if (m_Messages.TryDequeue(out msg))
                        {
                            TryProcessMessage(msg);
                        }
                        break;
                    
                    default:
                        Console.WriteLine("Error");
                        break;
                }
                break;

            // Behaviour following frontier exploration algorithm 
            case AuctionFrontierUtil.AuctionFrontierRole.Explorer:
                if (m_Messages.TryDequeue(out msg))
                {
                    TryProcessMessage(msg);
                    break;
                }

                var discovered = grid.GetDetectedGameObjects("objective").Where(x => x.GetComponent<DetectableVisibleObject>().isNotDetected);
                // discovered = discovered.Where(x => x.GetComponent<DetectableVisibleObject>().isNotDetected);

                /* // Remove comment when frontier works
                if (discovered.Any())
                {
                    // Only get those that arent discovered...
                    foreach (var item in discovered)
                    {
                        item.GetComponent<DetectableVisibleObject>().isNotDetected = false;
                        item.GetComponent<DetectableVisibleObject>().isDetected = true;
                        m_AuctionItems.Enqueue(item);
                    }
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Auctioneer;
                    m_AuctioneerId = Id;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                    
                    break;
                }
                */
                
                var cur_position = m_Agent.transform.position;
                dist_travelled+= Vector3.Distance(previous_pos, cur_position);
                previous_pos = cur_position;
                
                m_Time += Time.deltaTime;
                if (!(m_Time >= checkEvery)) return;
                var target = GridTracking.WFD(m_Target.transform.position, checkEvery);
                
                if (target != Vector3.up)
                {
                    Debug.Log($"Assigning new dest: {target}");
                    GridTracking.FRONTIERS.Remove(target);
                    m_Target.transform.position = target;
                    m_Agent.SetDestination(target);
                    Debug.DrawRay(target, Vector3.up * 10f, Color.green, checkEvery);

                }
                else
                {
                    if (Vector3.Distance(transform.position, m_Target.transform.position) > 10.0f)
                    {
                        m_Agent.SetDestination(m_Target.transform.position);
                        Debug.Log($"Current frontier target is very far. Continue to target {m_Target.transform.position}... ");
                        break;
                    }

                    var nearest = FindClosetsFrontier();
                    m_Target.transform.position = nearest;
                    m_Agent.SetDestination(nearest);
                    Debug.Log($"WFD found nothing. Talking nearest from global list {nearest}");

                }

                /*m_Target = FindClosestObject()?.transform;
                if (m_Target == null)
                {
                    StatisticsWriter.AppendAgentStatsMaxStep(0f, dist_travelled, 0, 0, DateTime.Now-sTime);
                    //m_ObjectCollectorSettings.EnvironmentReset();
                    dist_travelled = 0f;
                    sTime = DateTime.Now;
                }
                else
                {
                    m_Agent.SetDestination(m_Target.position);
                }
                */
                m_Time = 0;
                break;

            
            case AuctionFrontierUtil.AuctionFrontierRole.Worker:
                if (m_Messages.TryDequeue(out msg))
                {
                    TryProcessMessage(msg);
                }

                // Tell agent to start exploring when task is done
                if (m_Agent.remainingDistance <= 1.0f)
                {
                    Debug.Log("Finished the work. Reverting back to exlporer");
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;

                    // TODO: Assign nearest unassigned frontier point to agent 
                    // var target = GridTracking.WFD(transform.position, checkEvery);

                }

                break;
        }

        
    }
    
    #region Statistics
    
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
    
    #endregion
    
    #region Network

    public void InitAgentNetwork()
    {
        m_Messages = new Queue<AuctionFrontierUtil.Message>();
        
        // Filter on layer to ensure only one agent 'part' is returned per agent
        var agents = GameObject.FindGameObjectsWithTag("agent").AsEnumerable().
            Where(a => a.layer == 0).Select(x => x.GetComponent<AuctionFrontierAgent>());
        foreach (var agent in agents)
        {
            m_Network[agent.Id] = agent;
            m_Bids[agent.Id] = 0f;
        }
    }
    
    public void BroadcastMessage(AuctionFrontierUtil.Message msg)
    {
        foreach (var agent in m_Network.Values)
        {
            if (agent.Id != msg.Sender.Id) agent.QueueMessage(msg);
        }
    }

    void ResetBiddingTable()
    {
        foreach (var k in m_Bids.Keys)
        {
            m_Bids[k] = 0f;
        }
    }

    public void SendMessage(AuctionFrontierUtil.Message msg)
    {
        m_Network[msg.Receiver.Id].QueueMessage(msg);
    }

    void QueueMessage(AuctionFrontierUtil.Message msg)
    {
        m_Messages.Enqueue(msg);
    }

    public AuctionFrontierUtil.Message TryGetMessage()
    {
        AuctionFrontierUtil.Message msg = null;
        m_Messages.TryDequeue(out msg);
        return msg;
    }

    
    public void TryProcessMessage(AuctionFrontierUtil.Message msg)
    {
        Debug.Log($"Parsing messages from {msg.Sender.Id} - {msg.Header}");
        switch (msg.Header)
        {
            case AuctionFrontierUtil.MessageType.AuctionStart:
                if (m_Role == AuctionFrontierUtil.AuctionFrontierRole.Explorer)
                {
                    m_Agent.isStopped = true;
                    m_AuctioneerId = msg.Sender.Id;
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Bidder;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.Bidding;
                    m_AuctionItem = msg.Task;
                }
                else if (m_Role == AuctionFrontierUtil.AuctionFrontierRole.Worker)
                {
                    var busy_message = new AuctionFrontierUtil.Message(this,
                        AuctionFrontierUtil.MessageType.Bid,
                        null,
                        -1f);
                    m_Network[msg.Sender.Id].QueueMessage(busy_message);
                }

                break;
            
            case AuctionFrontierUtil.MessageType.Bid:
                m_Bids[msg.Sender.Id] = msg.Bid;
                break;
                
            case AuctionFrontierUtil.MessageType.Winner:
                var winnerId = msg.Task.GetComponent<AuctionFrontierAgent>().Id;
                if (winnerId == Id)
                {
                    Debug.Log($"Agent:{winnerId} won...");
                    m_Target = m_AuctionItem;
                    m_Agent.SetDestination(m_Target.transform.position);
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    m_Agent.isStopped = false;
                    m_AuctionItem = null;
                }
                else
                {
                    // Ignore if worker...
                    if (m_Role == AuctionFrontierUtil.AuctionFrontierRole.Worker) break;
                    
                    Debug.Log($"Agent:{Id} lost...");
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    
                    // TODO: Change to resume going towards allocated frontier location
                    m_Agent.SetDestination(msg.Sender.transform.position); // DEBUG
                    m_Agent.isStopped = false;
                    m_AuctionItem = null;
                }
                break;

            default:
                Console.WriteLine("Error in reading message");
                break;
        }
    }
    
    #endregion





}
