using System;
using System.Collections.Generic;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.Coordinates.Unity3D;
using Framework.InteractionAutomation.Monkey;
using Framework.InteractionAutomation.Monkey.Unity3D;
using Module.Verification.Oracle;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AutoLabMonkeyPresentation :
    MonoBehaviour
{
    [SerializeField]
    private bool showHud = true;

    [SerializeField]
    private bool showCursor = true;

    [SerializeField]
    private bool showTrail = true;

    [SerializeField]
    private int historyCapacity = 8;

    [SerializeField]
    private int trailCapacity = 24;

    [SerializeField]
    private float trailSampleDistance = 4f;

    [SerializeField]
    private float cursorSize = 18f;

    private readonly List<string> history =
        new List<string>();

    private readonly List<Vector2> trail =
        new List<Vector2>();

    private Mouse mouse;
    private Texture2D pixel;
    private Vector2 pointerPosition;
    private bool hasPointerPosition;
    private bool pointerPressed;

    private int seed;
    private int currentIteration;
    private int totalIterations;
    private string currentAction = "<none>";
    private string currentTarget = "<none>";
    private string status = "Idle";
    private string lastError = string.Empty;
    private string lastVerification = "<none>";
    private string coverage = "0 / 0";
    private string evidence = "<none>";
    private float pulseUntil;

    private GUIStyle titleStyle;
    private GUIStyle statusStyle;
    private GUIStyle detailStyle;

    public void Bind(
        Mouse virtualMouse)
    {
        mouse =
            virtualMouse ??
            throw new ArgumentNullException(
                nameof(virtualMouse));

        trail.Clear();
        hasPointerPosition = false;
    }

    public void Unbind()
    {
        mouse = null;
        pointerPressed = false;
    }

    public void BeginRun(
        int runSeed,
        int iterations)
    {
        seed = runSeed;
        currentIteration = 0;
        totalIterations = iterations;
        currentAction = "<none>";
        currentTarget = "<none>";
        status = "Starting";
        lastError = string.Empty;
        lastVerification = "<none>";
        coverage = "0 / 0";
        evidence = "<none>";
        history.Clear();
        trail.Clear();
    }

    public void SelectStep(
        PointerMonkeyStep step)
    {
        currentIteration = step.Sequence;
        currentAction = step.ActionId;
        currentTarget =
            step.DestinationTargetId.HasValue
                ? $"{step.TargetId.Value} => " +
                  $"{step.DestinationTargetId.Value.Value}"
                : step.TargetId.Value;
        status = "Selected";
        lastError = string.Empty;
    }

    public void BeginStep(
        PointerMonkeyStep step)
    {
        currentIteration = step.Sequence;
        currentAction = step.ActionId;
        status = "Executing";
        pulseUntil =
            Time.unscaledTime + 0.2f;
    }

    public void CompleteStep(
        PointerMonkeyStep step,
        EvaluationReport verification)
    {
        status = "Verified";
        lastVerification =
            Summarize(
                verification);

        AddHistory(
            $"#{step.Sequence} {step.ActionId} -> " +
            $"{FormatTarget(step)} " +
            $"{verification.Verdict}");
    }

    public void FailSelection(
        int sequence,
        string detail)
    {
        currentIteration = sequence;
        currentAction = "<selection>";
        currentTarget = "<unknown>";
        status = "Failed";
        lastError = detail ?? string.Empty;

        AddHistory(
            $"#{sequence} selection FAIL");
    }

    public void FailStep(
        PointerMonkeyStep step,
        string detail)
    {
        currentIteration = step.Sequence;
        currentAction = step.ActionId;
        currentTarget =
            FormatTarget(step);
        status = "Failed";
        lastError = detail ?? string.Empty;
        lastVerification = detail ?? string.Empty;

        AddHistory(
            $"#{step.Sequence} {step.ActionId} -> " +
            $"{FormatTarget(step)} FAIL");
    }

    public void UpdateCoverage(
        PointerMonkeyCoverageSnapshot snapshot)
    {
        coverage =
            $"{snapshot.CoveredCount} / " +
            $"{snapshot.EligibleCount} " +
            $"({snapshot.Ratio * 100d:0.#}%)";
    }

    public void CompleteRun(
        UnityPointerMonkeyRunReport report)
    {
        status =
            report.Passed
                ? "Run Complete"
                : $"Run {report.Verdict}";

        currentAction = "<complete>";

        evidence =
            $"entries={report.Evidence.Entries.Count}, " +
            $"retainedSteps={report.RetainedSteps.Count}, " +
            $"overwritten={report.OverwrittenStepCount}";
    }

    private void Awake()
    {
        pixel =
            new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                false)
            {
                name = "AutoLab Monkey Presentation Pixel",
                hideFlags = HideFlags.HideAndDontSave
            };

        pixel.SetPixel(
            0,
            0,
            Color.white);

        pixel.Apply();
    }

    private void Update()
    {
        if (mouse == null ||
            !mouse.added)
        {
            pointerPressed = false;
            return;
        }

        Vector2 unityPosition =
            mouse.position.ReadValue();

        InteractionPoint applicationPosition =
            UnityApplicationCoordinates
                .ToApplication(
                    unityPosition,
                    Screen.height);

        pointerPosition =
            new Vector2(
                (float)applicationPosition.X,
                (float)applicationPosition.Y);

        pointerPressed =
            mouse.leftButton.isPressed;

        if (!hasPointerPosition)
        {
            hasPointerPosition = true;
            trail.Add(pointerPosition);
            return;
        }

        if (!showTrail)
            return;

        if (trail.Count == 0 ||
            Vector2.Distance(
                trail[trail.Count - 1],
                pointerPosition) >=
            trailSampleDistance)
        {
            trail.Add(pointerPosition);

            while (trail.Count >
                   Math.Max(1, trailCapacity))
            {
                trail.RemoveAt(0);
            }
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (showTrail)
            DrawTrail();

        if (showCursor &&
            hasPointerPosition)
        {
            DrawCursor();
        }

        if (showHud)
            DrawHud();
    }

    private void DrawTrail()
    {
        if (pixel == null ||
            trail.Count == 0)
        {
            return;
        }

        for (int i = 0;
             i < trail.Count;
             i++)
        {
            float normalized =
                trail.Count <= 1
                    ? 1f
                    : (float)(i + 1) /
                      trail.Count;

            Color color =
                new Color(
                    0.1f,
                    0.8f,
                    1f,
                    Mathf.Lerp(
                        0.12f,
                        0.55f,
                        normalized));

            DrawRect(
                new Rect(
                    trail[i].x - 2f,
                    trail[i].y - 2f,
                    4f,
                    4f),
                color);
        }
    }

    private void DrawCursor()
    {
        float pulse =
            Time.unscaledTime <
            pulseUntil
                ? 6f
                : 0f;

        float size =
            cursorSize +
            pulse +
            (pointerPressed ? 8f : 0f);

        Color cursorColor =
            pointerPressed
                ? new Color(
                    1f,
                    0.35f,
                    0.2f,
                    0.95f)
                : new Color(
                    0.1f,
                    0.85f,
                    1f,
                    0.95f);

        float half =
            size / 2f;

        DrawRect(
            new Rect(
                pointerPosition.x - half,
                pointerPosition.y - 1.5f,
                size,
                3f),
            cursorColor);

        DrawRect(
            new Rect(
                pointerPosition.x - 1.5f,
                pointerPosition.y - half,
                3f,
                size),
            cursorColor);

        float ringSize =
            size + 8f;

        DrawBorder(
            new Rect(
                pointerPosition.x -
                ringSize / 2f,
                pointerPosition.y -
                ringSize / 2f,
                ringSize,
                ringSize),
            cursorColor,
            2f);

        string pointerLabel =
            pointerPressed
                ? $"DOWN  {currentAction}\n{currentTarget}"
                : $"{currentAction}\n{currentTarget}";

        GUI.Box(
            new Rect(
                pointerPosition.x + 16f,
                pointerPosition.y + 12f,
                240f,
                48f),
            pointerLabel,
            detailStyle);
    }

    private void DrawHud()
    {
        const float width = 430f;
        float height =
            lastError.Length > 0
                ? 430f
                : 380f;

        GUILayout.BeginArea(
            new Rect(
                12f,
                12f,
                width,
                height),
            GUI.skin.box);

        GUILayout.Label(
            "Deterministic Monkey QA",
            titleStyle);

        GUILayout.Space(4f);

        GUILayout.Label(
            $"Seed: {seed}",
            detailStyle);

        GUILayout.Label(
            $"Iteration: {currentIteration} / {totalIterations}",
            detailStyle);

        GUILayout.Label(
            $"Action: {currentAction}",
            detailStyle);

        GUILayout.Label(
            $"Target: {currentTarget}",
            detailStyle);

        GUILayout.Label(
            $"Coverage: {coverage}",
            detailStyle);

        GUILayout.Label(
            $"Pointer: " +
            $"{(hasPointerPosition ? pointerPosition.ToString("0.0") : "<unbound>")}",
            detailStyle);

        GUILayout.Label(
            $"Button: {(pointerPressed ? "DOWN" : "UP")}",
            detailStyle);

        GUILayout.Label(
            $"Status: {status}",
            statusStyle);

        GUILayout.Label(
            $"Verification: {lastVerification}",
            detailStyle);

        GUILayout.Label(
            $"Evidence: {evidence}",
            detailStyle);

        if (lastError.Length > 0)
        {
            GUILayout.Space(4f);

            GUILayout.Label(
                "Last Error:",
                statusStyle);

            GUILayout.Label(
                lastError,
                detailStyle);
        }

        GUILayout.Space(6f);

        GUILayout.Label(
            "Recent Steps",
            statusStyle);

        for (int i = 0;
             i < history.Count;
             i++)
        {
            GUILayout.Label(
                history[i],
                detailStyle);
        }

        GUILayout.EndArea();
    }

    private static string Summarize(
        EvaluationReport report)
    {
        if (report.Results.Count > 0)
        {
            OracleResult result =
                report.Results[
                    report.Results.Count - 1];

            return
                $"{report.Verdict}: " +
                $"{result.Code} — {result.Detail}";
        }

        if (report.Errors.Count > 0)
        {
            EvaluationError error =
                report.Errors[
                    report.Errors.Count - 1];

            return
                $"{report.Verdict}: " +
                $"{error.OracleId} — {error.Message}";
        }

        return report.Verdict.ToString();
    }

    private static string FormatTarget(
        PointerMonkeyStep step)
    {
        if (!step.DestinationTargetId.HasValue)
            return step.TargetId.Value;

        return
            $"{step.TargetId.Value} => " +
            $"{step.DestinationTargetId.Value.Value}";
    }

    private void AddHistory(
        string entry)
    {
        history.Add(
            entry ?? string.Empty);

        int capacity =
            Math.Max(
                1,
                historyCapacity);

        while (history.Count >
               capacity)
        {
            history.RemoveAt(0);
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

        statusStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                fontStyle = FontStyle.Bold
            };

        detailStyle =
            new GUIStyle(
                GUI.skin.label)
            {
                wordWrap = true
            };
    }

    private void DrawBorder(
        Rect rect,
        Color color,
        float thickness)
    {
        DrawRect(
            new Rect(
                rect.x,
                rect.y,
                rect.width,
                thickness),
            color);

        DrawRect(
            new Rect(
                rect.x,
                rect.yMax - thickness,
                rect.width,
                thickness),
            color);

        DrawRect(
            new Rect(
                rect.x,
                rect.y,
                thickness,
                rect.height),
            color);

        DrawRect(
            new Rect(
                rect.xMax - thickness,
                rect.y,
                thickness,
                rect.height),
            color);
    }

    private void DrawRect(
        Rect rect,
        Color color)
    {
        if (pixel == null)
            return;

        Color previous =
            GUI.color;

        GUI.color =
            color;

        GUI.DrawTexture(
            rect,
            pixel);

        GUI.color =
            previous;
    }

    private void OnDestroy()
    {
        if (pixel != null)
        {
            Destroy(
                pixel);
        }
    }
}
