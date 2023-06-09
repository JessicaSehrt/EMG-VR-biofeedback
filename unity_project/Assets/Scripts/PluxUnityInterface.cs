#define AUTO_EMG_CONNECT //uncomment this to enable auto connecting to the emg device

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
 
namespace Assets.Scripts
{
    public class PluxUnityInterface : MonoBehaviour
    {
        // Declaration of public variables.
        // [Graphical User Interface Objects]
        public Dropdown DeviceDropdown;
        public Button StartButton;
        public Button StartBySrcButton;
        public Button StopButton;
        public Button ScanButton;
        public Button AboutButton;
        public Button ConnectButton;
        public Button ChangeChannel;
        public InputField SamplingRateInput;
        public InputField ResolutionInput;
        public Dropdown ResolutionDropdown;
        public Toggle CH1Toggle;
        public Toggle CH2Toggle;
        public Toggle CH3Toggle;
        public Toggle CH4Toggle;
        public Toggle CH5Toggle;
        public Toggle CH6Toggle;
        public Toggle CH7Toggle;
        public Toggle CH8Toggle;
        public Toggle BTHToggle;
        public Toggle BLEToggle;
        public GameObject SamplingRateInfoPanel;
        public GameObject SelectedChannelPanel;
        public GameObject ChannelSelectioInfoPanel;
        public GameObject ConnectInfoPanel;
        public GameObject ConnectEstablishPanel;
        public GameObject BluetoothInfoPanel;
        public GameObject BLESamplingRateInfoPanel;
        public GameObject BatteryIconUnknown;
        public GameObject BatteryIcon0;
        public GameObject BatteryIcon10;
        public GameObject BatteryIcon50;
        public GameObject BatteryIcon100;
        public GameObject PlotIcon;
        public GameObject AcquiringIcon;
        public GameObject GreenFlag;
        public GameObject RedFlag;
        public GameObject TransparencyLevel;
        public GameObject Hidingwall; // Wand, hinter der die UI versteckt wird
        public GameObject CalibrationObject;

        public delegate void NotifyEmgUpdate(double currentValue, double maxValue);
        public event NotifyEmgUpdate EmgUpdated;
        //public GameObject convertscript; //Objekt, an das das Konvertierungsskript gehängt wird
        public Text ConnectText;
        public Text BatteryLevel;
        public Text CurrentChannel;
        public RectTransform GraphContainer;
        [SerializeField] public Sprite DotSprite;

        public WindowGraph GraphZone;

        // [Delegate References]
        // Delegates (needed for callback purposes).
        public delegate bool FPtr(int nSeq, IntPtr dataIn, int dataInSize);

        // [Generic Variables]
        public PluxDeviceManager PluxDevManager;
        public List<List<int>> MultiThreadList = null;
        public List<int> MultiThreadSubList = null;
        public List<int> ActiveChannels;
        public List<string> ListDevices;
        public int tempInt;
        public int LastLenMultiThreadString = 0;
        public int GraphWindSize = -1;
        public bool FirstPlot = true;
        public List<string> ResolutionDropDownOptions = new List<string>() {"8", "16"};
        public int VisualizationChannel = -1;
        public int SamplingRate;
        public int WindowInMemorySize;
        public bool UpdatePlotFlag = false;
        public string SelectedDevice = "";
        // [Hands-Feet-Custom-Variables]
        public List<double> convertedvalues = null;
        public List<double> mv_values = null; // for calculation of millivolts from raw data
        public List<double> bp_filtered_values = null; // for bandpassfiltering the millivolts
        public List<double> TKEO_values = null; // for performing TKEO on filtered millivolts
        public double converted_value; // fuer umrechnungsfunktion
        public Text labelforconnection;
        public string eindeutigeridentifier;
        public bool calibration_active = false;
        public double smoothed_tkeo = 0;
        public double values_in_window = 0;
        public Queue<double> samples = new Queue<double>();
        public int averagingwindow = 10;
        public double movingaverage = 0;
        public double threshold_l = 0;
        public double threshold_h = 0;
        public double maxvalue = 0;
        //for bandpass-filter
        public int lowcut = 10;
        public int highcut = 300;
        public int order = 3;
        public int fs = 1000;
        //trial for trigger without gameobject
        private bool trigger;

        private int update_num = 0;
        private bool isBluetoothConnected = false;


        // Awake is called when the script instance is being loaded.
        void Awake()
        {
            // Find references to graphical objects.
            GraphContainer = transform.Find("WindowGraph/GraphContainer").GetComponent<RectTransform>();  // User interface zone where the acquired data will be plotted using the "WindowGraph.cs" script.
            eindeutigeridentifier = System.DateTime.Now.Ticks.ToString();
        }

