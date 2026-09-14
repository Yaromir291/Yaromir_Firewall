namespace Yaromir_Firewall_FINAL1
{
    public class ConnectionInfo
    {
        public string ProcessName { get; set; } = string.Empty;
        public int Pid { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}