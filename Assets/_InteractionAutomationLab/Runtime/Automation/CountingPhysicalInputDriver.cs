using System;
using System.Threading;
using System.Threading.Tasks;
using Module.InteractionAutomation.Coordinates;
using Module.InteractionAutomation.PhysicalInput;

namespace Project.InteractionAutomationLab.Automation
{
    public sealed class CountingPhysicalInputDriver : IPhysicalInputDriver
    {
        private readonly IPhysicalInputDriver inner;
        private TaskCompletionSource<bool> nextPointerDown;

        public CountingPhysicalInputDriver(IPhysicalInputDriver inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public int SubmissionCount { get; private set; }
        public int PointerDownCount { get; private set; }
        public int PointerUpCount { get; private set; }
        public bool IsPointerPressed { get; private set; }

        public Task ArmNextPointerDownSignal()
        {
            if (nextPointerDown != null && !nextPointerDown.Task.IsCompleted)
                throw new InvalidOperationException("A pointer-down signal is already armed.");
            nextPointerDown = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return nextPointerDown.Task;
        }

        public async ValueTask MovePointerAsync(InteractionPoint position, CancellationToken cancellationToken = default)
        {
            await inner.MovePointerAsync(position, cancellationToken);
            SubmissionCount++;
        }

        public async ValueTask PointerDownAsync(PointerButton button, CancellationToken cancellationToken = default)
        {
            await inner.PointerDownAsync(button, cancellationToken);
            SubmissionCount++;
            PointerDownCount++;
            IsPointerPressed = true;
            nextPointerDown?.TrySetResult(true);
        }

        public async ValueTask PointerUpAsync(PointerButton button, CancellationToken cancellationToken = default)
        {
            await inner.PointerUpAsync(button, cancellationToken);
            SubmissionCount++;
            PointerUpCount++;
            IsPointerPressed = false;
        }

        public async ValueTask KeyDownAsync(PhysicalKey key, CancellationToken cancellationToken = default)
        {
            await inner.KeyDownAsync(key, cancellationToken);
            SubmissionCount++;
        }

        public async ValueTask KeyUpAsync(PhysicalKey key, CancellationToken cancellationToken = default)
        {
            await inner.KeyUpAsync(key, cancellationToken);
            SubmissionCount++;
        }

        public async ValueTask ScrollAsync(ScrollDelta delta, CancellationToken cancellationToken = default)
        {
            await inner.ScrollAsync(delta, cancellationToken);
            SubmissionCount++;
        }
    }
}
