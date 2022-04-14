using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public static class AuctionFrontierUtil
    {
        private static int nextId = -1;

        public static HashSet<GameObject> DISCOVERED_TARGETS = new HashSet<GameObject>();

        public static GameObject GetNearestDiscoveredObject(Vector3 worldPosition)
        {
            GameObject closest = null;
            float distance = Mathf.Infinity;
            
            foreach (GameObject obj in DISCOVERED_TARGETS)
            {
                var curdist = Vector3.Distance(worldPosition, obj.transform.position);
        
                if (curdist < distance)
                {
                    distance = curdist;
                    closest = obj;
                }
            }

            DISCOVERED_TARGETS.Remove(closest);
            return closest;
        }

        public static int GetNextId()
        {
            return ++nextId;
        }

        public static float CalculateAgentBid(Vector3 worldPosition, Vector3 targetPosition, float explorerRate, float capacityRatio)
        {
            var distance = Vector3.Distance(worldPosition, targetPosition);
            return (100f-distance) * explorerRate * capacityRatio;
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