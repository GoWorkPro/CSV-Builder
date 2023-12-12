using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoWorkPro.CsvBuilder
{
    public interface ICsvExtractor : IDisposable
    {
        MemoryStream GetStream();
        void SaveAsFile(string filePath);
    }
}
