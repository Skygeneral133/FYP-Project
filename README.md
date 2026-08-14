# Robotic Operation with Machine Learning Enabled Mixed Reality (FYP Project)

### Team Members:

* **Jun Hau Chang**
* **Wei Home Tan**
* **Mark Wen Ren Shim**
* **Tong Yang Chai**
## Project Overview

This project focuses on the development of an automated robotic pouring system for laboratory applications.

The system combines a robotic arm, load cell, video sensing, machine learning, and Unity-based mixed reality to investigate and simulate liquid pouring behaviour.

The project aims to provide a non-contact and automated method of transferring liquids between containers while collecting data about the pouring process.

### Project Scope

The project consists of the following main components:

1. **Robotic Arm Control**

   * Control of the robotic arm using python.
   * Develop robotic pouring motions and trajectories.

2. **Load Cell Data Collection**

   * Measure the weight of the receiving beaker during pouring.
   * Determine liquid delivery rate.
   * Estimate the volume of liquid transferred into the beaker.
   * Collect and process load cell data for machine learning.

3. **Video Data Collection**

   * Record the liquid behaviour during pouring.
   * Capture characteristics such as the liquid stream and splashing.
   * Analyse video data to extract features for machine learning.

4. **Machine Learning**

   * Analyse load cell and video data.
   * Investigate the relationship between robotic motion and pouring characteristics.
   * Identify pouring behaviour such as stable flow and splashing.
   * Explore prediction of pouring characteristics based on collected data.

5. **Mixed Reality Simulation**

   * Use Unity to visualise and simulate the pouring process.
   * Simulate potential fumes generated during chemical pouring.
   * Visualise the effects of different pouring conditions and pouring rates.
   * Provide an interactive representation of the robotic operation.

## Repository Structure
### Robot Arm
This directory contains the control code for the robotic arm in python

### Load Cell
This directory contains the code for load cell data acquisition and processing. Includes Arduino code for interfacing with the load cell and HX710 amplifier. 3D printed parts for the load cell setup are also included.

### Unity Stuff
This directory contains the Unity project for mixed reality simulation of the pouring process. It includes scripts, assets, and scenes for visualising the robotic operation and liquid behaviour.

## Project Goal

The final system aims to demonstrate how robotic manipulation, sensor data, machine learning, and mixed reality can be combined to understand and improve automated liquid pouring operations in laboratory environments.
