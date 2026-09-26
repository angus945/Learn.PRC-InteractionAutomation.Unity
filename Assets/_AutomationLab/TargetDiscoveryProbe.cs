using System.Threading.Tasks;
using Module.InteractionAutomation.Targets.Unity3D;
using UnityEngine;

public sealed class TargetDiscoveryProbe : MonoBehaviour
{
    private async void Start()
    {
        await RunAsync();
    }

    private static async Task RunAsync()
    {
        var source =
            AutomationLabInteractionComposition.CreateTargetSource();

        var targets = await source.GetTargetsAsync();

        Debug.Log($"Targets: {targets.Count}");

        foreach (var target in targets)
        {
            Debug.Log(
                $"Target: {target.Id}\n" +
                $"Kind: {target.Kind}\n" +
                $"Bounds: " +
                $"X={target.Bounds.X:0.##}, " +
                $"Y={target.Bounds.Y:0.##}, " +
                $"W={target.Bounds.Width:0.##}, " +
                $"H={target.Bounds.Height:0.##}\n" +
                $"Center: " +
                $"({target.Bounds.Center.X:0.##}, " +
                $"{target.Bounds.Center.Y:0.##})\n" +
                $"Space: {target.Bounds.Space}");
        }
    }
}