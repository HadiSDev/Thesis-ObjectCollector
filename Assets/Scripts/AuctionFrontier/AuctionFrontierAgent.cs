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
    public bool ShareCapacity = false;
    
    public bool enableCapacity;
    public float maxCapacity = 15;
    private float m_CollectedCapacity;
    private float m_TotalCollectted;
    private int m_Shares;
    private int timestep;
    private NavMeshAgent m_Agent;
    private GridSensorComponent3D grid;
    private AuctionFrontierCollectorSettings m_ObjectCollectorSettings;

    private DateTime sTime;
    public float dist_travelled;
    private Vector3 previous_pos;
    public float biddingTimeLimit = 5f;
    public float checkEvery; // check every x second
    float m_Time;
    
    // Auction-Frontier variables
    private Dictionary<int, AuctionFrontierAgent> m_Network;
    private Queue<AuctionFrontierUtil.Message> m_Messages;
    private Dictionary<int, float> m_Bids;
    private Queue<int> m_ExplorationEfficiency;
    private List<int> m_DiscoveredCells;
    public int timeIntervalInSteps;


    // Auctioneer
    private AuctionFrontierUtil.AuctionStage m_AuctionStage;
    private AuctionFrontierUtil.AuctionFrontierRole m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
    
    // Bidder
    private int m_AuctioneerId;
    private GameObject m_AuctionItem;
    
    // Explorer & Worker
    private GameObject m_Target;
    
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
            //Debug.DrawRay(pos, tmp.transform.forward * max, Color.yellow, 2f);
            if (i < limit) tmp.transform.Rotate(Vector3.up, lon / limit);
        }

        var v2 = tmp.transform.forward * max;

        float[] zs = new[] { v1.z + pos.z, v2.z + pos.z, pos.z };
        float[] xs = new[] { v1.x + pos.x, v2.x + pos.x, pos.x };

        var offset_z = 15f;
        var offset_x = 150f;
        var cellSize = 5f;

        var h = 6;
        var w = 60;
        
        var z_min = (int) Math.Clamp((zs.Min() + offset_z) / cellSize , 0f, h-1);
        var z_max = (int) Math.Clamp((zs.Max() + offset_z) / cellSize , 0f, h-1);

        var x_min = (int) Math.Clamp((xs.Min()  + offset_x) / cellSize , 0f, w-1);
        var x_max = (int) Math.Clamp((xs.Max()  + offset_x) / cellSize , 0f, w-1);
            
        
        //Debug.Log($"Z - min {z_min}  max {z_max}");
        Debug.Log($"X - min {x_min}  max {x_max}");

        
        Debug.DrawRay(v1 + pos, (v2 - v1), Color.cyan, .5f);
        DestroyImmediate(tmp);

        
        Debug.Log($"POS : {pos}");
        Debug.Log($"V1 : {v1}");
        Debug.Log($"V2 : {v2}");
        
        // Test detection with one object
        // must create a gameobject first in scene titled - "test
        var tst = GameObject.Find("test");
        Debug.Log($"TST (POS): {tst.transform.position}");
        var angle1 = Vector3.Angle(v1, tst.transform.position-pos);
        var angle2 = Vector3.Angle(v2, tst.transform.position-pos);
        var rdist = Vector3.Distance(pos, tst.transform.position);
        
        Debug.Log($"Angle {angle1}  Angle2 {angle2} dist: {rdist}");
        Debug.DrawRay(pos, tst.transform.position - pos, Color.green, .5f);
        
        
        Debug.Log($"V1 : {v1}");
        Debug.Log($"V2 : {v2}");
        Debug.Log($"Angle (outer): {Vector3.Angle(v1, v2)}");
        Debug.DrawRay(pos, tst.transform.position - pos, Color.cyan, .5f)
        


        for (float z = z_min; z <= z_max; z++)
        {
            for (float x = x_min; x <= x_max; x++)
            {
                // Check if cell is a stored frontier point. Remove if true
                var coord = Vector3.Scale(new Vector3(x, 1f, z), new Vector3(cellSize, 1f, cellSize)) - new Vector3(offset_x, 0f, offset_z);
                Debug.Log(coord);
                
                //Debug.Log(coord);

                // (Vector3.forward * (cellSize/2f)
                angle1 = Vector3.Angle(v1, coord-pos);
                angle2 = Vector3.Angle(v2, coord-pos);
                var dist = Vector3.Distance(pos, coord);
                    
                    

                    
                if (angle1 <= lon && angle2 <= lon && dist <= max) Debug.DrawRay(pos, (coord - pos), Color.cyan, 0.01f);

                //Debug.Log($"Angle (Point): {Vector3.Angle(v1, tst.transform.position - pos)} Dist: {Vector3.Distance(pos, tst.transform.position)}");


                //DestroyImmediate(tst);
            }
        }
    
        
    
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
    }
        
    */

    #region capacity sharing

    public decimal RoundDown(decimal i, double decimalPlaces)
    {
        var power = Convert.ToDecimal(Math.Pow(10, decimalPlaces));
        return Math.Floor(i * power) / power;
    }
    
    public float GetCapacity()
    {
        return m_CollectedCapacity;
    }

    public void SetCapacity(float val)
    {
        m_CollectedCapacity = val;
    }

    public void ShareCap(GameObject agent)
    {
        var other_agent = agent.GetComponent<AuctionFrontierAgent>();
        var other_cap = other_agent.GetCapacity();
        if (other_cap < m_CollectedCapacity)
        {
            var cap_left = maxCapacity - m_CollectedCapacity;
            if (cap_left >= other_cap)
            {
                other_agent.SetCapacity(0);
                m_CollectedCapacity += other_cap;
                if (other_cap > 0)
                {
                    m_Shares += 1;
                }
            }
            else
            {
                other_agent.SetCapacity(other_cap-cap_left);
                m_CollectedCapacity += cap_left;
                if (cap_left > 0)
                {
                    m_Shares += 1;
                }
            }

        }
    }
    
    public void ShareCapTwo(GameObject agent)
    {
        var other_agent = agent.GetComponent<AuctionFrontierAgent>();
        var other_cap = other_agent.GetCapacity();
        if (other_cap < m_CollectedCapacity - 1)
        {
            
            var cap_to_transfer = (float) RoundDown((Convert.ToDecimal(m_CollectedCapacity-other_cap))/2, 0);
            other_agent.SetCapacity( other_cap + cap_to_transfer);
            m_CollectedCapacity -= cap_to_transfer;
            m_Shares += 1;

            if (m_Target != null && m_Target.CompareTag("station")) m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
            if (other_agent.m_Target != null && other_agent.m_Target.CompareTag("station")) other_agent.m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;

        }
    }
    #endregion

    public void Awake()
    {
        Id = AuctionFrontierUtil.GetNextId();
        InitAgent();
        m_ObjectCollectorSettings = FindObjectOfType<AuctionFrontierCollectorSettings>();
        m_Agent = GetComponent<NavMeshAgent>();
        grid = GetComponent<GridSensorComponent3D>();
    }

    public void Start()
    {
        ResetAgent();
        Thread.Sleep(100);
    }

    public void ResetAgent()
    {
        Debug.Log($"Init agent{Id}...");
        InitAgent();
        ResetStats();
        timestep = 0;
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
        m_Target = null;
        m_Agent.ResetPath();
        m_Agent.isStopped = false;
        sTime = DateTime.Now;
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
                AuctionFrontierUtil.DISCOVERED_TARGETS.Add(item);
            }
            
            return true;
        }

        return false;
    }
    
    public bool DetectObjectsAsWorker()
    {
        var discovered = grid.GetDetectedGameObjects("objective").Where(x => x.GetComponent<DetectableVisibleObject>().isNotDetected);
        GameObject closets = m_Target;
        
        if (discovered.Any())
        {
            // Only get those that arent discovered...
            foreach (var item in discovered)
            {
                var detected_object = item.GetComponent<DetectableVisibleObject>();
                detected_object.isNotDetected = false;
                detected_object.isDetected = true;
                
                // If discovered item is closer than current target. Change to closets target
                if (Vector3.Distance(m_Agent.transform.position, item.transform.position) < Vector3.Distance(m_Agent.transform.position, closets.transform.position))
                {
                    Debug.Log($"Agent{Id} -> Discovered item closer to agent...");
                    closets = item;
                    AuctionFrontierUtil.DISCOVERED_TARGETS.Add(m_Target); // Enqueue previous target
                }
            }

            AuctionFrontierUtil.TARGETS.Add(closets);
            m_Agent.SetDestination(closets.transform.position);
            return true;
        }

        return false;
    }


    private void AssignDestination(Vector3 target)
    {
        if (target != Vector3.up && m_CollectedCapacity < maxCapacity)
        {
            m_Agent.SetDestination(target);
            Debug.DrawRay(target, Vector3.up * 10f, Color.green, checkEvery);
        }
        else // No more frontiers left to explore
        {
            if (GridTracking.GridWorldComplete() || m_CollectedCapacity >= maxCapacity)
            {
                Debug.Log($"Agent{Id} - Going to nearest station...");
                AssignDestinationToNearestStation();
            }
        }
    }

    public void AssignDestinationToNearestStation()
    {
        if (m_Target != null && m_Target.activeSelf && m_Target.CompareTag("objective")) AuctionFrontierUtil.DISCOVERED_TARGETS.Add(m_Target);
        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
        var nearest = AuctionFrontierUtil.FindClosetsObjectWithTag(transform.position, "station");
        m_Target = nearest;
        m_Agent.SetDestination(nearest.transform.position);
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

        // In case there are some sync error with appending discovered items to global objects list

        /*var global = GameObject.FindGameObjectsWithTag("objective").Where(o =>
            !o.GetComponent<DetectableVisibleObject>().isTargeted &&
            o.GetComponent<DetectableVisibleObject>().isDetected); */
        // Find closets discovered objects;
        foreach (GameObject obj in AuctionFrontierUtil.DISCOVERED_TARGETS)
        {
            var curdist = Vector3.Distance(transform.position, obj.transform.position);
            
            if (curdist < distance && obj.activeSelf)
            {
                distance = curdist;
                closets = obj;
            }
        }
        
        if (closets != null)
        {
            Debug.LogWarning($"Agent{Id} - {m_Role} - {m_AuctionStage} - Removing target from global list: {closets.transform.position}");
            closets.GetComponent<DetectableVisibleObject>().isTargeted = true;
            AuctionFrontierUtil.DISCOVERED_TARGETS.Remove(closets);
            AuctionFrontierUtil.TARGETS.Add(closets);
        }

        return closets;
    }

    public void DiscoveredCellsUpdate(int num)
    {
        m_DiscoveredCells.Add(num);
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
            m_TotalCollectted++;
        }
    }

    private bool EpisodeFinished()
    {
        var n_objects = GameObject.FindGameObjectsWithTag("objective").Length;
        return n_objects == 0 && m_CollectedCapacity == 0 && GridTracking.GridWorldComplete();
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
            
            if (n_objects == 0 && m_CollectedCapacity == 0 && GridTracking.GridWorldComplete())
            {
                DisableAgent();
            }
        }
    }
    
    void Update()
    {
        /* DEBUG - DRAW detected objects + collected objects */ 
        foreach (var frontier in AuctionFrontierUtil.DISCOVERED_TARGETS)
        {
            Debug.DrawRay(frontier.transform.position, Vector3.up * 10f, Color.yellow, checkEvery);
        }
        
        /*
        foreach (var tar in AuctionFrontierUtil.TARGETS)
        {
            Debug.DrawRay(tar.transform.position, Vector3.up * 10f, Color.magenta, checkEvery);
        }
        */

        //Debug.Log($"Agent{Id} - Role: {m_Role} : Stage: {m_AuctionStage}");
        
        
        timestep++;
        var HasDetectedObjects = DetectObjects();
        AuctionFrontierUtil.Message msg;
        var target = Vector3.zero;

        // Update internal stats
        var cur_position = m_Agent.transform.position;
        var dist = Vector3.Distance(previous_pos, cur_position);
        dist_travelled+= dist;
        previous_pos = cur_position;
        
        // Capacity sharing
        if (ShareCapacity && Vector3.Distance(GameObject.FindGameObjectWithTag("station").transform.position, cur_position) > 20)
        {
            var agents = FindObjectsOfType<AuctionFrontierAgent>();
            foreach (var agent in agents)
            {
                if (agent.gameObject != gameObject)
                {
                    var dist_to_agent = Vector3.Distance(agent.gameObject.transform.position, cur_position);
                   
                    //1st variant
                    //if (dist_to_agent < 20 && agent.GetCapacity() + m_CollectedCapacity >= maxCapacity)
                    //{
                    //    ShareCap(agent.gameObject);
                    //}

                    // 2nd variant
                    if (dist_to_agent < 10)
                    {
                        ShareCapTwo(agent.gameObject);
                        break;
                    }
                   
                   
                }
            }
        }

        
        if (m_Messages.TryDequeue(out msg))
        {
            TryProcessMessage(msg);
        }
        
        
        /*
            var D = GameObject.FindGameObjectsWithTag("objective").Where(o =>
            !o.GetComponent<DetectableVisibleObject>().isTargeted &&
            o.GetComponent<DetectableVisibleObject>().isDetected);
        */
        switch (m_Role)
        {
            // Initiate auction for discovered objectives
            case AuctionFrontierUtil.AuctionFrontierRole.Auctioneer:
                if (AuctionFrontierUtil.DISCOVERED_TARGETS.Count > 0 && m_AuctionItem == null)
                {
                    m_AuctionItem = FindClosetsDiscoveredObject();
                    if (m_AuctionItem != null)
                    {
                        m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                    }
                }
                else if (AuctionFrontierUtil.DISCOVERED_TARGETS.Count == 0 && m_AuctionItem == null)
                {
                    Debug.Log($"Agent{Id} - Auctioneer going back to being explorer");
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    m_AuctioneerId = -1;
                    break;
                }

                switch (m_AuctionStage)
                {
                    // Broadcast objective location to all agents
                    case AuctionFrontierUtil.AuctionStage.TaskAnnouncement:
                        InitAuction();
                        m_Time = 0;
                        break;
                
                    // Receive biddings from all agent
                    case AuctionFrontierUtil.AuctionStage.Bidding:
                        m_Time += Time.deltaTime;
                        
                        // Listen for bids...
                        if (m_Messages.TryDequeue(out msg))
                        {
                            TryProcessMessage(msg);
                        }
                        if (!m_Bids.Values.Contains(0f)) m_AuctionStage = AuctionFrontierUtil.AuctionStage.NotifyWinner;

                            if (m_Time > biddingTimeLimit) // If not all have responded... make auctioneer collect
                        {
                            Debug.Log($"Agent{Id} - Auctioneer did not receive all bids. Assigning target to itself... ");
                            m_Agent.SetDestination(m_AuctionItem.transform.position);
                            m_Target = m_AuctionItem;
                            m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
                            m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                            m_Agent.isStopped = false;
                            m_AuctionItem = null;
                            ResetBiddingTable();
                        }
                        
                        break;
                
                    // Broadcast winner
                    case AuctionFrontierUtil.AuctionStage.NotifyWinner:
                        var winnerId = m_Bids.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                        var winner = m_Network[winnerId];
                        
                        // Notify bidders who won
                        Debug.LogWarning($"Agent{Id} - Notifying winnerId: {winnerId} => target: {m_AuctionItem.transform.position}");
                        msg = new AuctionFrontierUtil.Message(this, AuctionFrontierUtil.MessageType.Winner, m_AuctionItem, winner.Id);
                        BroadcastMessage(msg);
                        
                        // If auctioneer was elected
                        if (winner.Id == Id)
                        {
                            Debug.LogWarning($"Agent{Id} - Auctioneer won target: {m_AuctionItem.transform.position}...");
                            m_Target = m_AuctionItem;
                            m_Agent.SetDestination(m_Target.transform.position);
                            m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
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
                        if (m_AuctionItem == null || !m_AuctionItem.activeSelf) // Assuming that global view detects if object have already been collected by other agent
                        {
                            ExplorerInit(); // Revert back to being explorer
                            break;
                        }
                        
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
                        m_Time = 0;
                        break;
                    
                    case AuctionFrontierUtil.AuctionStage.NotifyWinner:
                        m_Time += Time.deltaTime;
                        if (m_Time > biddingTimeLimit) // If no winner has been announced - revert back to being an explorer
                        {
                            if (m_AuctionItem != null && m_AuctionItem.activeSelf)
                            {
                                //AuctionFrontierUtil.DISCOVERED_TARGETS.Add(m_AuctionItem);
                                m_AuctionItem.GetComponent<DetectableVisibleObject>().isTargeted = false;
                                m_AuctionItem = null;
                            }
                            ExplorerInit();
                            m_Time = 0;
                            break;
                        }

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
                if (HasDetectedObjects || AuctionFrontierUtil.DISCOVERED_TARGETS.Count > 0)
                {
                    m_AuctioneerId = Id;
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Auctioneer;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.TaskAnnouncement;
                    m_AuctionItem = FindClosetsDiscoveredObject();
                    InitAuction();
                    break;
                }
                
                if (GridTracking.GridWorldComplete() && AuctionFrontierUtil.DISCOVERED_TARGETS.Count == 0 || m_CollectedCapacity >= maxCapacity)
                {
                    AssignDestinationToNearestStation();
                    if (EpisodeFinished() && m_Agent.remainingDistance < 1f)
                    {
                        DisableAgent();
                    }
                    break;
                }

                if (m_Messages.TryDequeue(out msg))
                {
                    TryProcessMessage(msg); 
                    break;
                }
                
                
                m_Time += Time.deltaTime;
                if (m_Time <= checkEvery)
                {
                    return;
                }
                m_Time = 0;
                
                // Assign destination based-on policy
                target = GridTracking.WFD(transform.position, checkEvery);
                AssignDestination(target);
                break;

            
            case AuctionFrontierUtil.AuctionFrontierRole.Worker:
                if (m_CollectedCapacity >= maxCapacity)
                {
                    if (m_Target != null && m_Target.activeSelf && m_Target.CompareTag("objective"))
                    {
                        Debug.LogWarning($"Agent{Id} - Worker exceeding capacity. Target: {m_Target.transform.position} reset. Returning to station...");
                        AuctionFrontierUtil.DISCOVERED_TARGETS.Add(m_Target);
                        m_Target.GetComponent<DetectableVisibleObject>().isTargeted = false;
                        AssignDestinationToNearestStation();
                        break;
                    }
                }

                // Simulation end in case 
                if (EpisodeFinished() && m_Agent.remainingDistance < 1f)
                {
                    DisableAgent();
                    break;
                }
                
                // Tell agent to start exploring when task is done or task is gone
                if (m_Agent.remainingDistance < .15f || !m_Target.activeSelf)
                {
                    Debug.Log($"Agent{Id} - Finished work..."); 
                    ExplorerInit();
                }
                
                break;
        }
    }

    #region Auction

    public void InitAuction()
    {
        var msg = new AuctionFrontierUtil.Message(this, AuctionFrontierUtil.MessageType.AuctionStart, m_AuctionItem);
        BroadcastMessage(msg);
        m_AuctionStage = AuctionFrontierUtil.AuctionStage.Bidding;
        var auctioneerBid = AuctionFrontierUtil.CalculateAgentBid(transform.position,
            m_AuctionItem.transform.position, CalculateExplorationRate(), m_CollectedCapacity/maxCapacity);
        m_Bids[Id] = auctioneerBid > 0f ? auctioneerBid : -1f;
        m_Time = 0;
    }

    #endregion
    
    #region Statistics
    
    public void DisableAgent()
    {
        // AgentReward := # of collected objectives
        // AgentStep := # of discovered cells in gridworld
        Debug.Log($"Disabling Agent{Id}...");
        if(m_ObjectCollectorSettings.m_Is_evaluating) StatisticsWriter.AppendAgentStatsMaxStep(m_TotalCollectted, dist_travelled, m_DiscoveredCells.Sum(), Id, DateTime.Now - sTime, m_Shares);
        gameObject.SetActive(false);
    }
    
    public void ResetStats()
    {
        dist_travelled = 0;
        m_TotalCollectted = 0;
        sTime = DateTime.Now;
    } 

    public void ExplorerInit()
    {
        m_Role = AuctionFrontierUtil.AuctionFrontierRole.Explorer;
        m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    
        // Resume going towards allocated frontier point
        m_Agent.isStopped = false;
        m_AuctionItem = null;
        
        var target = GridTracking.WFD(transform.position, checkEvery);
        AssignDestination(target);
        m_Time = 0f;
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
        m_DiscoveredCells = new List<int>();
        m_TotalCollectted = 0;
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
                    //m_Agent.isStopped = true;
                    m_AuctioneerId = msg.Sender.Id;
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Bidder;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.Bidding;
                    m_AuctionItem = msg.Task;
                }
                else if (m_Role == AuctionFrontierUtil.AuctionFrontierRole.Worker || m_Role == AuctionFrontierUtil.AuctionFrontierRole.Bidder)
                {
                    var busy_message = new AuctionFrontierUtil.Message(this,
                        AuctionFrontierUtil.MessageType.Bid,
                        null,
                        -1f);
                    m_Network[msg.Sender.Id].QueueMessage(busy_message);
                }
                else if (m_Role == AuctionFrontierUtil.AuctionFrontierRole.Auctioneer) // In case two agents discover ah the same
                {
                    Debug.Log($"Agent{Id} - Abandon auction and continue as bidder on target: {msg.Task.transform.position}...");

                    m_AuctioneerId = msg.Sender.Id;
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Bidder;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.Bidding;
                    if (m_AuctionItem != null) // Add item back to queue
                    {
                        Debug.Log($"Agent{Id} - Reinserting object {m_AuctionItem.transform.position}...");
                        AuctionFrontierUtil.DISCOVERED_TARGETS.Add(m_AuctionItem);
                        m_AuctionItem.GetComponent<DetectableVisibleObject>().isTargeted = false;
                    }

                    m_AuctionItem = msg.Task;
                }

                break;
            
            case AuctionFrontierUtil.MessageType.Bid:
                m_Bids[msg.Sender.Id] = msg.Bid;
                break;
                
            case AuctionFrontierUtil.MessageType.Winner:
                var winnerId = msg.Bid; // Field used to hold winner id :)
                Debug.LogWarning($"Agent{Id} - {m_Role} - Received auction winner for {msg.Task.transform.position}");
                if (winnerId == Id && m_Role == AuctionFrontierUtil.AuctionFrontierRole.Bidder)
                {
                    m_Target = msg.Task;
                    Debug.LogWarning($"Agent:{Id} - Assigning target: {m_Target.transform.position}...");
                    m_Agent.SetDestination(m_Target.transform.position);
                    m_Role = AuctionFrontierUtil.AuctionFrontierRole.Worker;
                    m_AuctionStage = AuctionFrontierUtil.AuctionStage.NoAuction;
                    m_AuctioneerId = -1;
                    m_Agent.isStopped = false;
                    m_AuctionItem = null;
                }
                else if (winnerId == Id && m_Role == AuctionFrontierUtil.AuctionFrontierRole.Worker || m_Role == AuctionFrontierUtil.AuctionFrontierRole.Auctioneer)
                {
                    Debug.LogWarning($"Agent{Id} - {m_Role} - Agent is currently busy to collect {msg.Task.transform.position}. Reinserting element");
                    AuctionFrontierUtil.DISCOVERED_TARGETS.Add(msg.Task);
                    m_AuctioneerId = -1;
                    msg.Task.GetComponent<DetectableVisibleObject>().isTargeted = false;
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
