using System.Collections.Generic;
using UnityEngine;

public class DrawingPicker : MonoBehaviour
{
    enum ObjectState
    {
        None,
        FacingFront,
        FacingBack
    }
    [SerializeField] private AudioClip pickUpSound;
    public GameObject player;
    public float distanceToCamera = 0.6f;

    //private bool isObjectFacingFront = false;
    //private bool isObjectFacingBack = false;

    private OutlineSelection outlineSelection;
    private AudioSource audioSource;
    //caching original positions and rotations of selectable drawings
    private Camera mainCamera;
    private PlayerController playerController;

    //Vector3[] originalPositions;
    private ObjectState currentState = ObjectState.None;
    private GameObject currentObject;
    private readonly Dictionary<GameObject, Vector3> originalPositions = new();
    private readonly Dictionary<GameObject, Quaternion> originalRotations = new();

    void Start()
    {
        outlineSelection = GetComponent<OutlineSelection>();
        if (outlineSelection == null)
        {
            outlineSelection = gameObject.AddComponent<OutlineSelection>();
        }
        mainCamera = Camera.main;
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
        SaveOriginalTransforms();
    }

    void SaveOriginalTransforms()
{
    originalPositions.Clear();
    originalRotations.Clear();
    foreach (var info in outlineSelection.GetSelectableInfos()) 
    {
        originalPositions[info.obj] = info.transform.position;
        originalRotations[info.obj] = info.transform.rotation;
    }
}

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (!OutlineSelection.IsOutlineEnabled || PasswordEventCameraController.IsPasswordActive)
        {
            Debug.Log("Outline is not enabled or password event is active.");
            return;
        }
        GameObject closestObject = OutlineSelection.ClosestObject;
        if (closestObject == null || !closestObject.CompareTag("SelectableDrawing"))
            return;
        if (currentState == ObjectState.None)
        {
            currentObject = closestObject;
        }
        if (currentObject == null) return;
        switch (currentState)
        {
            case ObjectState.None:
                EnterInspectMode(currentObject);
                currentState = ObjectState.FacingFront;
                break;
            case ObjectState.FacingFront:
                RotateObject(currentObject);
                currentState = ObjectState.FacingBack;
                break;
            case ObjectState.FacingBack:
                ExitInspectMode(currentObject);
                currentState = ObjectState.None;
                currentObject = null;
                break;
        }

    }
    void EnterInspectMode(GameObject obj)
    {
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        LookAtObjectFront(obj);
        PlaySound(obj, pickUpSound);
    }

    void ExitInspectMode(GameObject obj)
    {
        ResetObjectPosition(obj);

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        PlaySound(obj, pickUpSound);
    }


    void LookAtObjectFront(GameObject obj)
    {
        if (obj == null) return;
        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceToCamera;
        obj.transform.position = targetPosition;

        Quaternion localRotation = Quaternion.Euler(0, 180f, -90f);
        Quaternion targetRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up) * localRotation;

        obj.transform.rotation = targetRotation;
    }

    void ResetObjectPosition(GameObject obj)
    {
        if (originalPositions.TryGetValue(obj, out Vector3 originalPosition) && originalRotations.TryGetValue(obj, out Quaternion originalRotation))
        {
            obj.transform.position = originalPosition;
            obj.transform.rotation = originalRotation;
        }
    }

    void RotateObject(GameObject obj)
    {
        if (obj == null || mainCamera == null)
        return;

        Vector3 targetPosition = mainCamera.transform.position + mainCamera.transform.forward * distanceToCamera;
        obj.transform.position = targetPosition;

        Quaternion localRotation = Quaternion.Euler(0f, 0f, -90f);
        Quaternion targetRotation = Quaternion.LookRotation(mainCamera.transform.forward, Vector3.up) * localRotation;

        obj.transform.rotation = targetRotation;

        PlaySound(obj, pickUpSound);
    }
    void PlaySound(GameObject obj, AudioClip sound)
    {
        audioSource = obj.GetComponent<AudioSource>();
        if (sound != null && audioSource != null)
        {
            audioSource.PlayOneShot(sound);
        }
    }
}