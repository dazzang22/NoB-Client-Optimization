using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OutlineSelection : MonoBehaviour
{
    public struct SelectableInfo 
    {
        public GameObject obj;
        public Outline outline;
        public Transform transform;
    }

    [SerializeField] private AudioSource highlightSource;
    [SerializeField] private string[] selectableTags = { "SelectableDrawing", "SelectablePasswordScreen", "SelectableIdCard" };
    [SerializeField] private float maxDistance;
    
    public TutorialExpose tutorialExpose;
    public GameObject EKeyUi;
    public static bool IsOutlineEnabled { get; private set; } = false;
    public static GameObject ClosestObject { get; private set; }

    private SelectableInfo? selectionInfo; 
    private bool tutorialExposed = false;
    private static Dictionary<GameObject, bool> objectSoundPlayedMap = new Dictionary<GameObject, bool>();

    // 캐싱용 리스트
    private Camera mainCamera;
    private float maxSqrDistance;
    private readonly List<SelectableInfo> selectableInfos = new List<SelectableInfo>();

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
        SelectableInfo? closest = FindClosestInfo();

        if (closest.HasValue)
        {
            UpdateClosestObject(closest.Value.obj);

            if (ShouldHighlight(closest.Value))
            {
                ProcessHighlight(closest.Value);
                TryShowTutorial();
            }

            ProcessSelection();
        }
        else
        {
            ClearHighlightAndSelection();
        }
    }

    public List<SelectableInfo> GetSelectableInfos()
    {
        return selectableInfos;
    }

    void CacheSelectableObjects()
    {
        selectableInfos.Clear();
        foreach (string tag in selectableTags)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            foreach (var obj in objectsWithTag)
            {
                selectableInfos.Add(new SelectableInfo {
                    obj = obj,
                    outline = obj.GetComponent<Outline>(),
                    transform = obj.transform
                });
            }
        }
    }

    // 구조체 사용
    SelectableInfo? FindClosestInfo()
    {
        SelectableInfo? closest = null;
        float closestSqrDistance = float.MaxValue;
        Vector3 cameraPosition = mainCamera.transform.position;

        foreach (var info in selectableInfos)
        {
            if (info.obj == null || !info.obj.activeInHierarchy) continue;

            float sqrDistance = (info.transform.position - cameraPosition).sqrMagnitude;
            
            if (sqrDistance <= maxSqrDistance && sqrDistance < closestSqrDistance)
            {
                closest = info;
                closestSqrDistance = sqrDistance;
            }
        }

        if (closest.HasValue)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(closest.Value.transform.position);
            if(!(0 <= screenPoint.x && screenPoint.x <= Screen.width && 0 <= screenPoint.y && screenPoint.y <= Screen.height))
            {
                return null;
            }
        }
        return closest;
    }

    bool ShouldHighlight(SelectableInfo info)
    {
        if (info.obj != ClosestObject) return false;
        
        bool hasValidTag = false;
        foreach(var tag in selectableTags) {
            if(info.obj.CompareTag(tag)) { hasValidTag = true; break; }
        }

        return hasValidTag && info.transform != (selectionInfo?.transform) && info.obj.activeInHierarchy;
    }

    void ProcessHighlight(SelectableInfo info)
    {
        ClearOutlineComponent(selectionInfo);
        EnableOutline(info);
        selectionInfo = info;
        PlaySelectionSound(info.obj);
    }

    void ProcessSelection()
    {
        if (selectionInfo.HasValue)
        {
            EnableOutline(selectionInfo.Value);
        }
    }

    public void ClearHighlightAndSelection()
    {
        ClearOutlineComponent(selectionInfo);
        selectionInfo = null;
        ClosestObject = null;
    }

    void EnableOutline(SelectableInfo info)
    {
        if (info.outline != null)
        {
            info.outline.enabled = true;
            info.outline.OutlineMode = Outline.Mode.OutlineVisible;
            IsOutlineEnabled = true;
        }
    }

    void ClearOutlineComponent(SelectableInfo? info)
    {
        if (info.HasValue && info.Value.outline != null)
        {
            info.Value.outline.enabled = false;
            IsOutlineEnabled = false;
        }
    }

    void UpdateClosestObject(GameObject obj) { ClosestObject = obj; }

    void DisableAllOutlines()
    {
        foreach (var info in selectableInfos)
        {
            if (info.outline != null) info.outline.enabled = false;
        }
    }

    void TryShowTutorial()
    {
        if (tutorialExposed || !BasicTutorial.IsEkeyEnabled || tutorialExpose == null || EKeyUi == null) return;
        tutorialExpose.SetImage(EKeyUi);
        tutorialExpose.ShowAndHideImage(KeyCode.E);
        tutorialExposed = true;
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