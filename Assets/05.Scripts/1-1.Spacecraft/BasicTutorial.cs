using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 접근을 위해 추가

public class BasicTutorial : MonoBehaviour
{
    public TutorialExpose tutorialExpose;
    public static bool IsEkeyEnabled { get; private set; } = false;

    public GameObject movementUi;
    public GameObject uiA;
    public GameObject uiW;
    public GameObject uiS;
    public GameObject uiD;
    public GameObject scrollUi;

    private bool eventA = false;
    private bool eventW = false;
    private bool eventS = false;
    private bool eventD = false;
    private bool eventScroll = false;
    
    private bool isTransitioning = false; 

    void Start()
    {
        movementUi.SetActive(false);
        uiA.SetActive(false);
        uiW.SetActive(false);
        uiS.SetActive(false);
        uiD.SetActive(false);
    }

    public void CheckInputs()
    {
        if (isTransitioning || AllEventsCompleted())
        {
            if (AllEventsCompleted()) IsEkeyEnabled = true;
            return;
        }

        if (!eventScroll && (!eventA || !eventW || !eventS || !eventD))
        {
            if (tutorialExpose.imageToShow != movementUi)
            {
                tutorialExpose.SetImage(movementUi);
                tutorialExpose.ShowImage();
            }

            if (Input.GetKeyDown(KeyCode.A) && !eventA) { uiA.SetActive(true); eventA = true; }
            if (Input.GetKeyDown(KeyCode.W) && !eventW) { uiW.SetActive(true); eventW = true; }
            if (Input.GetKeyDown(KeyCode.S) && !eventS) { uiS.SetActive(true); eventS = true; }
            if (Input.GetKeyDown(KeyCode.D) && !eventD) { uiD.SetActive(true); eventD = true; }

            if (eventA && eventW && eventS && eventD)
            {
                StartCoroutine(TransitionToScroll());
            }
        }
    }

    IEnumerator TransitionToScroll()
    {
        isTransitioning = true;

        SetChildImageGreen(uiA);
        SetChildImageGreen(uiW);
        SetChildImageGreen(uiS);
        SetChildImageGreen(uiD);

        tutorialExpose.ShowSuccessAndHide();

        yield return new WaitForSeconds(tutorialExpose.displayTime);

        uiA.SetActive(false);
        uiW.SetActive(false);
        uiS.SetActive(false);
        uiD.SetActive(false);
        tutorialExpose.SetImage(scrollUi);
        StartCoroutine(ScrollDetection());
        
        isTransitioning = false;
    }

    private void SetChildImageGreen(GameObject uiObj)
    {
        if (uiObj.TryGetComponent<Image>(out Image img))
        {
            img.color = tutorialExpose.greenColor;
        }
    }

    IEnumerator ScrollDetection()
    {
        tutorialExpose.ShowImage();
        bool scrollingDetected = false;
        
        while (!scrollingDetected)
        {
            if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0)
            {
                scrollingDetected = true;
                eventScroll = true;

                tutorialExpose.ShowSuccessAndHide();
                yield return new WaitForSeconds(tutorialExpose.displayTime);
                
                IsEkeyEnabled = true;
            }
            yield return null;
        }
    }

    public bool AllEventsCompleted()
    {
        return eventA && eventW && eventS && eventD && eventScroll;
    }
}