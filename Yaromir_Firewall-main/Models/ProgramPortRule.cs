using System.Collections.Generic;

namespace Yaromir_Firewall_FINAL1
{
    public class ProgramPortRule
    {
        public string ProgramName { get; set; } = string.Empty;
        public List<int> AllowedPorts { get; set; } = new List<int>();
        public List<int> BlockedPorts { get; set; } = new List<int>();
    }
}
