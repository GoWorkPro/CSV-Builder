using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    internal interface IEncryption
    {
        string Decrypt(string protectedData);
        string Encrypt(string data);
    }
}
