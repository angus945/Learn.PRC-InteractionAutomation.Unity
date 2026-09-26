#nullable enable

using System;
using Project.RpcLab.Contracts;
using TMPro;
using UnityEngine;

namespace Project.RpcLab.Runtime
{
    public sealed class RpcLabDebugPanel : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private RpcLabState _state = null!;

        [Header("Existing scene labels")]
        [SerializeField] private TMP_Text _connectionText = null!;
        [SerializeField] private TMP_Text _messageText = null!;
        [SerializeField] private TMP_Text _revisionText = null!;
        [SerializeField] private TMP_Text _frameText = null!;

        [Header("Local experiment")]
        [SerializeField, TextArea]
        private string _localMessage = "Hello from Unity";

        private void Start()
        {
            if (_state == null ||
                _connectionText == null ||
                _messageText == null ||
                _revisionText == null ||
                _frameText == null)
            {
                enabled = false;

                throw new InvalidOperationException(
                    "Assign State and all four TMP text references.");
            }

            _connectionText.text = "Connection: Local only";
            _messageText.richText = false;

            Render(_state.ReadState());
        }

        private void Update()
        {
            Render(_state.ReadState());
        }

        [ContextMenu("RPC Lab/Set Local Message")]
        public void SetLocalMessage()
        {
            if (!CanRunLocalCommand())
            {
                return;
            }

            try
            {
                LabStateDto result = _state.SetMessage(_localMessage);

                Render(result);
                LogState(result);
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning("Rejected: " + exception.Message, this);
            }
        }

        [ContextMenu("RPC Lab/Read Local State")]
        public void ReadLocalState()
        {
            if (!CanRunLocalCommand())
            {
                return;
            }

            LogState(_state.ReadState());
        }

        private bool CanRunLocalCommand()
        {
            if (!Application.isPlaying ||
                !isActiveAndEnabled ||
                _state == null)
            {
                Debug.LogWarning(
                    "Enter Play Mode and assign State before running this command.",
                    this);

                return false;
            }

            return true;
        }

        private void Render(LabStateDto state)
        {
            _messageText.text = "Message: " + state.Message;
            _revisionText.text = "Revision: " + state.Revision;
            _frameText.text = "Frame: " + state.Frame;
        }

        private void LogState(LabStateDto state)
        {
            Debug.Log(
                $"message=\"{state.Message}\" " +
                $"revision={state.Revision} " +
                $"frame={state.Frame} " +
                $"thread={state.HandlerThreadId} " +
                $"instance={state.InstanceId}",
                this);
        }
    }
}