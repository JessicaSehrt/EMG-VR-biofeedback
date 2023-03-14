//#define USE_FEEDBACK //uncomment this to enable the Feedback methods

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Assets.Scripts;

public class MainLogic : MonoBehaviour
{
    [Tooltip("The amount of seconds before starting every epoch.")]
    public ushort withinStudyDelay = 3;

    [Tooltip("The amount of seconds before starting the first epoch.")]
    public ushort preStudyDelay = 10;

    [Tooltip("The amount of seconds for a break in the middle of the experiment (for relaxation).")]
    public ushort midStudyDelay = 60;

    public short participantID;
    public InputField InputField_p;
    public InputField InputField_c;
    public GameObject[] target;
    public TextMeshProUGUI tmp_counter;
    public GameObject targetTafel;
    public GameObject m_Dot;
    public GameObject countdownSign;
    public GameObject questionnaireWall;
    
    public PluxUnityInterface emgPanelPluxUnityInterface;
    public FeedbackManager feedbackManager;

    private readonly long applicationStartTimestamp = DateTime.Now.Ticks;
    private FitsLawEpoch currentFitsLawEpoch;
    private StudyDesignManager studyDesignManager = new StudyDesignManager();
    private ConditionDescription[] currentConditionSet;
    private short currentFitsLawEpochCounter;
    private TMP_Text countdownSignText;
    private bool experimentCompleted = false;
    
    public float m_RaycastDefaultLength = 500.0f;

    // Start is called before the first frame update
    void Start()
    {
        countdownSignText = countdownSign.GetComponentInChildren<TMP_Text>();
        try
        {
            participantID = Convert.ToInt16(InputField_p.text);
            currentFitsLawEpochCounter = (short)(
                string.IsNullOrEmpty(InputField_c.text) ? 0 : Convert.ToInt16(InputField_c.text) - 1
            );
        }
        catch (System.FormatException)
        {
            Debug.LogError(
                $"Cannot parse Integer for either field participant ID or Condition. " +  
                $"Check the values and restart the game. Participant: \"{InputField_p.text}\", Condition: \"{InputField_c.text}\""
            );
            UnityEditor.EditorApplication.ExitPlaymode();
            throw;
        } 
        emgPanelPluxUnityInterface.EmgUpdated += OnEmgUpdated;
#if USE_FEEDBACK
            currentConditionSet = studyDesignManager.GetCurrentBalancedLatinSquare(participantID);
#else
            currentConditionSet = new[] {new ConditionDescription(false, false, false)};
#endif
        InitNewFitsLawEpoch(preStudyDelay);
    }

    // Update is called once per frame
    void Update()
    {        
        // move dot to the position of the raycast hit --> might exchange dot with hand
        RaycastHit hit = CreateRaycast(m_RaycastDefaultLength);
        if (hit.collider != null)
        {
            m_Dot.transform.position = hit.point;
        }
        
        if (experimentCompleted)
        {
            countdownSign.SetActive(true);
            countdownSignText.text = "Experiment completed.\n\nExperiment abgeschlossen.";
            return;
        }

        if (!currentFitsLawEpoch.IsFinished())
        {
            currentFitsLawEpoch.run(hit);
            return;
        }

        if (!questionnaireWall.activeSelf)
        {
            questionnaireWall.SetActive(true);
#if USE_FEEDBACK
            var currentCondition = currentConditionSet[currentFitsLawEpochCounter - 1];
            Debug.Log($"Fitt's Law epoch was #{currentFitsLawEpochCounter}. " +
                      "Feedbacks: " + (currentCondition.HasAuditive ? "Auditive " : "") +
                      (currentCondition.HasVisual ? "Visual " : "") +
                      (currentCondition.HasTactile ? "Tactile" : "")
            );
#endif
        }
        
        if (Input.GetKeyDown(KeyCode.Return))
        {
            questionnaireWall.SetActive(false);
            
            if (currentFitsLawEpochCounter >= currentConditionSet.Length)
            {
                experimentCompleted = true;
                return;
            }
            InitNewFitsLawEpoch(currentFitsLawEpochCounter == currentConditionSet.Length / 2 ? midStudyDelay : withinStudyDelay);
        }
    }
    
    private void InitNewFitsLawEpoch(int delayInSeconds)
    {
        StartCoroutine(nameof(StartCountdown), delayInSeconds);
        currentFitsLawEpoch = new FitsLawEpoch(emgPanelPluxUnityInterface, currentFitsLawEpochCounter,
            participantID, targetTafel, target, applicationStartTimestamp);
        
        currentFitsLawEpochCounter++;
        
        
#if USE_FEEDBACK
        var currentCondition = currentConditionSet[currentFitsLawEpochCounter - 1];
        Debug.Log($"Starting new Fitt's law epoch #{currentFitsLawEpochCounter}. " +
            "Feedbacks: " + (currentCondition.HasAuditive ? "Auditive " : "") +
            (currentCondition.HasVisual ? "Visual " : "") +
            (currentCondition.HasTactile ? "Tactile" : "")
        );
#else
        var currentCondition = currentConditionSet[0];
        Debug.Log($"Starting new fits law epoch with condition #{currentFitsLawEpochCounter}.");
#endif
        
        feedbackManager.NextEpoch(currentCondition);
    }

    private IEnumerator StartCountdown(int delayInSeconds)
    {
        countdownSign.SetActive(true);
#if USE_FEEDBACK
        var roundCounterText = $"Starting round {currentFitsLawEpochCounter + 1} / {currentConditionSet.Length}\n";
#else
        var roundCounterText = $"Starting condition {currentFitsLawEpochCounter + 1}\n";
#endif
        for (int i = delayInSeconds; i > 0; i--)
        {
            countdownSignText.text = roundCounterText + "\n" + i;
            yield return new WaitForSeconds(1);
        }
        countdownSign.SetActive(false);
    }

    private void OnEmgUpdated(double currentValue, double maxValue)
    {
        feedbackManager.UpdateFeedback(currentValue, maxValue);
    }
    
    private RaycastHit CreateRaycast(float length)
    {
        RaycastHit hit;
    
        var pos = Camera.main.gameObject.transform.position;
        var forward = Camera.main.gameObject.transform.forward;
        Ray ray = new Ray(pos, forward);
        Physics.Raycast(ray, out hit, m_RaycastDefaultLength);
    
        return hit;
    }
}
