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
    private AudioSource audioSource;
    //caching original positions and rotations of selectable drawings
    private Camera mainCamera;
    private PlayerController playerController;

    private ObjectState currentState = ObjectState.None;
    private GameObject currentObject;
    private Vector3 originPosition;
    private Quaternion originRotation;

    void Start()
    {
        mainCamera = Camera.main;
        originPosition = transform.position;
        originRotation = transform.rotation;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
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
        if (closestObject == null || closestObject!= gameObject || !gameObject.CompareTag("SelectableDrawing")) return;
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
        if (obj == null) return;
        obj.transform.position = originPosition;
        obj.transform.rotation = originRotation;
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