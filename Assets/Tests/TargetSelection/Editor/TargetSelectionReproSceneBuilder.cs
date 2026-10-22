using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TargetSelectionReproSceneBuilder
{
    public const string ScenePath = "Assets/Tests/TargetSelection/TargetSelectionRepro.unity";

    [MenuItem("Tools/Target Selection Repro/Generate Scene")]
    public static void GenerateScene()
    {
        EnsureFolder("Assets/Tests");
        EnsureFolder("Assets/Tests/TargetSelection");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject player = new GameObject("Player_SelectionOrigin");
        player.transform.position = Vector3.zero;
        AudioSource audioSource = player.AddComponent<AudioSource>();
        OutlineSelection productionSelection = player.AddComponent<OutlineSelection>();

        SerializedObject selectionSerializedObject = new SerializedObject(productionSelection);
        selectionSerializedObject.FindProperty("highlightSource").objectReferenceValue = audioSource;
        selectionSerializedObject.FindProperty("maxDistance").floatValue = 1.5f;
        selectionSerializedObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;
        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraComponent.fieldOfView = 60f;
        cameraComponent.nearClipPlane = 0.1f;
        cameraComponent.farClipPlane = 100f;

        GameObject candidateA = CreateCandidate("Candidate_A_Nearest", new Vector3(1f, 0f, 0.2f));
        GameObject candidateB = CreateCandidate("Candidate_B_Expected", new Vector3(0f, 0f, 1.4f));

        GameObject controllerObject = new GameObject("TargetSelectionReproController");
        TargetSelectionReproController controller = controllerObject.AddComponent<TargetSelectionReproController>();
        controller.Configure(cameraComponent, candidateA, candidateB);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new IOException($"Failed to save reproduction scene at {ScenePath}");
        }

        Selection.activeGameObject = controllerObject;
        Debug.Log($"[TargetSelectionRepro] Scene generated: {ScenePath}");
    }

    // Used by the command-line evidence run. This changes only the in-memory scene.
    public static void RunAutomatedEvidence()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TargetSelectionReproController controller = Object.FindObjectOfType<TargetSelectionReproController>();
        if (controller == null)
        {
            throw new MissingReferenceException("TargetSelectionReproController was not found in the repro scene.");
        }

        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("runAllCasesOnStart").boolValue = true;
        serializedController.FindProperty("exitEditorWhenComplete").boolValue = true;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[TargetSelectionRepro] Starting automated evidence run from {scene.path}");
        EditorApplication.isPlaying = true;
    }

    private static GameObject CreateCandidate(string objectName, Vector3 position)
    {
        GameObject candidate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        candidate.name = objectName;
        candidate.tag = "SelectableDrawing";
        candidate.transform.position = position;
        candidate.transform.localScale = Vector3.one * 0.15f;

        Outline outline = candidate.AddComponent<Outline>();
        outline.enabled = false;
        return candidate;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