        // Start is called before the first frame update
        void Start()
        {
            // Welcome Message, showing that the communication between C++ dll and Unity was established correctly.
            Debug.Log("Connection between C++ Interface and Unity established with success !\n");
            PluxDevManager = new PluxDeviceManager(ScanResults, ConnectionDone);
            int welcomeNumber = PluxDevManager.WelcomeFunctionUnity();
            Debug.Log("Welcome Number: " + welcomeNumber);
            
#if AUTO_EMG_CONNECT
            //Auto-Scan on startup
            ScanButtonFunction();
#endif

            // Initialization of Variables.      
            MultiThreadList = new List<List<int>>();
            ActiveChannels = new List<int>();

            // Initialization of graphical zone.

            WindowGraph.IGraphVisual graphVisual = new WindowGraph.LineGraphVisual(GraphContainer, DotSprite, new Color(0, 158, 227, 0), new Color(0, 158, 227));
            GraphContainer = graphVisual.GetGraphContainer();
            GraphZone = new WindowGraph(GraphContainer, graphVisual);
            try
            {
                GraphZone.ShowGraph(new List<int>() { 0 }, graphVisual, -1, (int _i) => "" + (_i), (float _f) => Mathf.RoundToInt(_f) + "k");
                isBluetoothConnected = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                isBluetoothConnected = false;
            }

            // Create a timer that controls the update of real-time plot.
            System.Timers.Timer waitForPlotTimer = new System.Timers.Timer();
            waitForPlotTimer.Elapsed += new ElapsedEventHandler(OnWaitingTimeEnds);
            // Update timing of plot and all progress bars
            waitForPlotTimer.Interval = 100; //in milliseconds
            waitForPlotTimer.Enabled = true;
            waitForPlotTimer.AutoReset = true;

            ConnectEstablishPanel.SetActive(false);
        }

