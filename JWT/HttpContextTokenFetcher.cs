using HttpClients;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aims.Services.JWT
{
    public class HttpContextTokenFetcher:ITokenFetcher
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpContextTokenFetcher(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetToken()
        {
            string token = "";

            if (httpContextAccessor.HttpContext != null)
            {
                var bearer = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToList().Find(x => x.StartsWith("Bearer")) ?? "";
                if (!string.IsNullOrEmpty(bearer))
                {
                    token = bearer.Replace("Bearer ", "");
                }
            }
            return token;
        }
    }
}
