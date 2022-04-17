using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public static class AuctionFrontierUtil
    {
        private static int nextId = -1;
        private static float env_diagonal_distance = 141.42f;
        public static Queue<GameObject> DISCOVERED_TARGETS = new Queue<GameObject>();

        public static GameObject GetNearestDiscoveredObject(Vector3 worldPosition)
        {
            GameObject closest = null;
            float distance = Mathf.Infinity;
            
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
        
        public static Vector3 FindClosetsObjectWithTag(Vector3 worldPosition, string tag)
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
            return closest.transform.position;
        }

        public static int GetNextId()
        {
            return ++nextId;
        }

        public static float CalculateAgentBid(Vector3 worldPosition, Vector3 targetPosition, float explorerRate, float capacityRatio)
        {
            var distance = Vector3.Distance(worldPosition, targetPosition);
            return (env_diagonal_distance - distance) * (1f - explorerRate) * (1f - capacityRatio); 
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
            NONE,
            MAP_OPEN,
            MAP_CLOSE,
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