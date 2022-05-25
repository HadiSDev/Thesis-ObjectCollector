import pandas as pd
import glob
import csv

base_dir = "./AuctionFrontier_ProblemD"
read_dir = base_dir + "/AuctionFrontierD_Variant1_Dist10"

all_files = glob.glob(read_dir + "/*.csv")

 
# Id  NumOfAgents  CompletionTime  MinReward  ...  AvgDistTravelled  MaxDistTravelled  SDDistTravelled  TotalShares
# Writing to file
dff = pd.DataFrame()
for filename in all_files:
    df = pd.read_csv(filename)
    entry = dict()
    entry["aa_filename"] = filename.split("/")[-1]
    entry["agents_num"] = df["NumOfAgents"].mean()
    # Completion time
    entry["complete_time_avg"] = df["CompletionTime"].mean() 
    entry["complete_time_avg_std"] = df["CompletionTime"].std() 


    # Distance
    entry["dist_travelled_avg"] = df["AvgDistTravelled"].mean() 
    entry["dist_travelled_avg_std"] = df["AvgDistTravelled"].std() 

    entry["totaldist"] = entry["agents_num"] * df["AvgDistTravelled"].mean() 
    entry["totaldist_std"] = (df["NumOfAgents"] * df["AvgDistTravelled"]).std()

    entry["max_dist_travelled_avg"] = df["MaxDistTravelled"].mean() 
    entry["max_dist_travelled_std"] = df["MaxDistTravelled"].std() 

    entry["min_dist_travelled_avg"] = df["MinDistTravelled"].mean() 
    entry["min_dist_travelled_std"] = df["MinDistTravelled"].std() 

    # Cells discovered
    entry["cell_discovered_avg"] = df["AvgAgentStep"].mean() 
    entry["max_cell_discovered_avg"] = df["MaxAgentStep"].mean() 
    entry["max_cell_discovered_std"] = df["MaxAgentStep"].std() 
    entry["min_cell_discovered_avg"] = df["MinAgentStep"].mean() 
    entry["min_cell_discovered_std"] = df["MinAgentStep"].std() 

    # Object Collected
    entry["obj_collected_avg"] = df["AvgReward"].mean() 
    entry["max_obj_collected_avg"] = df["MaxReward"].mean() 
    entry["max_obj_collected_std"] = df["MaxReward"].std() 
    entry["min_obj_collected_avg"] = df["MinReward"].mean() 
    entry["min_obj_collected_std"] = df["MinReward"].std() 

    # Capacity-Share
    entry["num_capacity_share"] = df["TotalShares"].mean()
    entry["num_capacity_share_std"] = df["TotalShares"].std()


    #print(entry)
    df_dictionary = pd.DataFrame([entry])
    dff = pd.concat([dff, df_dictionary], ignore_index=True)


dff.to_csv(read_dir+"/AuctionFrontierD_var1_Dist10.csv", sep=',', mode='a')
