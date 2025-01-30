# Muscle Locations and Biofeedback Modalities for EMG Interaction

We investigated which muscles and sensory modalities can provide optimal real-time interaction with muscle-based input in Virtual Reality. 

We explored optimal sensor placement and compared the input from five different muscle locations temple, (biceps, triceps, forearm, calf) to the input of the VR controller in a user study (N=18). results showed that EMG input offers flexibility in sensor placement at muscle locations.

We probed auditory, tactile, and visual feedback modalities as biofeedback for EMG interaction in a user study (N=40).   
Objective and subjective results indicate that input performance can be improved by presenting muscle tension as simultaneous tactile and visual feedback.

Unity Project and Arduino code for EMG multi-modal biofeedback in a Virtual Reality adapted interactive experiment based on a Fitts’ law target selection task.
The project is intended to be used with a HTC Vive Pro Eye Head mounted display. It is developed for Unity 2019.4.1f and integrates the signal processing from biosignals from the biosignalplux EMG Sensor with the 4-Channel Hub from biosignalsplux.

To motivate further investigations on the base of this research we provide the dataset including the Unity project, the log files from the studies, and the scripts we used for the evaluation. 

You find the evaluation data for the muscle location study here: https://www.dropbox.com/home/Evaluation%20EMG%20Muscle%20Location

Here you can find it for the biofeedback modality study: https://www.dropbox.com/home/Evaluation%20EMG%20Biofeedback%20Modality

A detailed description of the study procedure and hardware integration can be found in the related dissertation (*doctoral thesis reference here, coming soon*).

Information on Fitts Law calculation: https://www.yorku.ca/mack/FittsLawSoftware/doc/Throughput.html 

Inclusion of scripts:
- MainLogic.cs - as Component of "Controller"
- FeedbackManager.cs - as Component of "Controller"
- PluxUnityInterface.cs - as Component of "EMG_Panel"


![musclesEMG](https://github.com/user-attachments/assets/f7145b49-604a-4fa5-bfb2-121fd240810a)

![modalitiesEMG](https://github.com/user-attachments/assets/e6bdddb4-69be-48ef-9bc7-5debe78d7228)


```@inproceedings{SehrtCHI2023,
author = {Sehrt, Jessica and Wißmann, Tim and Breitenbach, Jan and Schwind, Valentin},
title = {The Effects of Body Location and Biosignal Feedback Modality on Performance and Workload Using Electromyography in Virtual Reality},
year = {2023},
isbn = {978-1-4503-9421-5},
publisher = {Association for Computing Machinery},
address = {New York, NY, USA},
url = {https://doi.org/10.1145/3544548.3580738},
doi = {10.1145/3544548.3580738},
keywords = {Electromyography, Physiological Sensing, Virtual Reality, Biofeedback, Accessibility},
location = {Hamburg, Germany},
series = {CHI '23}
}
