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
        /// <summary>
        /// Gets a <see cref="MemoryStream"/> containing the processed CSV data.
        /// </summary>
        /// <returns>A <see cref="MemoryStream"/> containing the processed CSV data.</returns>
        public MemoryStream GetStream();

        /// <summary>
        /// Saves the processed CSV data to a file at the specified file path.
        /// </summary>
        /// <param name="filePath">The file path where the CSV data will be saved.</param>
        public void SaveAsFile(string filePath);

        /// <summary>
        /// Converts the processed CSV data into an array of <see cref="DataTable"/> instances based on provided criteria.
        /// </summary>
        /// <param name="startAndEndCriterias">An array of <see cref="StartEndCriteria"/> instances specifying criteria for extracting data.</param>
        /// <returns>An array of <see cref="DataTable"/> instances representing the processed CSV data.</returns>
        public DataTable[] ToDataTables(params StartEndCriteria[] startAndEndCriterias);

        /// <summary>
        /// Converts the processed CSV data into an array of <see cref="DataTable"/> instances based on provided criteria, with an option to skip rows matching the criteria values.
        /// </summary>
        /// <param name="skipMatchCriteriaValue">A boolean indicating whether to skip rows matching the criteria values.</param>
        /// <param name="startAndEndCriterias">An array of <see cref="StartEndCriteria"/> instances specifying criteria for extracting data.</param>
        /// <returns>An array of <see cref="DataTable"/> instances representing the processed CSV data.</returns>
        public DataTable[] ToDataTables(bool skipMatchCriteriaValue, params StartEndCriteria[] startAndEndCriterias);

        /// <summary>
        /// It expects the column number and row number to modify value at specific point, column number and row number start At 1
        /// </summary>
        /// <param name="columnNumber"></param>
        /// <param name="rowNumber"></param>
        /// <param name="value"></param>
        void SetValue<T>(int columnNumber, int rowNumber, T value);

        /// <summary>
        /// Retrieves the value at the specified column and row indices from the processed CSV data.
        /// </summary>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="columnNumber">The column index (zero-based) from which to retrieve the value.</param>
        /// <param name="rowNumber">The row index (zero-based) from which to retrieve the value.</param>
        /// <returns>The value at the specified column and row indices, converted to the specified type <typeparamref name="T"/>.</returns>
        T GetValue<T>(int columnNumber, int rowNumber);
        object[] GetRowValues(int rowNumber);
        /// <summary>
        /// Update row at specific row number, row number start At 1
        /// </summary>
        /// <param name="rowNumber"></param>
        void SetRow(int rowNumber, object[] values);
    }
}
