using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Tools;
internal static class SimpleEncrypt
{
    public static byte[] Encrypt(string content)
    {
        var contentBytes = Encoding.UTF8.GetBytes(content);

        using var aes = Aes.Create();
        using var output = new MemoryStream();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateKey();
        aes.GenerateIV();

        output.Write(aes.IV);
        output.Write(aes.Key);

        var encryptor = aes.CreateEncryptor();

        using (var stream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
        {
            stream.Write(contentBytes);
        }

        return output.ToArray();
    }

    public static string EncryptToBase64(string content) {
        var bytes = Encrypt(content);
        return Convert.ToBase64String(bytes);
    }
}
