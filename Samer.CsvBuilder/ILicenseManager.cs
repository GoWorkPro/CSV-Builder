using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    internal interface ILicenseManager
    {
        void StoreStartDateIfNotExists(DateTime startDate);
        DateTime? GetStartDate();
        bool ValidLicense();
    }
}
