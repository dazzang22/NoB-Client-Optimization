using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class OutlineSelection : MonoBehaviour
{
    [SerializeField] private AudioSource highlightSource;
    [SerializeField] private string[] selectableTags = { "SelectableDrawing", "SelectablePasswordScreen", "SelectableIdCard" };
    [SerializeField] private float maxDistance;
    public TutorialExpose tutorialExpose;
    public GameObject EKeyUi;
    public static bool IsOutlineEnabled { get; private set; } = false;
    public static GameObject ClosestObject { get; private set; }

    private Transform selection;

    private bool tutorialExposed = false;
    private static Dictionary<GameObject, bool> objectSoundPlayedMap = new Dictionary<GameObject, bool>();

    //caching 
    private Camera mainCamera;
    private float maxSqrDistance;
    private readonly List<GameObject> selectableObjects = new List<GameObject>();


    void Start()
    {
        highlightSource = GetComponent<AudioSource>();
        mainCamera = Camera.main;
        maxSqrDistance = maxDistance * maxDistance;
        CacheSelectableObjects();
        DisableAllOutlines();
    }

    void Update()
    {
        GameObject closestObject = FindClosestObject();

        if (closestObject != null)
        {
            UpdateClosestObject(closestObject);

            if (ShouldHighlight(closestObject))
            {
                ProcessHighlight(closestObject);
                TryShowTutorial();
            }

            ProcessSelection();
        }
        else
        {
            ClearHighlightAndSelection();
        }
    }
    void TryShowTutorial()
    {
        if (tutorialExposed)
            return;

        if (!BasicTutorial.IsEkeyEnabled)
            return;

        if (tutorialExpose == null || EKeyUi == null)
            return;

        tutorialExpose.SetImage(EKeyUi);
        tutorialExpose.ShowAndHideImage(KeyCode.E);
        tutorialExposed = true;
    }
    void DisableAllOutlines()
    {
        foreach (GameObject obj in selectableObjects)
        {
            if (obj == null) continue;

            Outline outline = obj.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }
    void CacheSelectableObjects()
    {
        selectableObjects.Clear();

        foreach (string tag in selectableTags)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            selectableObjects.AddRange(objectsWithTag);
        }
    }

    bool ShouldHighlight(GameObject obj)
    {
        if (obj != ClosestObject)
            return false;
        return obj.tag.Contains("Selectable") && obj.transform != selection && obj.activeInHierarchy;
    }

    void ProcessHighlight(GameObject obj)
    {
        ClearOutlineComponent(selection);
        EnableOutline(obj);
        selection = obj.transform;
        PlaySelectionSound(obj);
    }


    void ProcessSelection()
    {
        if (selection != null)
        {
            EnableOutline(selection.gameObject);
        }
    }

    public void ClearHighlightAndSelection()
    {
        ClearOutlineComponent(selection);
        selection = null;
    }

    void EnableOutline(GameObject obj)
    {
        if (obj == null) return;

        Outline outlineComponent = obj.GetComponent<Outline>();
        if (outlineComponent != null)
        {
            outlineComponent.enabled = true;
            outlineComponent.OutlineMode = Outline.Mode.OutlineVisible;
            UpdateOutlineStatus(true);
        }
    }

    void ClearOutlineComponent(Transform objTransform)
    {
        if (objTransform != null)
        {
            Outline outlineComponent = objTransform.gameObject.GetComponent<Outline>();
            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
                UpdateOutlineStatus(false);
            }
        }
    }

    void UpdateClosestObject(GameObject obj)
    {
        ClosestObject = obj;
    }

    void UpdateOutlineStatus(bool newStatus)
    {
        IsOutlineEnabled = newStatus;
    }

    GameObject FindClosestObject()
    {
        GameObject closestObject = null;
        float closestSqrDistance = float.MaxValue;
        Vector3 cameraPosition = mainCamera.transform.position;
        float maxSqrDistance = maxDistance * maxDistance;
        var objPosition = Vector3.zero;

        foreach (GameObject obj in selectableObjects)
        {
            if (obj == null || !obj.activeInHierarchy)
                continue;
            objPosition = obj.transform.position;
            if ((objPosition - cameraPosition).sqrMagnitude > maxSqrDistance)
                continue;
            Vector3 screenPoint = mainCamera.WorldToViewportPoint(objPosition);
            if (screenPoint.z <= 0 || screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1)
                continue;
            float sqrDistance = (cameraPosition - objPosition).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestObject = obj;
            }
        }
        return closestObject;
    }

    void PlaySelectionSound(GameObject obj)
    {
        if (!objectSoundPlayedMap.ContainsKey(obj) || !objectSoundPlayedMap[obj])
        {
            if (highlightSource != null && highlightSource.clip != null)
            {
                highlightSource.PlayOneShot(highlightSource.clip);
                objectSoundPlayedMap[obj] = true;
            }
        }
    }
}

