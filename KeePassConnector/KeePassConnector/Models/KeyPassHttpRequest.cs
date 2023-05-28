using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePassConnector.Models
{
    internal class KeyPassHttpRequest
    {
        public string? RequestType { get; set; }
        public string? SortSelection { get; set; }
        public string? TriggerUnlock { get; set; }
        public string? Id { get; set; }
        public string? Nonce { get; set; }
        public string? Verifier { get; set; }
        public string? Url { get; set; }
    }
}
