using System.Collections.Generic;
using System.Linq;
using CustomDetectableObjects;
using Unity.Mathematics;
using UnityEngine;

namespace DefaultNamespace
{
    public static class AuctionFrontierUtil
    {
        private static int nextId = -1;
        public static HashSet<GameObject> DISCOVERED_TARGETS = new HashSet<GameObject>();
        public static HashSet<GameObject> TARGETS = new HashSet<GameObject>();

        public static bool FINISHED = false;
        public static float env_diagonal_distance;
        private static int counter;

        public static void CLEAR()
        {
            DISCOVERED_TARGETS = new HashSet<GameObject>();
            TARGETS = new HashSet<GameObject>();
        }

        public static void AddToGlobalObjectList(GameObject obj)
        {
            if (obj.activeSelf) DISCOVERED_TARGETS.Add(obj);
        }

        public static GameObject GetNearestDiscoveredObject(Vector3 worldPosition)
        {
            GameObject closest = null;
            float distance = Mathf.Infinity;
            var DISCOVERED_TARGETS = GameObject.FindGameObjectsWithTag("objective").Where(o =>
                !o.GetComponent<DetectableVisibleObject>().isTargeted &&
                o.GetComponent<DetectableVisibleObject>().isDetected);
            foreach (GameObject obj in DISCOVERED_TARGETS)
            {
                var curdist = Vector3.Distance(worldPosition, obj.transform.position);
        
                if (curdist < distance && obj.activeSelf)
                {
                    distance = curdist;
                    closest = obj;
                }
            }

            return closest;
        }
        
        public static GameObject FindClosetsObjectWithTag(Vector3 worldPosition, string tag)
        {
            GameObject[] gos;
            gos = GameObject.FindGameObjectsWithTag(tag);
            GameObject closest = null;
            float distance = Mathf.Infinity;
            foreach (GameObject obj in gos)
            {
                var curdist = Vector3.Distance(worldPosition, obj.transform.position);
                if (curdist < distance && obj.activeSelf)
                {
                    distance = curdist;
                    closest = obj;
                }
            }
            return closest;
        }
        
        public static int GetNextId()
        {
            return ++nextId;
        }
        
        public static float CalculateAgentBid(Vector3 worldPosition, Vector3 targetPosition, float explorerRate, float capacityRatio)
        {
            if (capacityRatio == 1f) return -1f;
            var distance = Vector3.Distance(worldPosition, targetPosition);
            return (1 - distance/env_diagonal_distance) + (1-explorerRate) +  (1-capacityRatio); 
        }

        public enum AuctionStage
        {
            Bidding,
            NotifyWinner,
            TaskAnnouncement,
            NoAuction
        }
        
        public enum CELL_STATE
        {
            UNKNOWN_REGION,
            KNOWN_OPEN,
            KNOWN_CLOSE,
            FRONTIER_OPEN,
            FRONTIER_CLOSE
        }

        public enum AuctionFrontierRole
        {
            Auctioneer,
            Bidder,
            Explorer,
            Worker
        }
        
        public enum MessageType
        {
            AuctionStart,
            Bid, 
            Winner,
            Loser
        }

        public class Message
        {
            public Message(AuctionFrontierAgent sender, AuctionFrontierAgent receiver,  MessageType header, GameObject task, float bid=0f)
            {
                Bid = bid;
                Sender = sender;
                Receiver = receiver;
                Header = header;
                Task = task;
            }
            
            public Message(AuctionFrontierAgent sender,  MessageType header, GameObject task, float bid=0f)
            {
                Bid = bid;
                Sender = sender;
                Header = header;
                Task = task;
            }

            public float Bid { get; set; }

            public AuctionFrontierAgent Sender { get; set; }
            public AuctionFrontierAgent Receiver { get; set; }

            public MessageType Header { get; set; }

            public GameObject Task { get; set; }
            
        }
        

    }
}