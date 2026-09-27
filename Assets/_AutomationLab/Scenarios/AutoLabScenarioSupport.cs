using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Module.Verification.Oracle;
using UnityEngine;

public static class AutoLabScenarioSupport
{
    public static IEnumerator WaitFor(
        Task task)
    {
        while (!task.IsCompleted)
            yield return null;
    }

    public static bool ReportFault(
        string label,
        Task task)
    {
        if (task.IsCanceled)
        {
            Debug.LogError(
                $"{label} CANCELLED");
            return true;
        }

        if (!task.IsFaulted)
            return false;

        Debug.LogException(
            task.Exception?
                .GetBaseException());

        return true;
    }

    public static bool ReportVerificationFailure(
        string label,
        EvaluationReport report)
    {
        if (report.Verdict == TestVerdict.Passed)
            return false;

        var message =
            new StringBuilder();

        message
            .Append(label)
            .Append(" VERIFICATION FAILED\n")
            .Append("Verdict=")
            .Append(report.Verdict)
            .Append('\n');

        foreach (OracleResult result in report.Results)
        {
            message
                .Append(result.Code)
                .Append(": ")
                .Append(result.Verdict)
                .Append(" — ")
                .Append(result.Detail)
                .Append('\n');
        }

        foreach (EvaluationError error in report.Errors)
        {
            message
                .Append("OracleError ")
                .Append(error.OracleId)
                .Append(": ")
                .Append(error.ExceptionType)
                .Append(" — ")
                .Append(error.Message)
                .Append('\n');
        }

        Debug.LogError(
            message.ToString());

        return true;
    }
}
