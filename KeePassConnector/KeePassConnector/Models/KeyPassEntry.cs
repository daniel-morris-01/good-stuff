using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeePassConnector.Models
{
    public class KeyPassEntry
    {
        public string? Name { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public string? StringFields { get; set; }
        public string? Uuid { get; set; }
    }
}
