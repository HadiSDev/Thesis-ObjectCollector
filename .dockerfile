FROM pytorch/pytorch:1.11.0-cuda11.3-cudnn8-devel
WORKDIR /app


ENV PATH="/root/miniconda3/bin:${PATH}"
ARG PATH="/root/miniconda3/bin:${PATH}"
RUN apt-get update
RUN apt-get install -y git
RUN apt-get install -y wget && rm -rf /var/lib/apt/lists/*

RUN wget \
    https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh \
    && mkdir /root/.conda \
    && bash Miniconda3-latest-Linux-x86_64.sh -b \
    && rm -f Miniconda3-latest-Linux-x86_64.sh 
RUN conda --version
RUN conda update -n base -c defaults conda
RUN conda create --name Thesis

RUN git clone https://github.com/HadiSDev/Thesis-ObjectCollector.git
RUN conda init bash && conda activate Thesis && conda install pytorch torchvision torchaudio cudatoolkit=11.3 -c pytorch && pip install mlagents


