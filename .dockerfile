FROM anibali/pytorch:1.8.1-cuda11.1
WORKDIR /app
USER root
RUN sudo apt-get update

RUN sudo apt-get update
RUN sudo apt-get install -y git
RUN sudo apt-get install -y wget && rm -rf /var/lib/apt/lists/*
RUN rm -rf Thesis-ObjectCollector/
ADD https://api.github.com/repos/HadiSDev/Thesis-ObjectCollector/git/refs/heads/master version.json
RUN git clone -b master https://github.com/HadiSDev/Thesis-ObjectCollector.git 

RUN pip install mlagents
RUN chmod -R 755 /app/Thesis-ObjectCollector/exe/ObjectCollector.x86_64
# mlagents-learn Thesis-ObjectCollector/config/ppo/PPORun.yaml --env=Thesis-ObjectCollector/exe/ObjectCollector --run-id=Problem_A_full_1_PPO --no-graphics --env-args --mlagents-scene-name Problem_A_full_1
# mlagents-learn Thesis-ObjectCollector/config/poca/PocaRun.yaml --env=Thesis-ObjectCollector/exe/ObjectCollector --run-id=Problem_A_full_1_Poca --no-graphics --env-args --mlagents-scene-name Problem_A_full_1

# CMD mlagents-learn Thesis-ObjectCollector/config/GridObjectCollectorLSTMCamera.yaml --env=Thesis-ObjectCollector/exes/scenario_C/scenario_C --run-id=ScenarioCCamera --no-graphics
# --gpus=all --volume "I:\Thesis-ObjectCollector\results:/app/results"