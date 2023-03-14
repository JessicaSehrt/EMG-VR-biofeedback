using System.Collections;
using System.Globalization;
using Assets.Scripts;
using UnityEngine;

public class FeedbackManager : MonoBehaviour
{
    public GameObject visualFeedback;
    public AudioSource auditiveFeedback;
    public SerialController tactileSerialController;

    private ConditionDescription currentCondition = new ConditionDescription(false, false, false);
    private bool isInTestMode;

    void Start()
    {
        UpdateFeedback(0, 1);
        //TestFeedback();
        //SetTactileFeedback(1,true);
    }

    public void UpdateFeedback(double currentValue, double maxValue)
    {
        if(isInTestMode)
            return;
        var percentage = calculatePercentage(currentValue, maxValue);
        
        SetVisualFeedback(percentage, currentCondition.HasVisual);
        SetTactileFeedback(percentage, currentCondition.HasTactile);
        SetAuditiveFeedback(percentage, currentCondition.HasAuditive);
    }

    public void NextEpoch(ConditionDescription condition)
    {
        currentCondition = condition;
    }

    public void TestFeedback()
    {
        if (!isInTestMode)
        {  
            isInTestMode = true;
            StartCoroutine(nameof(TestFeedbackCo));
        }
    }

    private IEnumerator TestFeedbackCo()
    {
        SetAuditiveFeedback(1, true);
        yield return new WaitForSecondsRealtime(1);
        SetAuditiveFeedback(0, false);
        SetTactileFeedback(1, true);
        yield return new WaitForSecondsRealtime(1);
        SetTactileFeedback(0, false);
        isInTestMode = false;
    }

    private float calculatePercentage(double currentValue, double maxValue)
    {
        return (float)(currentValue / maxValue);
    }
    private void SetVisualFeedback(float percentage, bool activate)
    {
        visualFeedback.transform.parent.gameObject.SetActive(activate);
        visualFeedback.GetComponent<loadingcolorful>().fillAmount = percentage;
    }

    private void SetTactileFeedback(float percentage, bool activate)
    {
        if(!activate)
        {
            SetTactileFeedback(0, true);
            return;
        }
        //Debug.Log(XInputDotNetPure.GamePad.GetState(XInputDotNetPure.PlayerIndex.One).IsConnected);
        if (tactileSerialController is null)
        {
            XInputDotNetPure.GamePad.SetVibration(
                 XInputDotNetPure.PlayerIndex.One,
                 percentage > 0.2 ? percentage * 2 : 0,
                 percentage * 2
            );
            return;
        }
        
        //Vibration wird ausgelöst
        tactileSerialController.SendSerialMessage((percentage > 0 ? (short)(75+(percentage * 255 * 1.6)) : 0) + "\n");
    }

    private void SetAuditiveFeedback(float percentage, bool activate)
    {
        auditiveFeedback.enabled = activate;
        if (!activate)
        {
            auditiveFeedback.Stop();
            return;
        }

        auditiveFeedback.loop = true;
        auditiveFeedback.pitch = (float) (percentage * 0.4);
        
        if(!auditiveFeedback.isPlaying)
        {
            auditiveFeedback.Play();
        }
    }
}