        // Update function, being constantly invoked by Unity.
        void Update()
        {
            //OnEmgUpdate(new System.Random().Next(0,100), 100); // Uncomment for testing purposes without emg device
            try
            {
                // Get packages of data.
                int[] pacakgeOfData;
                if (isBluetoothConnected)
                {
                    pacakgeOfData = PluxDevManager.GetPackageOfData(VisualizationChannel, ActiveChannels, UpdatePlotFlag);
                }
                else
                {
                    pacakgeOfData = null;
                }

                // Check if there it was communicated an event/error code.
                if (pacakgeOfData != null)
                {
                    if (pacakgeOfData.Length != 0)
                    {
                        // Creation of the first graphical representation of the results.
                        if (MultiThreadList[VisualizationChannel].Count >= 0)
                        {
                            if (FirstPlot)
                            {
                                // Update flag (after this step we won't enter again on this statement).
                                FirstPlot = false;

                                // Plot first set of data.
                                // Subsampling if sampling rate is bigger than 100 Hz.
                                List<int> subSamplingList = GetSubSampleList(new int[GraphWindSize], SamplingRate, GraphWindSize);
                                GraphZone.ShowGraph(subSamplingList, null, -1, _i => "-" + (GraphWindSize - _i),
                                    _f => Mathf.RoundToInt(_f / 1000) + "k");
                            }
                            // Update plot.
                            else if (FirstPlot == false)
                            {
                                // This if clause ensures that the real-time plot will only be updated every 1 second (Memory Restrictions).
                                if (UpdatePlotFlag && pacakgeOfData != null)
                                {
                                    // Get the values linked with the last 10 seconds of information.
                                    MultiThreadSubList = GetSubSampleList(pacakgeOfData, SamplingRate, GraphWindSize);
                                    GraphZone.UpdateValue(MultiThreadSubList);

                                    convertedvalues = convert_to_mv_TKEO(MultiThreadSubList);
                                    
                                    if (calibration_active)
                                    {
                                        // definition of trigger threshold
                                        Debug.Log("Kalibrierung ist aktiv");
                                        if (convertedvalues.Average() > maxvalue)
                                        {
                                            maxvalue = convertedvalues.Average();
                                            Debug.Log("Neuer Maxvalue: " + maxvalue);
                                        }

                                        threshold_h = maxvalue * 0.2;
                                        threshold_l = maxvalue * 0.1;
                                        Debug.Log("Min/Max Thresholds Maxvalue: " + threshold_l + " / " + threshold_h);
                                    }

                                    if (trigger) {
                                        trigger = convertedvalues.Average() > threshold_l;
                                    } else {
                                        trigger = convertedvalues.Average() > threshold_h;
                                    }
                                    //Debug.Log($"Current:{convertedvalues.Average()}, Low:{threshold_l}, High:{threshold_h}, Trigger:{trigger}");
                                    // notify with new emg value
                                    OnEmgUpdated(convertedvalues.Average(), maxvalue);

                                    // Reboot flag.
                                    UpdatePlotFlag = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Debug.Log("Exception in the Update method: " + exception.StackTrace);
                Console.WriteLine("Current Thread: " + Thread.CurrentThread.Name);
            }
            catch (ExternalException exc)
            {
                Debug.Log("ExternalException in the Update() callback:\n" + exc.Message + "\n" + exc.StackTrace);
                
                // Stop Acquisition in a secure way.
                StopButtonFunction(-1);
            }
            catch (Exception exc)
            {
                Debug.Log("Unidentified Exception inside Update() callback:\n" + exc.Message + "\n" + exc.StackTrace);
            }

            if (Input.GetKeyDown("c"))
            {
                //ConnectButtonFunction(false); //verbindet mit Hardware, die über Suche gefunden und in GUI ausgewählt wurde
                //PluxDevManager.PluxDev("00:07:80:0F:30:28"); //verbindet direkt mit unserer Hardware
            }

            // Ein- und Ausblenden der "Wand", hinter der das UI-Panel versteckt wird
            if (Input.GetKeyDown("h"))
            {
                if (Hidingwall.activeInHierarchy)
                {
                    Hidingwall.SetActive(false);
                    Debug.Log("die Mauer muss weg!");
                }
                else if (Hidingwall.activeInHierarchy == false)
                {
                    Hidingwall.SetActive(true);
                    Debug.Log("die Mauer muss wieder her!");

                }
            }
            
            // Kalibrierung der EMG-Thresholds ein/aus
            if (Input.GetKeyDown("k"))
            {
                if (calibration_active)
                {
                    calibration_active = false;
                    Debug.Log("Kalibrierung beendet");
                    if (!(CalibrationObject is null))
                    {
                        CalibrationObject.GetComponentInChildren<TMP_Text>().text = "";
                    }
                }
                else
                {
                    maxvalue = 0;
                    threshold_l = 0;
                    calibration_active = true;
                    Debug.Log("Kalibrierung gestartet");
                    if (!(CalibrationObject is null))
                    {
                        CalibrationObject.GetComponentInChildren<TMP_Text>().text =
                            "Calibration in progress!\n" +
                            "Please tense your muscle with effort...\n" +
                            "\n" +
                            "Kalibrierung gestartet!\n" +
                            "Bitte spannen Sie Ihren Muskel mit Mühe an...\n";
                    }
                }
            }
        }

        // Method invoked when the application was closed.
        void OnApplicationQuit()
        {
            // Disconnect from device.
            PluxDevManager.DisconnectPluxDev();
            Debug.Log("Application ending after " + Time.time + " seconds");
        }

        // ===========================================================================================================================================
        // ================================ Functions invoked when after a specific interaction with GUI elements ====================================
        // ===========================================================================================================================================
        // Function invoked during the onClick event of "ScanButton".
        public void ScanButtonFunction()
        {
            try
            {
                // List of available Devices.
                List<string> listOfDomains = new List<string>();
                if (BTHToggle.isOn)
                {
                    listOfDomains.Add("BTH");
                }
                if (BLEToggle.isOn)
                {
                    listOfDomains.Add("BLE");
                }

                PluxDevManager.GetDetectableDevicesUnity(listOfDomains);

                // Disable scan button.
                ScanButton.interactable = false;
            }
            catch (Exception e)
            {
                // Show info message.
                BluetoothInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, BluetoothInfoPanel));

                // Disable Drop-down.
                DeviceDropdown.interactable = false;
            }
        }

        // Function invoked during the onClick event of "ConnectButton".
        public void ConnectButtonFunction(bool typeOfStop)
        {
            try
            {
                // Change the color and text of "Connect" button.
                if (ConnectText.text == "Connect")
                {
                    ConnectEstablishPanel.SetActive(true);

                    // Get the selected device.
                    this.SelectedDevice = this.ListDevices[DeviceDropdown.value];

                    // Connection with the device.
                    Debug.Log("Trying to establish a connection with device " + this.SelectedDevice);
                    Console.WriteLine("Selected Device: " + this.SelectedDevice);
                    PluxDevManager.PluxDev(this.SelectedDevice);
                }
                else if (ConnectText.text == "Disconnect")
                {

                    try
                    {
                        // Disconnect device.
                        PluxDevManager.DisconnectPluxDev();
                    }
                    catch (Exception exception)
                    {
                        Debug.Log("Trying to disconnect from an unconnected device...");
                    }

                    ConnectText.text = "Connect";
                    GreenFlag.SetActive(false);
                    RedFlag.SetActive(true);

                    // Disable "Device Configuration" panel options.
                    SamplingRateInput.interactable = false;
                    SamplingRateInput.text = "1000";
                    ResolutionInput.interactable = false;
                    ResolutionDropdown.interactable = false;

                    // Disable channel selection buttons.
                    CH1Toggle.interactable = false;
                    CH1Toggle.isOn = true;
                    // Change the toggle state.
                    List<Toggle> toggleList = new List<Toggle>(){ CH2Toggle, CH3Toggle, CH4Toggle, CH5Toggle, CH6Toggle, CH7Toggle, CH8Toggle};
                    foreach (Toggle toggleBtn in toggleList)
                    {
                        toggleBtn.interactable = false;
                        toggleBtn.isOn = false;
                    }

                    // Disable Start and Device Configuration buttons.
                    StartButton.interactable = false;
                    StartBySrcButton.interactable = false;

                    // Disable the battery icons.
                    List<GameObject> ListBatteryIcons = new List<GameObject>() { BatteryIcon0, BatteryIcon10, BatteryIcon50, BatteryIcon100 };
                    foreach (var batImg in ListBatteryIcons)
                    {
                        batImg.SetActive(false);
                    }
                    BatteryIconUnknown.SetActive(true);

                    // Show the quantitative battery value.
                    BatteryLevel.text = "";

                    // Disable Drop-down options.
                    DeviceDropdown.ClearOptions();

                    //Add the options created in the List above
                    DeviceDropdown.AddOptions(new List<string>(){"Select Device"});

                    // Disable drop-down and Connect button if a PLUX Device was disconnected.
                    DeviceDropdown.interactable = false;
                    ConnectButton.interactable = false;

                    // Show PlotIcon.
                    PlotIcon.SetActive(true);
                    TransparencyLevel.SetActive(true);

                    // Reboot of global variables.
                    RebootVariables();
                }
            }
            catch(Exception e)
            {
                // Print information about the exception.
                Debug.Log(e);

                // Show info message.
                ConnectInfoPanel.SetActive(true);
                ConnectEstablishPanel.SetActive(false);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));
            }
        }

        // Function invoked during the onClick event of "StartButton".
        public void StartButtonFunction()
        {
            try
            {
                // Get Device Configuration input values.
                SamplingRate = Int32.Parse(SamplingRateInput.text);
                //int resolution = Int32.Parse(ResolutionDropDownOptions[ResolutionDropdown.value]);
                int resolution = 16;

                // Update graphical window size variable (the plotting zone should contain 10 seconds of data).
                GraphWindSize = SamplingRate * 10;
                WindowInMemorySize = Convert.ToInt32(1.1 * GraphWindSize);

                // Number of Active Channels.
                int nbrChannels = 0;
                Toggle[] toggleArray = new Toggle[]
                    {CH1Toggle, CH2Toggle, CH3Toggle, CH4Toggle, CH5Toggle, CH6Toggle, CH7Toggle, CH8Toggle};
                MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
                for (int i = 0; i < toggleArray.Length; i++)
                {
                    if (toggleArray[i].isOn == true)
                    {
                        // Preparation of a string that will be communicated to our .dll
                        // This string will be formed by "1" or "0" characters, identifying sequentially which channels are active or not.
                        ActiveChannels.Add(i + 1);

                        // Definition of the first active channel.
                        if (VisualizationChannel == -1)
                        {
                            VisualizationChannel = i + 1;

                            // Update the label with the Current Channel Number.
                            CurrentChannel.text = "CH" + VisualizationChannel;
                        }

                        nbrChannels++;
                    }

                    // Dictionary that stores all the data received from .dll API.
                    MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
                }

                // Check if at least one channel is active.
                if (ActiveChannels.Count != 0)
                {
                    // Start of Acquisition.
                    //Thread.CurrentThread.Name = "MAIN_THREAD";
                    if (PluxDevManager.GetDeviceTypeUnity() != "MuscleBAN BE Plux")
                    {
                        PluxDevManager.StartAcquisitionUnity(SamplingRate, ActiveChannels, resolution);
                    }
                    else
                    {
                        // Definition of the frequency divisor (subsampling ratio).
                        int freqDivisor = 10;
                        PluxDevManager.StartAcquisitionMuscleBanUnity(SamplingRate, ActiveChannels, resolution,
                            freqDivisor);
                    }

                    // Enable StopButton.
                    StopButton.interactable = true;

                    // Disable ConnectButton.
                    ConnectButton.interactable = false;

                    // Disable Start Button.
                    StartButton.interactable = false;
                    StartBySrcButton.interactable = false;

                    // Hide PlotIcon and show AcquiringIcon.
                    PlotIcon.SetActive(false);
                    TransparencyLevel.SetActive(false);
                    //AcquiringIcon.SetActive(true);

                    // Hide panel with the "Change Channel" button.
                    SelectedChannelPanel.SetActive(true);
                    if (ActiveChannels.Count == 1)
                    {
                        ChangeChannel.interactable = false;
                    }
                    else
                    {
                        ChangeChannel.interactable = true;
                    }

                    // Disable About Button to avoid entering a new scene during the acquisition.
                    AboutButton.interactable = false;
                }
                else
                {
                    // Show Info Message.
                    ChannelSelectioInfoPanel.SetActive(true);

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, ChannelSelectioInfoPanel));
                }
            }
            catch (Exception exc)
            {
                // Exception info.
                Debug.Log("Exception: " + exc.Message + "\n" + exc.StackTrace);

                // Show info message.
                ConnectInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));
                   
