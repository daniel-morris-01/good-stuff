using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KeePassConnector
{
    internal class EncryptionManager
    {
        public static string Encrypt(string text, byte[] key, byte[] iv)
        {
            using var AES = Aes.Create("AesCryptoServiceProvider");
            AES!.Key = key;
            AES.IV = iv;
            using var memoryStream = new MemoryStream();
            var plainBytes = Encoding.UTF8.GetBytes(text);
            using var csStream = new CryptoStream(memoryStream
                , AES.CreateEncryptor()
                , CryptoStreamMode.Write);
            csStream.Write(plainBytes, 0, plainBytes.Length);
            csStream.FlushFinalBlock();

            byte[] cipherBytes = memoryStream.ToArray();
            memoryStream.Close();
            csStream.Close();

            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);

            return cipherText;
        }

        public static string Decrypt(string text, byte[] key, byte[] iv)
        {
            using var AES = Aes.Create("AesCryptoServiceProvider");
            AES!.Key = key;
            AES.IV = iv;

            using MemoryStream memoryStream = new MemoryStream();
            using var csStream = new CryptoStream(memoryStream
                , AES.CreateDecryptor()
                , CryptoStreamMode.Write);

            string plainText = String.Empty;

            byte[] cipherBytes = Convert.FromBase64String(text);
            var s = System.Text.Encoding.UTF8.GetString(cipherBytes);

            csStream.Write(cipherBytes, 0, cipherBytes.Length);

            csStream.FlushFinalBlock();

            byte[] plainBytes = memoryStream.ToArray();

            plainText = Encoding.UTF8.GetString(plainBytes, 0, plainBytes.Length);

            memoryStream.Close();
            csStream.Close();

            return plainText;
        }
    }
}
