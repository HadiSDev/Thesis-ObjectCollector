using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CustomDetectableObjects;
using DefaultNamespace;
using Interfaces;
using MBaske.Sensors.Grid;
using Statistics;
using UnityEngine;
using UnityEngine.AI;

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
    private AuctionFrontierCollectorSettings m_ObjectCollectorSettings;

    private DateTime sTime;
    public float dist_travelled;
    private Vector3 previous_pos;
    public float checkEvery; // check every x second
    float m_Time;
    
    // Auction-Frontier variables
    private Dictionary<int, AuctionFrontierAgent> m_Network;
    private Queue<AuctionFrontierUtil.Message> m_Messages;
    private Dictionary<int, float> m_Bids;
    private Queue<int> m_ExplorationEfficiency;
    public int timeIntervalInSteps;


    // Auctioneer
    private AuctionFrontierUtil.AuctionStage m_AuctionStage;
    private AuctionFrontierUtil.AuctionFrontierRole m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
    
    // Bidder
    private int m_AuctioneerId;
    private GameObject m_AuctionItem;
    
    // Explorer & Worker
    private GameObject m_Target;

    public void Awake()
    {
        Id = AuctionFrontierUtil.GetNextId();

        InitAgent();
        m_ObjectCollectorSettings = FindObjectOfType<AuctionFrontierCollectorSettings>();
        m_Agent = GetComponent<NavMeshAgent>();
        grid = GetComponent<GridSensorComponent3D>();
    }

    void Start()
    {
        InitAgent();
        
        m_Network = new Dictionary<int, AuctionFrontierAgent>();
        m_Messages = new Queue<AuctionFrontierUtil.Message>();
        m_Bids = new Dictionary<int, float>();

        // Filter on layer to ensure only one agent 'part' is returned per agent
        var agents = GameObject.FindGameObjectsWithTag("agent").AsEnumerable().
            Where(a => a.layer == 0).Select(x => x.GetComponent<AuctionFrontierAgent>());
        
        foreach (var agent in agents)
        {
            m_Network[agent.Id] = agent;
            m_Bids[agent.Id] = 0f;
        }
        previous_pos = m_Agent.transform.position;
        m_Agent.SetDestination(m_Agent.transform.position);
        sTime = DateTime.Now;
        //StartCoroutine(RotateCoroutine());
    }

    
    IEnumerator RotateCoroutine()
    {
        Debug.Log("Starting co...");
        yield return new WaitForSeconds(5);
    }

    #region auction-frontier
    public bool DetectObjects()
    {
        var discovered = grid.GetDetectedGameObjects("objective").Where(x => x.GetComponent<DetectableVisibleObject>().isNotDetected);
        if (discovered.Any())
        {
            // Only get those that arent discovered...
            foreach (var item in discovered)
            {
                item.GetComponent<DetectableVisibleObject>().isNotDetected = false;
                item.GetComponent<DetectableVisibleObject>().isDetected = true;
                AuctionFrontierUtil.DISCOVERED_TARGETS.Enqueue(item);
            }
            
            return true;
        }

        return false;
    }

    private void AssignDestination(Vector3 target)
    {
        if (target != Vector3.up && m_CollectedCapacity < maxCapacity)
        {
            //var distLeftToTarget = Vector3.Distance(transform.position, m_Target.transform.position);

            //if (distLeftToTarget < 100f) // Only assign new frontier when nearing its current target
            //{
            Debug.Log($"Agent{Id}: Assigning new dest: {target}");
            GridTracking.FRONTIERS.Remove(target);
            m_Target.transform.position = target;
            m_Agent.SetDestination(target);
            Debug.DrawRay(m_Target.transform.position, Vector3.up * 10f, Color.green, checkEvery);

            //} 
            //Debug.Log($"Current frontier target is still far. Continue to target {m_Target.transform.position}... ");

        }
        else // No more frontiers left to explore - Head home
        {
            /*
            if (Vector3.Distance(transform.position, m_Target.transform.position) > grid.MaxDistance/2)
            {
                m_Agent.SetDestination(m_Target.transform.position);
                Debug.Log($"Current frontier target is very far. Continue to target {m_Target.transform.position}... ");
                return;
            }
            */
            AssignDestinationToNearestStation();
            Debug.Log($"WFD found nothing... Seeking nearest station to deposit collected objects");
        }
    }

    public void AssignDestinationToNearestStation()
    {
        var nearest = AuctionFrontierUtil.FindClosetsObjectWithTag(transform.position, "station");
        m_Target.transform.position = nearest;
        m_Agent.SetDestination(nearest);
    }
    
    /*
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
    */
    
    public GameObject FindClosetsDiscoveredObject()
    {
        GameObject closets = null;
        float distance = Mathf.Infinity;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("objective").Where(obj => obj.gameObject.GetComponent<DetectableVisibleObject>().isDetected))
        {
            var curdist = Vector3.Distance(transform.position, obj.transform.position);
            
            if (curdist < distance)
            {
                distance = curdist;
                closets = obj;
            }
        }
        return closets;
    }

    public void AddExplorationScore(int score)
    {
        m_ExplorationEfficiency.Enqueue(score);
        if (m_ExplorationEfficiency.Count > timeIntervalInSteps) m_ExplorationEfficiency.Dequeue();
    }

    public int CalculateExplorationRate()
    {
        return m_ExplorationEfficiency.Sum() / GridTracking.TotalNumberOfCells;
    }

    #endregion
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("objective"))
        {
            //Satiate();
            collision.gameObject.GetComponent<AuctionFrontierObjectLogic>().OnEaten();
            m_CollectedCapacity++;
        }
    }
    
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("station"))
        {
            var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
            if (m_CollectedCapacity > 0)
            {
                m_CollectedCapacity = 0;
            }
            
            if (n_objects == 0 && m_CollectedCapacity == 0)
            {
                DisableAgentAndTerminateEpisodeIfDone();
            }
        }
    }
    
    void Update()
    {
        
        /* DEBUG - DRAW STORED FRONTIER POINTS*/
        /*
        foreach (var frontier in GridTracking.FRONTIERS)
        { Debug.DrawRay(frontier, Vector3.up * 10f, Color.yellow, checkEvery); }
        Debug.Log($"Agent{Id} - Role: {m_Role} : Stage: {m_AuctionStage}");
        */
        
        AuctionFrontierUtil.Message msg;
        var target = Vector3.zero;

        // Update internal stats
        var cur_position = m_Agent.transform.position;

        var dist = Vector3.Distance(previous_pos, cur_position);
        if (dist > .5f)
        {
            dist_travelled+= dist;
            previous_pos = cur_position;
        }
        
        
        switch (m_Role)
        {
            // Initiate auction for discovered objectives
            case AuctionFrontierUtil.AuctionFrontierRole.Auctioneer:
                if (AuctionFrontierUtil.DISCOVERED_TARGETS.Count > 0 && m_AuctionItem == null)
                {
                    var item = AuctionFrontierUtil.DISCOVERED_TARGETS.Dequeue();
                    if (item.activeSelf) // Remove inactive elements
                    {
                        m_AuctionItem = item;
                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                        //Debug.Log($"Auctioning item {item} -> stage: {m_AuctionStage}");
                    }
                }
                else if (AuctionFrontierUtil.DISCOVERED_TARGETS.Count == 0 && m_AuctionItem == null)
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
                            m_AuctionItem.transform.position, CalculateExplorationRate(), m_CollectedCapacity/maxCapacity); // TODO: Update args later
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
                        Debug.Log($"Agent{Id} - Notifying winner with Id: {winnerId}...");
                        msg = new AuctionFrontierUtil.Message(this, AuctionFrontierUtil.MessageType.Winner, winner.gameObject);
                        BroadcastMessage(msg);
                        
                        // If auctioneer was elected
                        if (winner.Id == Id)
                        {
                            m_Agent.SetDestination(m_AuctionItem.transform.position);
                            m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
                            m_Agent.isStopped = false;
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
                        var target_pos = m_AuctionItem.transform.position;
                        var explorerRate = CalculateExplorationRate();
                        var capacityRatio = m_CollectedCapacity / maxCapacity;
                        var bid = AuctionFrontierUtil.CalculateAgentBid(cur_position, target_pos, explorerRate, capacityRatio);
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
                
                if (DetectObjects())
                {
                    if (m_Messages.TryDequeue(out msg))
                    {
                        TryProcessMessage(msg); // Check that auction has not started before initiating a new auction
                        break;
                    }
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Auctioneer;
                    m_AuctioneerId = Id;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                    break;
                }
                
                m_Time += Time.deltaTime;
                if (m_Time <= checkEvery)
                {
                    return;
                }
                m_Time = 0;
                
                target = GridTracking.WFD(transform.position, checkEvery);
                // Assign destination based-on policy
                AssignDestination(target);
                break;

            
            case AuctionFrontierUtil.AuctionFrontierRole.Worker:
                var HasDetectedObjects = DetectObjects();
                if (m_Messages.TryDequeue(out msg))
                {
                    TryProcessMessage(msg);
                }

                // Tell agent to start exploring when task is done or task is gone
                if (m_Agent.remainingDistance <= .5f || m_Target.activeSelf)
                {
                    //Debug.Log("Finished the work...");

                    if (AuctionFrontierUtil.DISCOVERED_TARGETS.Count > 0) 
                    {
                        /*
                        // Initiate auction if there are items left to be auctioned (items.count > # agents)
                        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Auctioneer;
                        m_AuctioneerId = Id;
                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;  
                        */

                        // Set destination to closets discovered item
                        var closets = FindClosetsDiscoveredObject();
                        m_Target = closets;
                        m_Agent.SetDestination(closets.transform.position);
                    }
                    else // Revert back to being explorer
                    {
                        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
                        target = GridTracking.WFD(transform.position, checkEvery);
                        AssignDestination(target);
                    }
                }

                break;
        }

        
    }
    
    #region Statistics
    
    public void DisableAgentAndTerminateEpisodeIfDone()
    {
        gameObject.SetActive(false);
        if(m_ObjectCollectorSettings.m_Is_evaluating) StatisticsWriter.AppendAgentStatsMaxStep(0f, dist_travelled, 0, Id, DateTime.Now - sTime);                                       // Reset lists
        
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
    
    #endregion
    
    #region Network

    public void InitAgent()
    {
        m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
        m_AuctioneerId = -1;
        m_ExplorationEfficiency = new Queue<int>();
        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
        if (m_Target == null)
        {
            m_Target =  new GameObject("target");
        }
        else
        {
            m_Target.transform.position = m_Agent.transform.position;
        }

    }
    
    public void BroadcastMessage(AuctionFrontierUtil.Message msg)
    {
        foreach (var agent in m_Network.Values)
        {
            if (agent.Id != msg.Sender.Id)
            {
               agent.QueueMessage(msg);
            }

        }
    }

    void ResetBiddingTable()
    {
        for(int i = 0; i < m_Bids.Keys.Count; i++)
        {
            m_Bids[i] = 0f;
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
    
    public void TryProcessMessage(AuctionFrontierUtil.Message msg)
    {
        // Debug.Log($"Parsing messages from {msg.Sender.Id} - {msg.Header}");
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
                    
                    // Resume going towards allocated frontier point
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
