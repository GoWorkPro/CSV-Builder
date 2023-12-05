using Microsoft.Win32;
using System;

namespace GoWorkPro.CsvBuilder.LicenseManager
{
    internal class LicenseManager : ILicenseManager
    {
        IEncryption _encryption;
        private LicenseManager()
        {
            _encryption = new Encryption();
        }
        private const string RegistryKeyPath = @"SOFTWARE\GoWorkPro\CsvBuilder";
        private const string StartDateValueName = "StartDate";
        // Store the start date or other sensitive information in the Registry
        public void StoreStartDateIfNotExists(DateTime startDate)
        {
            if (GetStartDate() == null)
            {
                using (var registryKey = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    registryKey?.SetValue(StartDateValueName, _encryption.Encrypt(startDate.ToString("yyyy-MM-dd")), RegistryValueKind.String);
                }
            }
        }

        // Retrieve the start date or other sensitive information from the Registry
        public DateTime? GetStartDate()
        {
            using (var registryKey = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, RegistryKeyPermissionCheck.ReadSubTree))
            {
                if (registryKey != null)
                {
                    var startDateValue = registryKey.GetValue(StartDateValueName) as string;

                    if (!string.IsNullOrEmpty(startDateValue))
                    {
                        startDateValue = _encryption.Decrypt(startDateValue);
                        if (DateTime.TryParse(startDateValue, out var result))
                        {
                            return result;
                        }
                    }
                }
            }

            return null;
        }

        public bool ValidLicense()
        {
            DateTime? startDate = GetStartDate();
            if (!startDate.HasValue)
            {
                this.StoreStartDateIfNotExists(DateTime.Now);
                return true;
            }
            else if (startDate.Value.AddDays(15) < DateTime.Now)
            {
                return false;
            }
            else
                return true;
        }
    }
}
