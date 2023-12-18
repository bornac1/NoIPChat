using MessagePack;
using Sodium;

namespace Messages
{
    public static class Encryption
    {
        public static KeyPair GenerateECDH()
        {
            return PublicKeyBox.GenerateKeyPair();
        }
        public static KeyPair GetECDH(byte[] key)
        {
            return PublicKeyBox.GenerateKeyPair(key);
        }
        public static byte[] GenerateAESKey(KeyPair my, byte[] publickey)
        {
            return ScalarMult.Mult(my.PrivateKey, publickey);
        }
        public static byte[] Encrypt(byte[] data, byte[] nounce, byte[] aeskey)
        {
            return SecretAeadAes.Encrypt(data, nounce, aeskey);
        }
        public static byte[] Decrypt(byte[] data, byte[] nounce, byte[] aeskey)
        {
            return SecretAeadAes.Decrypt(data, nounce, aeskey);
        }
        public static Message EncryptMessage(Message message, byte[] aeskey)
        {
            if (message.Pass != null || message.Msg != null || message.Data != null && message.Nounce == null)
            {
                message = CopyMessage(message);
                message.Nounce = SecretAeadAes.GenerateNonce();
                //Encrypt password
                if (message.Pass != null)
                {
                    message.Pass = Encrypt(message.Pass, message.Nounce, aeskey);
                }
                //Encrypt Msg
                if (message.Msg != null)
                {
                    message.Msg = Encrypt(message.Msg, message.Nounce, aeskey);
                }
                //Encrypt data
                if (message.Data != null)
                {
                    message.Data = Encrypt(message.Data, message.Nounce, aeskey);
                }
            }
            return message;
        }
        public static Message DecryptMessage(Message message, byte[] aeskey)
        {
            if (message.Nounce != null)
            {
                message = CopyMessage(message);
                //Decrypt password
                if (message.Pass != null)
                {
                    message.Pass = Decrypt(message.Pass, message.Nounce, aeskey);
                }
                //Decrypt Msg
                if (message.Msg != null)
                {
                    message.Msg = Decrypt(message.Msg, message.Nounce, aeskey);
                }
                //Decrypt data
                if (message.Data != null)
                {
                    message.Data = Decrypt(message.Data, message.Nounce, aeskey);
                }
                message.Nounce = null;
            }
            return message;
        }
        private static Message CopyMessage(Message message)
        {
            return new()
            {
                CV = message.CV,
                CVU = message.CVU,
                SV = message.SV,
                SVU = message.SVU,
                Update = message.Update,
                User = message.User,
                Users = message.Users,
                Pass = message.Pass,
                Name = message.Name,
                Auth = message.Auth,
                Disconnect = message.Disconnect,
                Server = message.Server,
                Sender = message.Sender,
                Receiver = message.Receiver,
                Msg = message.Msg,
                Data = message.Data,
                Nounce = message.Nounce,
                PublicKey = message.PublicKey,
                Hop = message.Hop,
            };
        }
    }
}
