using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Packer
{
    internal class Program
    {
        private static void GenerateCertificate()
        {
            using RSA rsa = RSA.Create();
            CertificateRequest req = new("cn=NoIPChat", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            X509Certificate2 cert = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(10));
            byte[] certData = cert.Export(X509ContentType.Pfx);
            System.IO.File.WriteAllBytes("NoIPChat.pfx", certData);
            RSA? publicKey = cert.GetRSAPublicKey();
            if (publicKey != null)
            {
                RSAParameters rsaParams = publicKey.ExportParameters(false);
                if (rsaParams.Modulus != null && rsaParams.Exponent != null)
                {
                    string modulusBase64 = Convert.ToBase64String(rsaParams.Modulus);
                    string exponentBase64 = Convert.ToBase64String(rsaParams.Exponent);
                    Console.WriteLine("Modulus (Base64): " + modulusBase64);
                    Console.WriteLine("Exponent (Base64): " + exponentBase64);
                }
            }
        }
        private static void SignAndPack(string pathsc, string path)
        {
            X509Certificate2 cert = new("NoIPChat.pfx");
            var rsaPrivateKey = cert.GetRSAPrivateKey();
            if (rsaPrivateKey != null)
            {
                string[] files = Directory.GetFiles(path);
                byte[] signatures = new byte[256 * files.Length];
                int i = 0;
                string name = "Packet.nip";
                List<string> files1 = [];
                List<string> existing = [];
                foreach(string filesc in Directory.GetFiles(pathsc))
                {
                    if (filesc.Contains(".dll"))
                    {
                        existing.Add(Path.GetFileName(filesc));
                    }
                }
                foreach (string file in files)
                {
                    if (file != "sign" && !file.Contains(".nip") && !existing.Contains(Path.GetFileName(file)))
                    {
                        if (file.Contains(".dll"))
                        {
                            name = Path.GetFileNameWithoutExtension(file) + ".nip";
                        }
                        using (FileStream fs = new(file, FileMode.Open))
                        {
                            using SHA256 sha256 = SHA256.Create();
                            byte[] hash = sha256.ComputeHash(fs);
                            byte[] signature = rsaPrivateKey.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                            signature.CopyTo(signatures, i * 256);
                        }
                        i += 1;
                        files1.Add(file);
                    }
                }
                File.WriteAllBytes(Path.Combine(path, "sign"), signatures);
                files1.Add(Path.Combine(path, "sign"));
                ZipFiles([.. files1], name, path);
            }
        }
        static void ZipFiles(string[] filesToZip, string zipFileName, string directoryPath)
        {
            using FileStream zipToOpen = new(Path.Combine(directoryPath, zipFileName), FileMode.Create);
            using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
            foreach (string file in filesToZip)
            {
                string filePath = Path.Combine(directoryPath, file);
                if (File.Exists(filePath))
                {
                    string fileName = Path.GetFileName(file);
                    archive.CreateEntryFromFile(filePath, fileName);
                }
                else
                {
                    Console.WriteLine($"File '{file}' does not exist.");
                }
            }
        }

        private static List<byte[]> SplitByteArray(byte[] byteArray, int chunkSize)
        {
            return Enumerable.Range(0, (byteArray.Length + chunkSize - 1) / chunkSize)
                             .Select(i => byteArray.Skip(i * chunkSize).Take(chunkSize).ToArray())
                             .ToList();
        }
        private static bool Verify(string path)
        {
            List<bool> check = [];
            RSAParameters publicKeyParams = new()
            {
                Modulus = Convert.FromBase64String("t6y4eIkpe/HlTDHdmPT1D7mjqZsfXSu9ffl7oTx0w3dOGIILPg9p+0Ygbk2mI4rLAE6lvDG/msO6SHoykAhMpErLsP/r0Aie3bXecMQkGaPSFIXISms4IkZ89wW7FRb4960LrmUMxo5lIeL3yrRiMhl5aJ8h1sJ3V+1AM8Mfa0wQIHabqEJfifky+jM8nISWmu4INvgCBQpq/SVDgufNMC43Z2LS3G6Q6CBWgRGEqFq1kdgCu3lFwJ9H/9EqXXYES9f/n0VN4djcEcy4kEDmMEy8xbni97II1Lz70l1624wYNg00YWYOYVE5PTszufnZURdZtNTgHfymuD8neVgfwQ=="),
                Exponent = Convert.FromBase64String("AQAB")
            };
            RSACryptoServiceProvider rsaPublicKey = new();
            rsaPublicKey.ImportParameters(publicKeyParams);
            byte[] signatures1 = File.ReadAllBytes(Path.Combine(path, "sign"));
            List<byte[]> signatures = SplitByteArray(signatures1, 256);
            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                byte[] fileHash;
                using (FileStream fs = new(file, FileMode.Open))
                {
                    using SHA256 sha256 = SHA256.Create();
                    fileHash = sha256.ComputeHash(fs);
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
        }
        static void Main(string[] args)
        {
            string? pathsc;
            string? path;
            if (args.Length > 0)
            {
                //Ignore other
                pathsc = args[0];
                path = args[1];
            }
            else
            {
                Console.WriteLine("Path to Server or Client");
                pathsc = Console.ReadLine();
                Console.Write("Path to directory:");
                path = Console.ReadLine();
            }
            if (!string.IsNullOrEmpty(pathsc) && !string.IsNullOrEmpty(path))
            {
                SignAndPack(Path.GetFullPath(pathsc), Path.GetFullPath(path));
            }
            Console.Write("Press any key to exit.");
            Console.ReadLine();
        }
    }
}
