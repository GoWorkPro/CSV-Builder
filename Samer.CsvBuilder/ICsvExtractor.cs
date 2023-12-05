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

        DataTable[] ToDataTables(KeyValuePair<Func<ReadCriteria, bool>, Func<ReadCriteria, bool>>[] startAndEndCriterias);

        /// <summary>
        /// It expects the column number and row number to modify value at specific point, column number and row number start At 1
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <param name="rowNumber"></param>
        /// <param name="value"></param>
        void SetValue<T>(int columnNumber, int rowNumber, T value);
        T GetValue<T>(int columnNumber, int rowNumber);
        object[] GetRowValues(int rowNumber);
        /// <summary>
        /// Update row at specific row number, row number start At 1
        /// </summary>
        /// <param name="rowNumber"></param>
        void SetRow(int rowNumber, object[] values);
    }
}
