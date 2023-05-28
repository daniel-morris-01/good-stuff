using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePassConnector.Models
{
    internal class KeyPassHttpResponse
    {
        public int Count { get; set; }
        public string? Error { get; set; }
        public string? Hash { get; set; }
        public string? Id { get; set; }
        public string? Nonce { get; set; }
        public string? RequestType { get; set; }
        public bool Success { get; set; }
        public string? Verifier { get; set; }
        public string? Version { get; set; }
        public string? objectName { get; set; }
        public KeyPassEntry[]? Entries { get; set; }
    }
}
