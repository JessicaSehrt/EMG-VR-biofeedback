//#define TRIGGER_MODE_XR_GRAB_PINCH
//#define TRIGGER_MODE_DEBUG

using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

namespace Assets.Scripts
{
    public class FitsLawEpoch
    {
        bool status = true;

        double newtime, oldtime;
        private short participantID;
        private short condition;
        private int uniqueseed;
        private InputField InputField_p;
        private InputField InputField_c;
        private GameObject[] target;
        private GameObject CurrentTarget;
        private TextMeshProUGUI tmp_target1_localCoord;
        private bool nextround;
        private float Amplitude; //private setzen?
        private float Size; //private setzen?
        private float ID;
        private double Duration;
        private double TP_estimate;
        private float posTargetX = 0;
        private float posTargetY = 0;
        private float posHitX = 0;
        private float posHitY = 0;
        private float posOriginX = 0;
        private float posOriginY = 0;
        private float posOriginHitX = 0;

        private float posOriginHitY = 0;

        //public float Distance_b;
        //public float Distance_c;
        //attention! it seems the initial values given here are overwritten by the values defined within the unity scene at the Controller GameObject in the Main Logic Script properties
        private float[] AmplitudeArray = { 20, 18, 14, 14, 20, 14, 22 };
        private float[] SizeArray = { 11, 6, 3, 2, 2, 1, 1 };
        private float[] IDArray = { 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 4.5f };
        private int[] Order = { 0, 1, 2, 3, 4, 5, 6 };
        private float tx;
        private float ty;
        private float tz;
        private int currentTargetIndex = 0;
        private int oldTargetIndex = -1;
        private int targetsLeft = 9;
        private int round = 0;

        private Vector3 wallcenter;

        //for triggering with EMG
        public bool edge = false;
        private int buffer = 0;

        //for triggering with Controller
        public SteamVR_Action_Boolean grabPinchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");

        private readonly PluxUnityInterface pluxUnityInterface;

        private readonly long applicationStartTimestamp;

        public FitsLawEpoch(PluxUnityInterface pluxUnityInterface, short condition, short participantID,
            GameObject targetTafel, GameObject[] targets, long applicationStartTimestamp)
        {
            // Init values and variables
            this.pluxUnityInterface = pluxUnityInterface;
            this.condition = condition;
            this.participantID = participantID;
            this.applicationStartTimestamp = applicationStartTimestamp;
            nextround = true;
            wallcenter = targetTafel.transform.position;
            tz = (wallcenter.z + 0.05f);
            currentTargetIndex = 0;
            oldTargetIndex = -1; // muss zu Beginn ungleich currentTargetIndex sei
            targetsLeft = 9;
            oldtime = Time.time;
            newtime = Time.time;
            target = targets;
            
            // build 32bit seed from by concatenating the 16 bit condition to the 16 bit participant id
            uniqueseed = participantID;
            uniqueseed = (uniqueseed << 16) + condition;
            Debug.Log("Seed: " + uniqueseed);
            // shuffle the order
            Debug.Log("Order before shuffling: " + Order[0] + +Order[1] + Order[2] + Order[3] + Order[4] + Order[5] +
                      Order[6]);
            Order = Shuffle(Order, uniqueseed);
            Debug.Log("Order after shuffling: " + Order[0] + +Order[1] + Order[2] + Order[3] + Order[4] + Order[5] +
                      Order[6]);
        }


