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
    public float fadeDuration = 2f;

    private KeyCode hideKeyCode;
    private Image imageComponent;
    private Color tempColor;

    private Coroutine fadeCoroutine;
    private Coroutine hideCoroutine;

    public void SetImage(GameObject image)
    {
        imageToShow = image;
        imageComponent = image.GetComponent<Image>();
        imageComponent.color = new Color(imageComponent.color.r, imageComponent.color.g, imageComponent.color.b, 0f);
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
    public void ShowSuccessAndHide()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        // 페이드 효과를 멈추고 성공 연출 코루틴 실행
        StartCoroutine(SuccessRoutine());
    }

    private IEnumerator SuccessRoutine()
    {
        // 알파값을 최대(maxAlpha)로 올리고 초록색으로 덮어씌움
        Color color = imageComponent.color;
        color.r = greenColor.r;
        color.g = greenColor.g;
        color.b = greenColor.b;
        color.a = maxAlpha;
        imageComponent.color = color;

        // 초록색 상태로 유저가 인식할 수 있게 잠시 대기
        yield return new WaitForSeconds(displayTime);

        HideImage();
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
    // 클래스 상단에 변수 선언 (캐싱)

    private void SetImageAlpha(float alpha)
    {
        // imageComponent.color를 직접 수정하지 않고 값만 가져와서 알파만 바꿉니다.
        tempColor = imageComponent.color;
        tempColor.a = alpha;
        imageComponent.color = tempColor;
    }

    // FadeInOut 내부의 while 루프 안에서도 SetImageAlpha를 쓰면 이제 안전합니다.
}



