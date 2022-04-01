FROM anibali/pytorch:1.8.1-cuda11.1
WORKDIR /app
USER root
RUN sudo apt-get update

RUN sudo apt-get update
RUN sudo apt-get install -y git
RUN sudo apt-get install -y wget && rm -rf /var/lib/apt/lists/*
RUN rm -rf Thesis-ObjectCollector/
RUN git clone https://github.com/HadiSDev/Thesis-ObjectCollector.git
RUN pip install mlagents
RUN chmod -R 755 /app/Thesis-ObjectCollector/exe/ObjectCollector.x86_64
# mlagents-learn Thesis-ObjectCollector/config/poca/PocaRun.yaml --env=Thesis-ObjectCollector/exes/Problem_A_full_2/Problem_A_full_2 --run-id=Problem_A_full_2_Poca --no-graphics
# CMD mlagents-learn Thesis-ObjectCollector/config/GridObjectCollectorLSTMCamera.yaml --env=Thesis-ObjectCollector/exes/scenario_C/scenario_C --run-id=ScenarioCCamera --no-graphics