        public void run(RaycastHit hit)
        {
            //check if all targets of this round (= ID) have been hit
            if (targetsLeft == 0)
            {

                //make sure next round will be initialized
                nextround = true;
                currentTargetIndex = 0;
                
                //check if this was the last round
                if (round == 8)
                {
                    status = false;
                    nextround = false;
                    Debug.Log("FitsLaw epoch is already finished, cannot proceed. " +
                              "Please destroy the FitsLawEpoch object instance and create a new one.");
                    return;
                }
            }
            else if (currentTargetIndex != oldTargetIndex)
            {
                //makes sure next target is active -> might be changed so it's only called oncev instead of every frame update
                CurrentTarget = target[currentTargetIndex];
                CurrentTarget.SetActive(true);
            }

            if (nextround)
            {
                // Generate new Fitts circle 
                if (round == 0)
                {
                    Amplitude = 1.8f;
                    Size = 0.3f;
                    Debug.Log("Test round, Amplitude 1.8, Size 0.3");
                }
                else
                {
                    Debug.Log("<- next round triggered -> ");
                    //Order Array is just to shuffle the IDs from 0 to 6, round then takes the next ID every round, -1 because round 0 is just for practice and should not be taken from the array
                    //rounds 1-7 should be taken from (shuffled) array elements 0-6
                    Amplitude = (AmplitudeArray[Order[round - 1]]) / 10;
                    Debug.Log("New Amplitude: " + Amplitude);
                    Size = (SizeArray[Order[round - 1]]) / 10;
                    Debug.Log("New Size: " + Size);
                    ID = IDArray[Order[round - 1]];
                }

                //TODO: take next Size and Amplitude from table of IDs
                for (int i = 0; i < target.Length; i++)
                {
                    //target[i].transform.position = new Vector3(0,i,i);
                    //Debug.Log(Convert.ToDouble(i * 40));
                    //Debug.Log(Math.Sin(Convert.ToDouble(i * 40)));
                    // Debug.Log((Math.Sin(Convert.ToDouble(i * 40)) * Amplitude)/2);
                    // Debug.Log((wallcenter.x + (Math.Sin(Convert.ToDouble(i * 40)) * Amplitude) / 2));
                    //calculates the radiant value of 40 degrees times i (because a 360 degree circle is divided into i=9 fractions)               
                    double radiant = i * 40 * 0.01745329;
                    //sets the x and y position of target i
                    tx = (float)(wallcenter.x + (Math.Sin(radiant) * Amplitude) / 2);
                    ty = (float)(wallcenter.y + (Math.Cos(radiant) * Amplitude) / 2);
                    // Debug.Log(tx + "<- tx | ty -> " + ty);
                    target[i].transform.position = new Vector3(tx, ty, tz);
                    target[i].transform.localScale = new Vector3(Size, 0.001f, Size);
                    target[i].SetActive(false);
                }

                round++;
                nextround = false;
                target[0].SetActive(true);
                targetsLeft = 9;
            }

            // main fitts law logic
            if (status)
            {
                bool grabPinchState = false;
                try
                {
                    grabPinchState = grabPinchAction.state;
                }
                catch (NullReferenceException)
                {
                    // ignore, since SteamVR throws a NullReference exception for the action on first read
                    // if no device is connected.        
                }
                
                //Generate edge of the EMG-trigger
                if ((pluxUnityInterface.isTriggering() || grabPinchState) && buffer < 4)
                {
                    edge = true;
                    buffer++;
                }
                else if ((pluxUnityInterface.isTriggering() || grabPinchState))
                {
                    edge = false;
                }

                if (!pluxUnityInterface.isTriggering() && !grabPinchState)
                {
                    edge = false;
                    buffer = 0;
                }

                //change following condition to if(true) for debugging
                //to "grabPinchState" for controller trigger 
                //to "trigger.activeSelf" for simple emg trigger 
                //to "edge" for edge detection of emg trigger 
#if TRIGGER_MODE_DEBUG
                if (true)
#elif TRIGGER_MODE_XR_GRAB_PINCH
                if (grabPinchState)
#else
                if (edge)
#endif
                {

                    if (hit.collider != null)

                    {
                        // if active target is hit by raycast and triggered, get next target index
                        // and decrement counter (targetsLeft)
                        if (hit.collider.name == CurrentTarget.name)
                        {
                            oldtime = newtime;
                            newtime = Time.time;
                            Duration = Math.Round((newtime - oldtime), 3) * 1000;
                            //Debug.Log(newtime);
                            //if condition makes sure, first round (for practice) and first target in each round are not logged
                            if (targetsLeft < 9 && round > 1)
                            {
                                if (targetsLeft == 8 && round == 2)
                                {
                                    Debug.Log("Start fits law metrics logging.");
                                }
                                TP_estimate = (ID / Duration) * 1000;
                                posTargetX = target[currentTargetIndex].transform.position.x;
                                posTargetY = target[currentTargetIndex].transform.position.y;
                                posHitX = hit.point.x;
                                posHitY = hit.point.y;
                                posOriginX = target[oldTargetIndex].transform.position.x;
                                posOriginY = target[oldTargetIndex].transform.position.y;
                                AddLog();
                            }

                            CurrentTarget.SetActive(false);
                            oldTargetIndex = currentTargetIndex;

                            if (targetsLeft % 2 == 0)
                            {
                                currentTargetIndex -= 4;
                            }
                            else
                            {
                                currentTargetIndex += 5;
                            }

                            targetsLeft--;

                            //to calculate the origin-dx in R script, the current hit point is written to an additional variable to be logged at the next hit
                            posOriginHitX = hit.point.x;
                            posOriginHitY = hit.point.y;
                        }

                    }


                }
            }
        }

        public static T[] Shuffle<T>(T[] array, int seed)
        {
            UnityEngine.Random.InitState(seed);

            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                int r = i + (int)(UnityEngine.Random.value * (n - i));
                (array[r], array[i]) = (array[i], array[r]);
            }

            return array;
        }


        public void AddLog()
        {
            string path = Application.dataPath + "/_TaskResults/" + participantID + "_" + applicationStartTimestamp + "_Log_Task.csv";
            //time reduced by a large number to receive a smaller value
            File.AppendAllText(path,
                        DateTime.Now.Ticks + ";"
                        + Time.unscaledTime.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + participantID + ";"
                        + condition + ";"
                        + ID.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + Size.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + Amplitude.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + Duration.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + TP_estimate.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posTargetX.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posTargetY.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posHitX.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posHitY.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posOriginX.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posOriginY.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posOriginHitX.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        + posOriginHitY.ToString(NumberFormatInfo.InvariantInfo) + ";"
                        //+ Distance_b + ";"
                        //+ Distance_c + ";"
                        + "\n");
        }

        public bool IsFinished()
        {
            return targetsLeft == 0 && round == 8;
        }
    }
}