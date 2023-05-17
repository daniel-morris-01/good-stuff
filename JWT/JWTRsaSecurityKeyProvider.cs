using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace Aims.Services.JWT
{
    public class JWTRsaSecurityKeyProvider
    {
        static RsaSecurityKey? _publicKey;
        static RsaSecurityKey? _privateKey;


        public static SecurityKey GetPublicKey(string configPrefix,IConfiguration configuration)
        {
            if (_publicKey == null)
            {
                RSA rsa = RSA.Create();
                var publicKey = configuration[$"{configPrefix}:PublicKey"];
                rsa.ImportRSAPublicKey(
                    source: Convert.FromBase64String(publicKey),
                    bytesRead: out int _
                );

                _publicKey = new RsaSecurityKey(rsa);
            }

            return _publicKey;
        }

        public static SecurityKey GetPrivateKey(string configPrefix, IConfiguration configuration)
        {
            if (_privateKey == null)
            {
                RSA rsa = RSA.Create();
                var privateKey = configuration[$"{configPrefix}:PrivateKey"];
                rsa.ImportRSAPrivateKey(
                    source: Convert.FromBase64String(privateKey),
                    bytesRead: out int _
                );

                _privateKey = new RsaSecurityKey(rsa);
            }

            return _privateKey;
        }
    }
}