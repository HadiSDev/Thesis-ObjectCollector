using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Statistics
{
    public static class StatisticsWriter
    {
        
        private static readonly string BASE_DIRECTORY = "./Assets/Scripts/Statistics/";
        private static List<Stats> m_Records = new List<Stats>();

        private static List<float> m_AgentRewards = new List<float>();
        private static List<float> m_AgentTravelDist = new List<float>();
        private static List<int> m_AgentSteps = new List<int>();
        private static int updateCounter;

        public static int NumAgents { get; set; }
        public static string FileName { get; set; }
        public static string WriteDirectory { get; set; }
        public static bool IsEvaluating { get; set; }

        public static void AppendAgentStats(float agentReward, float agentTravelDist, int agentStep)
        {
            m_AgentRewards.Add(agentReward);
            m_AgentTravelDist.Add(agentTravelDist);
            m_AgentSteps.Add(agentStep);
        }
        
        public static void AppendAgentStatsMaxStep(float agentReward, float agentTravelDist, int agentStep, int id, TimeSpan elapTime)
        {
            m_AgentRewards.Add(agentReward);
            m_AgentTravelDist.Add(agentTravelDist);
            m_AgentSteps.Add(agentStep);

            if (++updateCounter % NumAgents == 0 && IsEvaluating)
            {
                AppendStatToRecordList(id, elapTime);
                plotResults();
                PrepareEpisodeStats();
            }
        }

        public static void PrepareEpisodeStats()
        {
            m_AgentRewards.Clear();
            m_AgentSteps.Clear();
            m_AgentTravelDist.Clear();
        }
        
        public static void plotResults()
        {
            Directory.CreateDirectory( BASE_DIRECTORY + WriteDirectory);
            using (var writer = new StreamWriter($"./Assets/Scripts/Statistics/{WriteDirectory}/{FileName}.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(m_Records);
                csv.Flush();
                csv.Dispose();
            }
        }
        
        public static void AppendStatToRecordList(int id,  TimeSpan elapTime)
        {
            var record = new Stats(id, elapTime.TotalSeconds.ToString());
            record.ComputeAgentSpecificStats(m_AgentRewards, m_AgentSteps, m_AgentTravelDist);
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



        public Stats(int id, string completionTime)
        {
            Id = id;
            CompletionTime = completionTime;
        }

        public void ComputeAgentSpecificStats(List<float> rewards, List<int> stepCounts, List<float> distances)
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