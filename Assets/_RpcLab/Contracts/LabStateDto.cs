namespace Project.RpcLab.Contracts
{
    public sealed class LabStateDto
    {
        public string InstanceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int Revision { get; set; }
        public int Frame { get; set; }
        public int HandlerThreadId { get; set; }
    }
}