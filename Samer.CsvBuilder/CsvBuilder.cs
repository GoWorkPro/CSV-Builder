using System.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace GoWorkPro.CsvBuilder
{
    /// <summary>
    /// <author>Samer Shahbaz</author>
    /// <createDate>15/11/2023</createDate>
    /// <description>This utility is developed to build CSVs using DataTables.</description>
    /// <email>info@goworkpro.com</email>
    /// </summary>
    public class CsvBuilder : ICsvExtractor, ICsvBuilder
    {
        readonly DataSet _dataset;
        readonly MemoryStream _stream;
        private StreamWriter _streamWriter { set; get; }
        public delegate string ValueParser(string value, ValueType type, int column, int row, int tableIndex, int actualRow);
        bool _isBuild;
        /// <summary>
        /// Gets or sets the value rendering event. Must be set before calling the Build method.
        /// </summary>
        public event ValueParser? ValueRenderEvent
        {
            add
            {
                if (_isBuild)
                {
                    throw new InvalidOperationException("ValueRenderEvent must be set before calling either the Build method or ReadFile/ReadFileWhile.");
                }
                _valueRenderEvent += value;
            }
            remove
            {
                _valueRenderEvent -= value;
            }
        }

        private ValueParser? _valueRenderEvent;

        private CsvBuilder(DataSet dataset)
        {
            _dataset = dataset;
            _stream = new MemoryStream();
            _streamWriter = new StreamWriter(_stream);
        }

        public static ICsvBuilder Datasets(params DataTable[] dataTables)
        {
            var reArrangedDataset = new DataSet();
            foreach (var table in dataTables)
            {
                var clonedTable = table.Clone();
                clonedTable.Merge(table);
                reArrangedDataset.Tables.Add(clonedTable);
            }
            return new CsvBuilder(reArrangedDataset);
        }

        public static ICsvBuilder Datasets(DataSet dataSet)
        {
            if (dataSet.Tables.Count == 0)
                throw new InvalidOperationException("Atleast one Datatable is required in dataset to build.");
            return new CsvBuilder(dataSet);
        }

        public static ICsvBuilder Datasets(params List<string>[] rows)
        {
            DataSet dataSet = new DataSet();
            int maxColumns = (from x in rows
                              select x.Count into x
                              orderby x descending
                              select x).FirstOrDefault();
            DataTable dataTable = new DataTable();
            for (int i = 0; i < maxColumns; i++)
            {
                dataTable.Columns.Add(new DataColumn("column" + i + 1));
            }

            foreach (List<string> source in rows)
            {
                DataRowCollection rows2 = dataTable.Rows;
                object[] values = source.Select((string x) => x).ToArray();
                rows2.Add(values);
            }

            dataSet.Tables.Add(dataTable);
            return new CsvBuilder(dataSet);
        }

        public ICsvExtractor Build(params int[] columnsTobePresentedForTableIndex)
        {
            _clearStream();
            this._isBuild = true;
            var tableIndex = 0;
            var actualRow = 1;
            foreach (DataTable dataTable in _dataset.Tables)
            {
                var rowNumber = 1;
                if (columnsTobePresentedForTableIndex.Contains(tableIndex))
                {
                    var columns = new List<string>();
                    var columnNumber = 1;
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (_valueRenderEvent != null)
                            columns.Add(_valueRenderEvent(column.ColumnName, ValueType.column, columnNumber, rowNumber, tableIndex, actualRow));
                        else
                            columns.Add(column.ColumnName);
                        columnNumber++;
                    }
                    // Concatenate each column value with a comma
                    _streamWriter.WriteLine(string.Join(",", columns));
                    rowNumber++;
                    actualRow++;
                }

                foreach (DataRow row in dataTable.Rows)
                {
                    var rowValues = new List<string>();
                    var columnNumber = 1;
                    foreach (var cellValue in row.ItemArray)
                    {
                        if (_valueRenderEvent != null)
                        {
                            rowValues.Add(_valueRenderEvent(Convert.ToString(cellValue), ValueType.row, columnNumber, rowNumber, tableIndex, actualRow));
                        }
                        else
                        {
                            rowValues.Add(Convert.ToString(cellValue));
                        }
                        columnNumber++;
                    }
                    _streamWriter.WriteLine(string.Join(",", rowValues));
                    rowNumber++;
                    actualRow++;
                }
                tableIndex++;
            }

            return this;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _stream?.Dispose();
        }

        public MemoryStream GetStream() { _datasetsToStream(); _streamWriter.Flush(); _streamWriter.BaseStream.Position = 0; return (MemoryStream)_streamWriter.BaseStream; }

        private void _datasetsToStream()
        {
            if (!_isBuild)
                Build();
        }

        public void _clearStream()
        {
            _streamWriter.Flush();
            _streamWriter.BaseStream.SetLength(0);
            _streamWriter.BaseStream.Position = 0;
        }

        public void SaveAsFile(string filePath)
        {
            _datasetsToStream();
            // Save the stream content to a file
            using FileStream fileStream = File.Create(filePath);
            _streamWriter.Flush();
            _streamWriter.BaseStream.Position = 0;
            _streamWriter.BaseStream.CopyTo(fileStream);
        }

        public static ICsvExtractor ReadFile(string filePath)
        {
            var csvData = File.ReadAllLines(filePath);
            var dataset = new DataSet();

            if (csvData.Length > 0)
            {
                var dataTable = new DataTable();

                for (var i = 1; i <= csvData.Length; i++)
                {
                    var rowValues = csvData[i - 1].Split(',');

                    // Adjust the number of columns if necessary
                    while (rowValues.Length > dataTable.Columns.Count)
                    {
                        dataTable.Columns.Add(new DataColumn($"column{dataTable.Columns.Count + 1}"));
                    }

                    // Truncate or pad the row values to match the number of columns
                    Array.Resize(ref rowValues, dataTable.Columns.Count);

                    var row = dataTable.NewRow();
                    row.ItemArray = rowValues;
                    dataTable.Rows.Add(row);
                }
                dataset.Tables.Add(dataTable);
            }
            return new CsvBuilder(dataset);
        }

        /// <summary>
        /// Reads a CSV file and constructs a DataSet based on specified criteria.
        /// The method allows reading the file row by row until a user-defined condition is met.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to be read.</param>
        /// <param name="readTillCriteria">A lambda expression defining the condition to continue reading rows.</param>
        /// <returns>An ICsvExtractor representing the constructed DataSet based on the file content.</returns>
        public static ICsvExtractor ReadFileWhile(string filePath, Func<ReadCriteria, bool> readTillCriteria)
        {
            // Read all lines from the CSV file
            var csvData = File.ReadAllLines(filePath);

            var dataset = new DataSet();
            var isBreaked = false;

            if (csvData.Length > 0)
            {
                var dataTable = new DataTable();

                // Iterate through each line in the CSV data
                for (var i = 1; i <= csvData.Length; i++)
                {
                    var rowValues = csvData[i - 1].Split(',');

                    // Create criteria with the current row number
                    var criteria = new ReadCriteria { RowNumber = i };

                    // Adjust the number of columns if necessary
                    while (rowValues.Length > dataTable.Columns.Count)
                    {
                        dataTable.Columns.Add(new DataColumn($"column{dataTable.Columns.Count + 1}"));
                    }

                    var rowCells = new string[rowValues.Length];

                    // Iterate through each column in the current row
                    for (int columnNumber = 1; columnNumber <= rowValues.Length; columnNumber++)
                    {
                        var value = rowValues[columnNumber - 1];
                        rowCells[columnNumber - 1] = value;

                        // Set the current cell value in the criteria for potential user-defined conditions
                        criteria.Value = value;
                    }

                    // Add the row to the DataTable if there are cell values
                    if (rowCells.Length > 0)
                    {
                        var row = dataTable.NewRow();
                        row.ItemArray = rowCells;
                        dataTable.Rows.Add(row);
                    }

                    // Check the user-defined condition for breaking the loop
                    if (readTillCriteria.Invoke(criteria))
                    {
                        isBreaked = true;
                    }

                    // Break the loop if the condition is met
                    if (isBreaked)
                        break;
                }

                // Add the DataTable to the DataSet
                dataset.Tables.Add(dataTable);
            }

            // Return an ICsvExtractor representing the constructed DataSet
            return new CsvBuilder(dataset);
        }
        public DataTable[] ToDataTables(KeyValuePair<Func<ReadCriteria, bool>, Func<ReadCriteria, bool>>[] startAndEndCriterias)
        {
            var dataTables = new List<DataTable>();

            if (this._dataset.Tables.Count <= 0)
            {
                return Array.Empty<DataTable>();
            }

            var tableToRead = _dataset.Tables[0];

            foreach (var startEndCriteria in startAndEndCriterias)
            {
                var startCriteria = startEndCriteria.Key;
                var endCriteria = startEndCriteria.Value;

                var dataTable = new DataTable();
                var isReading = false;
                var isBreaked = false;

                // Loop through rows
                for (int rowNumber = 1; rowNumber <= tableToRead.Rows.Count; rowNumber++)
                {
                    var rowValues = tableToRead.Rows[rowNumber - 1].ItemArray;
                    var cellValues = new string[rowValues.Length];

                    for (int columnNumber = 1; columnNumber <= rowValues.Length; columnNumber++)
                    {
                        var cellValue = rowValues[columnNumber - 1];
                        var readCriteria = new ReadCriteria { RowNumber = rowNumber, Value = Convert.ToString(cellValue) };

                        // Check if the start condition is met
                        if (!isReading && startCriteria.Invoke(readCriteria))
                        {
                            isReading = true;
                        }

                        if (isReading)
                        {
                            cellValues.Append(cellValue);
                        }

                        // Check if the end condition is met
                        if (isReading && endCriteria.Invoke(readCriteria))
                        {
                            isReading = false;
                            isBreaked = true;
                            break; // Exit the loop once end condition is met
                        }
                    }

                    if (cellValues.Length > 0)
                    {
                        var row = dataTable.NewRow();
                        row.ItemArray = cellValues;
                        dataTable.Rows.Add(row);
                    }

                    if (isBreaked)
                        break;
                }

                dataTables.Add(dataTable);
            }

            return dataTables.ToArray();
        }

        public void SetValue<T>(int columnNumber, int rowNumber, T value)
        {
            if (columnNumber <= 0 || rowNumber <= 0)
                throw new InvalidOperationException("Column Number or Row Number value should start from 1.");
            var dataTable = _dataset.Tables[0];
            dataTable.Rows[rowNumber - 1][columnNumber - 1] = value;
            this._isBuild = false;
        }

        public T GetValue<T>(int columnNumber, int rowNumber)
        {

            if (columnNumber <= 0 || rowNumber <= 0)
                throw new InvalidOperationException("Column Number or Row Number value should start from 1.");
            var dataTable = _dataset.Tables[0];
            return (T)dataTable.Rows[rowNumber - 1][columnNumber - 1];
        }

        public object[] GetRowValues(int rowNumber)
        {
            if (rowNumber <= 0)
                throw new InvalidOperationException("Row Number value should start from 1.");
            var dataTable = _dataset.Tables[0];
            return dataTable.Rows[rowNumber - 1].ItemArray;
        }

        public void SetRow(int rowNumber, object[] values)
        {
            if (rowNumber <= 0)
                throw new InvalidOperationException("Row Number value should start from 1.");

            var dataTable = _dataset.Tables[0];

            // Truncate or pad the values array to match the number of columns
            Array.Resize(ref values, dataTable.Columns.Count);

            dataTable.Rows[rowNumber - 1].ItemArray = values;
        }


    }
}