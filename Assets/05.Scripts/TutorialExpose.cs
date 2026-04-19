using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialExpose : MonoBehaviour
{
    public GameObject imageToShow;
    public float displayTime = 5f;
    public Color greenColor = Color.green;
    public float minAlpha = 0.3f; 
    public float maxAlpha = 1f; 
    public float fadeDuration = 1f; 

    private KeyCode hideKeyCode;
    private Image imageComponent;

    private Coroutine fadeCoroutine;
    private Coroutine hideCoroutine;

    public void SetImage(GameObject image)
    {
        imageToShow = image;
        imageComponent = image.GetComponent<Image>();
        imageToShow.SetActive(true);
        SetImageAlpha(0f);
    }

    public void ShowAndHideImage(KeyCode keyCode)  
    {
        ShowImage();
        hideKeyCode = keyCode;
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(WaitAndHide());
    }

    public void ShowAndHideImage() 
    {
        ShowImage();
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator WaitAndHide()
    {
        while (!Input.GetKeyDown(hideKeyCode))
        {
            yield return null;
        }
        Color color = imageComponent.color;
        color.r = greenColor.r;
        color.g = greenColor.g;
        color.b = greenColor.b;
        imageComponent.color = color;
        yield return new WaitForSeconds(displayTime);
        HideImage();
        hideCoroutine = null;
    }
    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        HideImage();
        hideCoroutine = null;
    }

    public void ShowImage()
    {
        imageToShow.SetActive(true);
        if (fadeCoroutine == null)
        {
            fadeCoroutine = StartCoroutine(FadeInOut());
        }
    }

    public void HideImage()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        SetImageAlpha(0f);
    }

    IEnumerator FadeInOut()
    {
        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, elapsedTime / fadeDuration);
                SetImageAlpha(alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            SetImageAlpha(maxAlpha);
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                float alpha = Mathf.Lerp(maxAlpha, minAlpha, elapsedTime / fadeDuration);
                SetImageAlpha(alpha);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            SetImageAlpha(minAlpha);
        }
    }
    private void SetImageAlpha(float alpha)
    {
        imageComponent.color = new Color(imageComponent.color.r, imageComponent.color.g, imageComponent.color.b, alpha);
    }
}



