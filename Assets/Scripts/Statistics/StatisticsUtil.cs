using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using DefaultNamespace;
using Interfaces;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statistics
{
    public static class StatisticsWriter
    {
        
        public static readonly string BASE_DIRECTORY = "./Assets/Scripts/Statistics/";
        private static List<Stats> m_Records = new List<Stats>();
        private static List<float> m_AgentRewards = new List<float>();
        private static List<float> m_AgentTravelDist = new List<float>();
        private static List<int> m_AgentSteps = new List<int>();
        private static List<int> m_Shares = new List<int>();
        private static int updateCounter;

        public static int NumAgents { get; set; }
        public static string FileName { get; set; }
        public static string WriteDirectory { get; set; }
        public static bool IsEvaluating { get; set; }


        public static void Main()
        {
            var folderPath = "";
        }

        public static void AppendAgentStats(float agentReward, float agentTravelDist, int agentStep)
        {
            m_AgentRewards.Add(agentReward);
            m_AgentTravelDist.Add(agentTravelDist);
            m_AgentSteps.Add(agentStep);
        }
        
        public static void AppendAgentStatsMaxStep(float agentReward, float agentTravelDist, int agentStep, int id, TimeSpan elapTime, int shares)
        {
            m_AgentRewards.Add(agentReward);
            m_AgentTravelDist.Add(agentTravelDist);
            m_AgentSteps.Add(agentStep);
            m_Shares.Add(shares);

            if (++updateCounter % NumAgents == 0 && IsEvaluating)
            {
                Debug.Log("Writing to csv...");
                AppendStatToRecordList(id, elapTime);
                AuctionFrontierUtil.FINISHED = true;
                plotResults();
                PrepareEpisodeStats();
            }
        }

        public static void PrepareEpisodeStats()
        {
            m_AgentRewards.Clear();
            m_AgentSteps.Clear();
            m_AgentTravelDist.Clear();
            m_Shares.Clear();
            m_Records.Clear();
        }

        public static void plotResults()
        {
            Directory.CreateDirectory(BASE_DIRECTORY + WriteDirectory);
            var filePath = $"./Assets/Scripts/Statistics/{WriteDirectory}/{FileName}.csv";
            var exist = File.Exists(filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // Don't write the header again.
                HasHeaderRecord = false,
            };

            using (var writer = File.AppendText(filePath))
            using (var csv = new CsvWriter(writer, exist ? config : new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(m_Records);
                csv.Flush();
                csv.Dispose();
            }
        }
        
        public static void AppendStatToRecordList(int id,  TimeSpan elapTime)
        {
            var record = new Stats(id, (elapTime.TotalSeconds * Time.timeScale).ToString());
            record.ComputeAgentSpecificStats(m_AgentRewards, m_AgentSteps, m_AgentTravelDist, m_Shares);
            m_Records.Add(record);
        }
    }

    public class Stats
    {
        public int Id { get; set; }
        public int NumOfAgents { get; set; }
        public string CompletionTime { get; set; }
        
        public float MinReward { get; set; } 
        public float AvgReward { get; set; }
        public float MaxReward { get; set; }
        public double SDReward { get; set; }
        
        public float MinAgentStep { get; set; }
        public float AvgAgentStep { get; set; }
        public float MaxAgentStep { get; set; }
        public double SDAgentStep { get; set; }

        public float MinDistTravelled { get; set; }
        public float AvgDistTravelled { get; set; }
        public float MaxDistTravelled { get; set; }
        public double SDDistTravelled { get; set; }

        public float TotalShares { get; set; }



        public Stats(int id, string completionTime)
        {
            Id = id;
            CompletionTime = completionTime;
        }

        public void ComputeAgentSpecificStats(List<float> rewards, List<int> stepCounts, List<float> distances, List<int> shares)
        {
            NumOfAgents = distances.Count;
            MinDistTravelled = distances.Min();
            AvgDistTravelled = distances.Average();
            MaxDistTravelled = distances.Max();
            SDDistTravelled = StandardDeviation(distances);
            
            MinReward = rewards.Min();
            AvgReward = rewards.Average();
            MaxReward = rewards.Max();
            SDReward = StandardDeviation(rewards);
            
            MinAgentStep = stepCounts.Min();
            AvgAgentStep = (int) stepCounts.Average();
            MaxAgentStep = stepCounts.Max();

            TotalShares = shares.Sum();
        }

        public double StandardDeviation(IEnumerable<float> sequence)
        {
            double result = 0;
            if (sequence.Any())
            {
                float average = sequence.Average();
                double sum = sequence.Sum(d => Math.Pow((double)d - (double)average, 2));
                result = Math.Sqrt((sum) / sequence.Count());
            }
            
            return result;
        }
    }
}