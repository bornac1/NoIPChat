using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public partial class Client
    {
        private static List<byte[]> SplitByteArray(byte[] byteArray, int chunkSize)
        {
            return Enumerable.Range(0, (byteArray.Length + chunkSize - 1) / chunkSize)
                             .Select(i => byteArray.Skip(i * chunkSize).Take(chunkSize).ToArray())
                             .ToList();
        }
        private bool Verify(string path)
        {
            try
            {
                List<bool> check = [];
                RSAParameters publicKeyParams = new RSAParameters
                {
                    Modulus = Convert.FromBase64String("t6y4eIkpe/HlTDHdmPT1D7mjqZsfXSu9ffl7oTx0w3dOGIILPg9p+0Ygbk2mI4rLAE6lvDG/msO6SHoykAhMpErLsP/r0Aie3bXecMQkGaPSFIXISms4IkZ89wW7FRb4960LrmUMxo5lIeL3yrRiMhl5aJ8h1sJ3V+1AM8Mfa0wQIHabqEJfifky+jM8nISWmu4INvgCBQpq/SVDgufNMC43Z2LS3G6Q6CBWgRGEqFq1kdgCu3lFwJ9H/9EqXXYES9f/n0VN4djcEcy4kEDmMEy8xbni97II1Lz70l1624wYNg00YWYOYVE5PTszufnZURdZtNTgHfymuD8neVgfwQ=="),
                    Exponent = Convert.FromBase64String("AQAB")
                };
                RSACryptoServiceProvider rsaPublicKey = new RSACryptoServiceProvider();
                rsaPublicKey.ImportParameters(publicKeyParams);
                byte[] signatures1 = File.ReadAllBytes(Path.Combine(path, "sign"));
                List<byte[]> signatures = SplitByteArray(signatures1, 256);
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    byte[] fileHash;
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        using (SHA256 sha256 = SHA256.Create())
                        {
                            fileHash = sha256.ComputeHash(fs);
                        }
                    }
                    foreach (byte[] signature in signatures)
                    {
                        if (rsaPublicKey.VerifyHash(fileHash, "SHA256", signature))
                        {
                            //Found
                            check.Add(true);
                            break;
                        }
                    }
                }
                bool check1 = true;
                foreach (bool c in check)
                {
                    if (!c)
                    {
                        check1 = false;
                        break;
                    }
                }
                return check1;
            } catch (Exception ex)
            {
                WriteLog(ex).Wait();
            }
            return false;
        }
    }
}
