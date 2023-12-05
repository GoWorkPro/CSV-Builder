using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    internal class Encryption: IEncryption
    {
        public string Encrypt(string data)
        {
            
            byte[] userData = Encoding.Unicode.GetBytes(data);
            byte[] encryptedData = ProtectedData.Protect(userData, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public string Decrypt(string protectedData)
        {
            byte[] encryptedData = Convert.FromBase64String(protectedData);
            byte[] userData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.Unicode.GetString(userData);
        }
    }
}