                // Reboot interface.
                ConnectButtonFunction(true);
            }

        }

        // Function invoked during the onClick event of "StartBySrcButton".
        public void StartBySrcButtonFunction()
        {
            try
            {
                // Get Device Configuration input values.
                SamplingRate = Int32.Parse(SamplingRateInput.text);
                int resolution = Int32.Parse(ResolutionDropDownOptions[ResolutionDropdown.value]);

                // Update graphical window size variable (the plotting zone should contain 10 seconds of data).
                GraphWindSize = SamplingRate * 10;
                WindowInMemorySize = Convert.ToInt32(1.1 * GraphWindSize);

                // Number of Active Channels.
                int nbrChannels = 0;
                Toggle[] toggleArray = new Toggle[]
                    {CH1Toggle, CH2Toggle, CH3Toggle, CH4Toggle, CH5Toggle, CH6Toggle, CH7Toggle, CH8Toggle};
                MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
                List<PluxDeviceManager.PluxSource> pluxSources = new List<PluxDeviceManager.PluxSource>();
                for (int i = 0; i < toggleArray.Length; i++)
                {
                    if (toggleArray[i].isOn == true || PluxDevManager.GetProductIdUnity() == 542)
                    {
                        // Preparation of a string that will be communicated to our .dll
                        // This string will be formed by "1" or "0" characters, identifying sequentially which channels are active or not.
                        ActiveChannels.Add(i + 1);

                        // Definition of the first active channel.
                        if (VisualizationChannel == -1)
                        {
                            VisualizationChannel = i + 1;

                            // Update the label with the Current Channel Number.
                            CurrentChannel.text = "CH" + VisualizationChannel;
                        }

                        nbrChannels++;

                        // Add a new Plux::Source.
                        if (PluxDevManager.GetProductIdUnity() != 542) // Clause applicable only for non fNIRS Explorer systems.
                        {
                            pluxSources.Add(new PluxDeviceManager.PluxSource(i + 1, 1, resolution, 0x01));
                        }
                        else if (PluxDevManager.GetProductIdUnity() == 542 && i == toggleArray.Length - 1) // Configuring sources if we are dealing with a fNIRS Explorer system.
                        {
                            // >>> fNIRS Explorer [Full set of SpO2 and Accelerometer channels]
                            // [SpO2 Port (4 channels - 0x0F > 0000 1111)]
                            pluxSources.Add(new PluxDeviceManager.PluxSource(9, 1, resolution, 0x0F));

                            // [ACC Port (3 channels - 0x07 > 0000 0111)]
                            pluxSources.Add(new PluxDeviceManager.PluxSource(11, 1, resolution, 0x07));
                        }
                    }

                    // Dictionary that stores all the data received from .dll API.
                    MultiThreadList.Add(new List<int>(Enumerable.Repeat(0, GraphWindSize).ToList()));
                }


                // Check if at least one channel is active.
                if (ActiveChannels.Count != 0)
                {
                    // Start of Acquisition.
                    PluxDevManager.StartAcquisitionBySourcesUnity(SamplingRate, pluxSources.ToArray());

                    // Enable StopButton.
                    StopButton.interactable = true;

                    // Disable ConnectButton.
                    ConnectButton.interactable = false;

                    // Disable Start Button.
                    StartBySrcButton.interactable = false;
                    StartButton.interactable = false;

                    // Hide PlotIcon and show AcquiringIcon.
                    PlotIcon.SetActive(false);
                    TransparencyLevel.SetActive(false);
                    //AcquiringIcon.SetActive(true);

                    // Hide panel with the "Change Channel" button.
                    SelectedChannelPanel.SetActive(true);
                    if (ActiveChannels.Count == 1)
                    {
                        ChangeChannel.interactable = false;
                    }
                    else
                    {
                        ChangeChannel.interactable = true;
                    }

                    // Disable About Button to avoid entering a new scene during the acquisition.
                    AboutButton.interactable = false;
                }
                else
                {
                    // Show Info Message.
                    ChannelSelectioInfoPanel.SetActive(true);

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, ChannelSelectioInfoPanel));
                }
            }
            catch (Exception exc)
            {
                // Exception info.
                Debug.Log("Exception: " + exc.Message + "\n" + exc.StackTrace);

                // Show info message.
                ConnectInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));

                // Reboot interface.
                ConnectButtonFunction(true);
            }

        }

        // Function invoked during the onClick event of "StopButton".
        public void StopButtonFunction(int forceStop=0)
        {
            // Invoke stop function from PluxDeviceManager.
            bool typeOfStop;

            // Check how many samples were communicated by the device.
            typeOfStop = PluxDevManager.StopAcquisitionUnity(forceStop);

            // Enable About Button.
            AboutButton.interactable = true;

            // Enable ConnectButton.
            if (StopButton.interactable == true)
            {
                ConnectButton.interactable = true;
            }

            // Enable ScanButton.
            ScanButton.interactable = true;

            // Disable StopButton.
            StopButton.interactable = false;

            // Disconnect device if a forced stop occurred.
            if (ConnectText.text == "Disconnect")
            {
                ConnectButtonFunction(typeOfStop);
            }

            // Show a warning message if something wrong happened.
            if (typeOfStop == true || forceStop == -1)
            {
                // Show info message.
                ConnectInfoPanel.SetActive(true);

                // Present a message stating the communication error and hide it after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, ConnectInfoPanel));
            }

            // Hide info message.
            BLESamplingRateInfoPanel.SetActive(false);
        }

        // Function invoked during the onClick event of the About Button.
        public void AboutButtonFunction()
        {
            // Load About Scene.
            SceneManager.LoadScene("AboutWindow");

            // Reboot variables.
            if (ConnectText.text == "Disconnect")
            {
                ConnectButton.onClick.Invoke();
            }
            RebootVariables();
        }

        // Function invoked during the onValueChanged event of the Sampling Rate Input.
        public void SamplingRateOnChangeFunction()
        {
            // Check if "-" symbol is being introduced.
            if (SamplingRateInput.text != "")
            {
                if (SamplingRateInput.text.Contains("-") || SamplingRateInput.text.Length > 4 || Int32.Parse(SamplingRateInput.text) > 4000)
                {
                    // Show info message.
                    SamplingRateInfoPanel.SetActive(true);

                    // Place the standard Sampling rate.
                    SamplingRateInput.text = "1000";

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, SamplingRateInfoPanel));
                }
                // Check if we are dealing with BLE device. If so, when more than one channel is active the maximum sampling rate will be 100 Hz.
                else if (Int32.Parse(SamplingRateInput.text) > 100 && this.SelectedDevice.Contains("BLE") && GetNbrActiveToggle() > 1)
                {
                    // Force sampling rate to acquire the maximum value.
                    SamplingRate = 100;
                    SamplingRateInput.text = "100";

                    // Present info message.
                    BLESamplingRateInfoPanel.SetActive(true);

                    // Hide object after 5 seconds.
                    StartCoroutine(RemoveAfterSeconds(5, BLESamplingRateInfoPanel));
                }
            }
        }

        // Function invoked during the onValueChanged event of the Toggle Button Inputs.
        public void ToogleButtonOnChangeFunction()
        {
            if (Int32.Parse(SamplingRateInput.text) > 100 && this.SelectedDevice.Contains("BLE") && GetNbrActiveToggle() > 1)
            {
                // Force sampling rate to acquire the maximum value.
                SamplingRate = 100;
                SamplingRateInput.text = "100";

                // Present info message.
                BLESamplingRateInfoPanel.SetActive(true);

                // Hide object after 5 seconds.
                StartCoroutine(RemoveAfterSeconds(5, BLESamplingRateInfoPanel));
            }
        }

        // Function invoked during the onValueChanged event of the Bluetooth Toggle Button Inputs.
        public void BTToogleButtonOnChangeFunction(int btNbr)
        {
            Toggle[] toggleArray = new[] {BTHToggle, BLEToggle};
            if (!BTHToggle.isOn && !BLEToggle.isOn)
            {
                // Ignore the change command and keep the button active.
                toggleArray[btNbr].isOn = !toggleArray[btNbr].isOn;
            }
        }

        // Function invoked during the onValueChanged event of the Sampling Rate Input.
        public void SamplingRateOnEndEditFunction()
        {
            if (SamplingRateInput.text == "")
            {
                SamplingRateInput.text = "1000";
            }
        }

        // Function invoked during the onClick event of the "Change Channel" button.
        public void ChangeChannelClickFunction()
        {
            int indexOfVisualizationChn = ActiveChannels.IndexOf(VisualizationChannel);
            if (indexOfVisualizationChn == ActiveChannels.Count - 1)
            {
                VisualizationChannel = ActiveChannels[0];
            }
            else
            {
                VisualizationChannel = ActiveChannels[indexOfVisualizationChn + 1];
            }

            // Update the label with the Current Channel Number.
            CurrentChannel.text = "CH" + VisualizationChannel;
        }

        // Callback that receives the list of PLUX devices found during the Bluetooth scan.
        public void ScanResults(List<string> listDevices)
        {
            // Store list of devices in a global variable.
            this.ListDevices = listDevices;

            // Info message for development purposes.
            Console.WriteLine("Number of Detected Devices: " + this.ListDevices.Count);
            for (int i = 0; i < this.ListDevices.Count; i++)
            {
                Console.WriteLine("Device--> " + this.ListDevices[i]);
            }

            // Enable Dropdown if the list of devices is not empty.
            if (this.ListDevices.Count != 0)
            {
                // Add the new options to the drop-down box included in our GUI.
                //Create a List of new Dropdown options
                List<string> dropDevices = new List<string>();

                // Convert array to list format.
                dropDevices.AddRange(this.ListDevices);

                // A check into the list of devices.
                dropDevices = dropDevices.GetRange(0, dropDevices.Count);
                for (int i = dropDevices.Count - 1; i >= 0; i--)
                {
                    // Accept only strings containing "BTH" or "BLE" substrings "flagging" a PLUX Bluetooth device.
                    if (!dropDevices[i].Contains("BTH") && !dropDevices[i].Contains("BLE"))
                    {
                        dropDevices.RemoveAt(i);
                    }
                }

                // Raise an exception if none device was detected.
                if (dropDevices.Count == 0)
                {
                    throw new ArgumentException();
                }

                //Clear the old options of the Dropdown menu
                DeviceDropdown.ClearOptions();

                //Add the options created in the List above
                DeviceDropdown.AddOptions(dropDevices);

                // Enable drop-down and Connect button if a PLUX Device was detected .
                DeviceDropdown.interactable = true;
                ConnectButton.interactable = true;

                // Hide info message.
                ConnectInfoPanel.SetActive(false);

#if AUTO_EMG_CONNECT
                // Auto-Connect after scanning is done
                ConnectButtonFunction(false);
#endif
            }

            // Enable scan button.
            ScanButton.interactable = true;
        }

        // Callback invoked once the connection with a PLUX device was established.
        public void ConnectionDone()
        {
            // Change the color and text of "Connect" button.
            if (ConnectText.text == "Connect")
            {
                Debug.Log("Connection with device " + this.SelectedDevice + " established with success!");

                ConnectText.text = "Disconnect";
                GreenFlag.SetActive(true);
                RedFlag.SetActive(false);

                // Enable "Device Configuration" panel options.
                SamplingRateInput.interactable = true;
                ResolutionInput.interactable = true;
                ResolutionDropdown.interactable = true;

                // Enable channel selection buttons accordingly to the type of device.
                string devType = PluxDevManager.GetDeviceTypeUnity();
                Debug.Log("Product ID: " + PluxDevManager.GetProductIdUnity());
                if (devType == "MuscleBAN BE Plux")
                {
                    CH1Toggle.interactable = true;

                    //Clear the old options of the Dropdown menu
                    ResolutionDropdown.ClearOptions();

                    //Add the options created in the List above
                    ResolutionDropdown.AddOptions(new List<string>() { "8", "16" });
                }
                else if (devType == "BITalino")
                {
                    CH1Toggle.interactable = true;
                    CH2Toggle.interactable = true;
                    CH3Toggle.interactable = true;
                    CH4Toggle.interactable = true;
                    CH5Toggle.interactable = true;
                    CH6Toggle.interactable = true;

                    //Clear the old options of the Dropdown menu
                    ResolutionDropdown.ClearOptions();

                    //Add the options created in the List above
                    ResolutionDropdown.AddOptions(new List<string>() { "10" });
                }
                else if (devType == "biosignalsplux" || devType == "BioPlux")
                {
                    CH1Toggle.interactable = true;
                    CH2Toggle.interactable = true;
                    CH3Toggle.interactable = true;
                    CH4Toggle.interactable = true;
                    CH5Toggle.interactable = true;
                    CH6Toggle.interactable = true;
                    CH7Toggle.interactable = true;
                    CH8Toggle.interactable = true;

                    //Clear the old options of the Dropdown menu
                    ResolutionDropdown.ClearOptions();

                    //Add the options created in the List above
                    if (devType == "biosignalsplux")
                    {
                        ResolutionDropdown.AddOptions(new List<string>() { "8", "16" });
                        ResolutionDropDownOptions = new List<string>() { "8", "16" };
                    }
                    else
                    {
                        ResolutionDropdown.AddOptions(new List<string>() { "8", "12" });
                        ResolutionDropDownOptions = new List<string>() { "8", "12" };
                    }
                }
                else if (devType == "OpenBANPlux")
                {
                    CH1Toggle.interactable = true;
                    CH2Toggle.interactable = true;

                    //Clear the old options of the Dropdown menu
                    ResolutionDropdown.ClearOptions();

                    //Add the options created in the List above
                    ResolutionDropdown.AddOptions(new List<string>() { "8", "16" });
                }
                else if (PluxDevManager.GetProductIdUnity() != 542) // If the device is not a fNIRS Explorer then we are dealing with a not supported system.
                {
                    throw new NotSupportedException();
                }

                // Enable Start and Device Configuration buttons.
                StartButton.interactable = true;
                
                // Disable "Start by Sources" button if the device is a BITalino.
                if (PluxDevManager.GetDeviceTypeUnity() != "BITalino")
                {
                    StartBySrcButton.interactable = true;
                }

                // Disable Connect Button.
                //ConnectButton.interactable = false;

                // Hide show Info message if it is active.
                ConnectInfoPanel.SetActive(false);

                // Update Battery Level.
                int batteryLevel = -1;
                if (devType != "BioPlux")
                {
                    batteryLevel = PluxDevManager.GetBatteryUnity();
                }

                // Battery icon accordingly to the battery level.
                List<GameObject> ListBatteryIcons = new List<GameObject>() { BatteryIcon0, BatteryIcon10, BatteryIcon50, BatteryIcon100, BatteryIconUnknown };
                GameObject currImage;
                if (batteryLevel > 50)
                {
                    BatteryIcon100.SetActive(true);
                    currImage = BatteryIcon100;
                }
                else if (batteryLevel <= 50 && batteryLevel > 10)
                {
                    BatteryIcon50.SetActive(true);
                    currImage = BatteryIcon50;
                }
                else if (batteryLevel <= 10 && batteryLevel > 1)
                {
                    BatteryIcon10.SetActive(true);
                    currImage = BatteryIcon10;
                }
                else if (batteryLevel == 0)
                {
                    BatteryIcon0.SetActive(true);
                    currImage = BatteryIcon0;
                }
                else
                {
                    BatteryIconUnknown.SetActive(true);
                    currImage = BatteryIconUnknown;
                }

                // Disable the remaining images.
                foreach (var batImg in ListBatteryIcons)
                {
                    if (batImg != currImage)
                    {
                        batImg.SetActive(false);
                    }
                }

                // Show the quantitative battery value.
                if (batteryLevel != -1)
                {
                    BatteryLevel.text = batteryLevel.ToString() + "%";
                }
                else
                {
                    BatteryLevel.text = "N.A.";
                }

#if AUTO_EMG_CONNECT
                // Auto-Start on connect
                ConnectEstablishPanel.SetActive(false);
                StartButtonFunction();
#endif
            }
        }

        // Coroutine used to fade out info messages after x seconds.
        IEnumerator RemoveAfterSeconds(int seconds, GameObject obj)
        {
            yield return new WaitForSeconds(seconds);
            obj.SetActive(false);
        }

        // Function used to subsample acquired data.
        public List<int> GetSubSampleList(int[] originalArray, int samplingRate, int graphWindowSize)
        {
            // Subsampling if sampling rate is bigger than 100 Hz.
            List<int> subSamplingList = new List<int>();
            int subSamplingLevel = 1;
            if (samplingRate > 100)
            {
                // Subsampling Level.
                subSamplingLevel = samplingRate / 100;
                for (int i = 0; i < originalArray.Length; i++)
                {
                    if (i % subSamplingLevel == 0)
                    {
                        subSamplingList.Add(originalArray[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < originalArray.Length; i++)
                {
                    subSamplingList.Add(originalArray[i]);
                }
            }

            return subSamplingList;
        }

        public void RebootVariables()
        {
            MultiThreadList = new List<List<int>>();
            ActiveChannels = new List<int>();
            MultiThreadSubList = null;
            LastLenMultiThreadString = 0;
            GraphWindSize = -1;
            VisualizationChannel = -1;
            UpdatePlotFlag = false;
        }

        public void OnWaitingTimeEnds(object source, ElapsedEventArgs e)
        {
            // Update flag, which will trigger the update of real-time plot.
            UpdatePlotFlag = true;
        }

        // Get the number of active toggle buttons.
        private int GetNbrActiveToggle()
        {
            // Number of Active Channels.
            int nbrChannels = 0;
            Toggle[] toggleArray = new Toggle[]{CH1Toggle, CH2Toggle, CH3Toggle, CH4Toggle, CH5Toggle, CH6Toggle, CH7Toggle, CH8Toggle};
            for (int i = 0; i < toggleArray.Length; i++)
            {
                if (toggleArray[i].isOn == true)
                {
                    nbrChannels++;
                }
            }

            return nbrChannels;
        }

        public List<double> convert_to_mv_TKEO(List<int> values)
        {
            mv_values.Clear();
            bp_filtered_values.Clear();
            TKEO_values.Clear();
            //conversion to millivolts according to https://plux.info/signal-processing/458-emg-sensor-unit-conversion.html
            foreach (int i in values)
            {
                converted_value = ((3*(float)i / 65536) - 1.5); // Faktoren für Gain und Vcc in Formel eingerechnet
                mv_values.Add(converted_value);
            }

            bp_filtered_values = mv_values;

            //TKEO conversion and absolute value according to https://plux.info/signal-processing/460-event-detection-muscular-activations-emg.html
            int lc = 0;
            foreach (double j in bp_filtered_values)
            {
                if (lc == 0 || lc == (bp_filtered_values.Count - 1))
                {
                    TKEO_values.Add(Math.Abs(bp_filtered_values[lc]));
                }
                else
                {
                    TKEO_values.Add(Math.Abs((bp_filtered_values[lc] * bp_filtered_values[lc]) - (bp_filtered_values[lc + 1]* bp_filtered_values[lc - 1])));
                }
                lc++;
                
            }

            return TKEO_values;
        }

        public bool isTriggering()
        {
            return trigger;
        }

        protected virtual void OnEmgUpdated(double currentValue, double maxValue)
        {
            EmgUpdated?.Invoke(currentValue, maxValue);
        }
    }
}
