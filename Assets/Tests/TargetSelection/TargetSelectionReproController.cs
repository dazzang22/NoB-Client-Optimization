using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Positions the two candidates and reports the result produced by the production
/// OutlineSelection component. It never writes OutlineSelection.ClosestObject.
/// </summary>
public sealed class TargetSelectionReproController : MonoBehaviour
{
    public enum ReproCase
    {
        Case1BothOnScreen,
        Case2NearestOffScreen,
        Case3NearestBehindCamera,
        Case4NoValidCandidate
    }

    [Header("Production references")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GameObject candidateA;
    [SerializeField] private GameObject candidateB;

    [Header("Interactive reproduction")]
    [SerializeField] private ReproCase activeCase = ReproCase.Case2NearestOffScreen;
    [SerializeField] private bool applyCaseOnStart = true;

    [Header("Automated evidence run (normally disabled)")]
    [SerializeField] private bool runAllCasesOnStart;
    [SerializeField] private bool exitEditorWhenComplete;

    private static readonly Dictionary<ReproCase, Vector3> CandidateAPositions =
        new Dictionary<ReproCase, Vector3>
        {
            { ReproCase.Case1BothOnScreen, new Vector3(0f, 0f, 0.8f) },
            { ReproCase.Case2NearestOffScreen, new Vector3(1f, 0f, 0.2f) },
            { ReproCase.Case3NearestBehindCamera, new Vector3(0f, 0f, -0.8f) },
            { ReproCase.Case4NoValidCandidate, new Vector3(1f, 0f, 0.2f) }
        };

    private static readonly Dictionary<ReproCase, Vector3> CandidateBPositions =
        new Dictionary<ReproCase, Vector3>
        {
            { ReproCase.Case1BothOnScreen, new Vector3(0f, 0f, 1.4f) },
            { ReproCase.Case2NearestOffScreen, new Vector3(0f, 0f, 1.4f) },
            { ReproCase.Case3NearestBehindCamera, new Vector3(0f, 0f, 1.4f) },
            { ReproCase.Case4NoValidCandidate, new Vector3(-1.2f, 0f, 0.5f) }
        };

    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle passStyle;
    private GUIStyle failStyle;

    public void Configure(Camera cameraReference, GameObject firstCandidate, GameObject secondCandidate)
    {
        targetCamera = cameraReference;
        candidateA = firstCandidate;
        candidateB = secondCandidate;
    }

    private void Start()
    {
        if (applyCaseOnStart)
        {
            ApplyCase(activeCase);
        }

        if (runAllCasesOnStart)
        {
            StartCoroutine(RunAllCases());
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyCase(ReproCase.Case1BothOnScreen);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyCase(ReproCase.Case2NearestOffScreen);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyCase(ReproCase.Case3NearestBehindCamera);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyCase(ReproCase.Case4NoValidCandidate);

        if (targetCamera == null || candidateA == null || candidateB == null) return;

        Debug.DrawLine(targetCamera.transform.position, candidateA.transform.position, Color.red);
        Debug.DrawLine(targetCamera.transform.position, candidateB.transform.position, Color.green);
    }

    public void ApplyCase(ReproCase reproCase)
    {
        activeCase = reproCase;
        candidateA.SetActive(true);
        candidateB.SetActive(true);
        candidateA.transform.position = CandidateAPositions[reproCase];
        candidateB.transform.position = CandidateBPositions[reproCase];
    }

    private IEnumerator RunAllCases()
    {
        // Allow OutlineSelection.Start() to cache Camera.main and maxDistance first.
        yield return null;

        foreach (ReproCase reproCase in System.Enum.GetValues(typeof(ReproCase)))
        {
            ApplyCase(reproCase);
            yield return null;
            yield return null;
            LogCurrentResult(reproCase);
        }

        Debug.Log("[TargetSelectionRepro] AUTOMATED_RUN_COMPLETE");

#if UNITY_EDITOR
        if (exitEditorWhenComplete)
        {
            // Give editor background workers time to flush after the final result.
            yield return new WaitForSecondsRealtime(2f);
            UnityEditor.EditorApplication.Exit(0);
        }
#endif
    }

    private void LogCurrentResult(ReproCase reproCase)
    {
        GameObject actual = OutlineSelection.ClosestObject;
        GameObject expected = ExpectedObject(reproCase);
        string actualName = actual == null ? "null" : actual.name;
        string expectedName = expected == null ? "null" : expected.name;

        Debug.Log(
            $"[TargetSelectionRepro] {CaseLabel(reproCase)} " +
            $"A{{position={Format(candidateA.transform.position)}, distance={Distance(candidateA):F3}, " +
            $"viewport={Format(targetCamera.WorldToViewportPoint(candidateA.transform.position))}, " +
            $"onScreen={IsExpectedOnScreen(candidateA)}}} " +
            $"B{{position={Format(candidateB.transform.position)}, distance={Distance(candidateB):F3}, " +
            $"viewport={Format(targetCamera.WorldToViewportPoint(candidateB.transform.position))}, " +
            $"onScreen={IsExpectedOnScreen(candidateB)}}} " +
            $"Expected={expectedName} Actual={actualName} Match={actual == expected}");
    }

    private GameObject ExpectedObject(ReproCase reproCase)
    {
        switch (reproCase)
        {
            case ReproCase.Case1BothOnScreen:
                return candidateA;
            case ReproCase.Case2NearestOffScreen:
            case ReproCase.Case3NearestBehindCamera:
                return candidateB;
            default:
                return null;
        }
    }

    private float Distance(GameObject candidate)
    {
        return Vector3.Distance(targetCamera.transform.position, candidate.transform.position);
    }

    private bool IsExpectedOnScreen(GameObject candidate)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(candidate.transform.position);
        return viewport.z > 0f &&
               viewport.x >= 0f && viewport.x <= 1f &&
               viewport.y >= 0f && viewport.y <= 1f;
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:F3}, {value.y:F3}, {value.z:F3})";
    }

    private static string CaseLabel(ReproCase reproCase)
    {
        switch (reproCase)
        {
            case ReproCase.Case1BothOnScreen: return "CASE_1_BOTH_ON_SCREEN";
            case ReproCase.Case2NearestOffScreen: return "CASE_2_NEAREST_OFF_SCREEN";
            case ReproCase.Case3NearestBehindCamera: return "CASE_3_NEAREST_BEHIND_CAMERA";
            default: return "CASE_4_NO_VALID_CANDIDATE";
        }
    }

    private void OnGUI()
    {
        if (targetCamera == null || candidateA == null || candidateB == null) return;
        EnsureStyles();

        GameObject actual = OutlineSelection.ClosestObject;
        GameObject expected = ExpectedObject(activeCase);
        bool matches = actual == expected;

        GUILayout.BeginArea(new Rect(16f, 16f, 540f, 330f), panelStyle);
        GUILayout.Label("Target Selection — Production Reproduction", titleStyle);
        GUILayout.Label($"Active: {CaseLabel(activeCase)}   (keys 1–4)");
        GUILayout.Space(6f);
        DrawCandidate("A", candidateA, Color.red);
        DrawCandidate("B", candidateB, Color.green);
        GUILayout.Space(8f);
        GUILayout.Label($"Current Selected: {(actual == null ? "null" : actual.name)}");
        GUILayout.Label($"Expected: {(expected == null ? "null" : expected.name)}");
        GUILayout.Label(matches ? "RESULT: MATCH" : "RESULT: MISMATCH", matches ? passStyle : failStyle);
        GUILayout.Space(8f);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1  Both On-screen")) ApplyCase(ReproCase.Case1BothOnScreen);
        if (GUILayout.Button("2  Nearest Off-screen")) ApplyCase(ReproCase.Case2NearestOffScreen);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("3  Behind Camera")) ApplyCase(ReproCase.Case3NearestBehindCamera);
        if (GUILayout.Button("4  No Valid")) ApplyCase(ReproCase.Case4NoValidCandidate);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawCandidate(string label, GameObject candidate, Color color)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(candidate.transform.position);
        Color previous = GUI.color;
        GUI.color = color;
        GUILayout.Label(
            $"Candidate {label}: distance={Distance(candidate):F3}  " +
            $"viewport={Format(viewport)}  onScreen={IsExpectedOnScreen(candidate)}");
        GUI.color = previous;
    }

    private void EnsureStyles()
    {
        if (panelStyle != null) return;

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 12, 12),
            alignment = TextAnchor.UpperLeft
        };
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        passStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green }, fontStyle = FontStyle.Bold };
        failStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = new Color(1f, 0.35f, 0.35f) }, fontStyle = FontStyle.Bold };
    }
}
