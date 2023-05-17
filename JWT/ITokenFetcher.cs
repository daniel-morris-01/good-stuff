using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpClients
{
    public interface ITokenFetcher
    {
        string GetToken();
    }
}
