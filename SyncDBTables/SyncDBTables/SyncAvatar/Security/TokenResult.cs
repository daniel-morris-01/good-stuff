using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncAvatar.Security
{
    public class TokenResult
    {
        public string? token { get; set; }
        public DateTime expirationTimeUTC { get; set; }
    }
}
