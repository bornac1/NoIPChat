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
            return message;
        }
        public static Message DecryptMessage(Message message, byte[] aeskey)
        {
            if (message.Nounce != null)
            {
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
            }
            return message;
        }
    }
}
