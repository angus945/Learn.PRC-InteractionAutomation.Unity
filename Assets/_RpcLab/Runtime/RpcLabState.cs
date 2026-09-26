using System;
using System.Threading;
using Project.RpcLab.Contracts;
using UnityEngine;

namespace Project.RpcLab.Runtime
{
    public sealed class RpcLabState : MonoBehaviour
    {
        private const int MaxMessageLength = 256;

        private string _instanceId = string.Empty;
        private string _message = string.Empty;
        private int _revision;
        private int _ownerThreadId;

        private void Awake()
        {
            _ownerThreadId = Thread.CurrentThread.ManagedThreadId;
            _instanceId = Guid.NewGuid().ToString("N");
            _message = string.Empty;
            _revision = 0;
        }

        public LabStateDto ReadState()
        {
            VerifyAccess();

            return new LabStateDto
            {
                InstanceId = _instanceId,
                Message = _message,
                Revision = _revision,
                Frame = Time.frameCount,
                HandlerThreadId = Thread.CurrentThread.ManagedThreadId
            };
        }

        public LabStateDto SetMessage(string text)
        {
            VerifyAccess();

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (text.Length > MaxMessageLength)
            {
                throw new ArgumentException(
                    "Message must not exceed 256 UTF-16 code units.",
                    nameof(text));
            }

            // 先完成驗證與計算，再修改狀態。
            int nextRevision = checked(_revision + 1);

            _message = text;
            _revision = nextRevision;

            return ReadState();
        }

        private void VerifyAccess()
        {
            if (_ownerThreadId == 0 ||
                Thread.CurrentThread.ManagedThreadId != _ownerThreadId)
            {
                throw new InvalidOperationException(
                    "RpcLabState must be accessed on Unity's main thread after Awake.");
            }
        }
    }
}