using System.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.ComponentModel.Design;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;

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
        private readonly Options _options;

        private StreamWriter _streamWriter { set; get; }
        private StreamReader? _streamReader { get; set; }

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

        private CsvBuilder()
        {
            _dataset = new DataSet();
            _options = new Options();
            _stream = new MemoryStream();
            _streamWriter = new StreamWriter(_stream);
        }

        private CsvBuilder(DataSet dataset) : this()
        {
            _dataset = dataset;
        }

        private CsvBuilder(Options options, params DataTable[] tables) : this()
        {
            _dataset.Tables.AddRange(tables);
            this._options = options;
        }

        /// <summary>
        /// Creates a CSV builder from an array of DataTables.
        /// </summary>
        /// <param name="dataTables">An array of DataTable instances representing the data to be included in the CSV builder.</param>
        /// <returns>An instance of <see cref="ICsvBuilder"/> configured with the provided DataTables.</returns>
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

        /// <summary>
        /// Creates a CSV builder from a DataSet.
        /// </summary>
        /// <param name="dataSet">A DataSet containing DataTables representing the data to be included in the CSV builder.</param>
        /// <returns>An instance of <see cref="ICsvBuilder"/> configured with the DataTables from the provided DataSet.</returns>
        public static ICsvBuilder Datasets(DataSet dataSet)
        {
            if (dataSet.Tables.Count == 0)
                throw new InvalidOperationException("Atleast one Datatable is required in dataset to build.");
            return new CsvBuilder(dataSet);
        }

        /// <summary>
        /// Creates a CSV builder from an array of lists of strings.
        /// </summary>
        /// <param name="rows">An array of lists of strings representing the data to be included in the CSV builder.</param>
        /// <returns>An instance of <see cref="ICsvBuilder"/> configured with the provided lists of strings.</returns>
        public static ICsvBuilder Datasets(params List<string>[] rows)
        {
            return Datasets(new Options(), rows);
        }

        /// <summary>
        /// Creates a CSV builder from an array of lists of strings with the specified options for processing.
        /// </summary>
        /// <param name="options">Options specifying how the CSV data should be handled during building.</param>
        /// <param name="rows">An array of lists of strings representing the data to be included in the CSV builder.</param>
        /// <returns>An instance of <see cref="ICsvBuilder"/> configured with the provided options and lists of strings.</returns>
        public static ICsvBuilder Datasets(Options options, params List<string>[] rows)
        {
            var rowsAfterSperators = rows.Select(x => string.Join(options.Separator, x)).Where(x => options.RemoveEmptyRows && !string.IsNullOrWhiteSpace(x) || !options.RemoveEmptyRows).Skip(options.SkipInitialNumberOfRows).ToArray();
            return new CsvBuilder(options, parseIntoTable(options, rowsAfterSperators));
        } 

        public ICsvExtractor Build(params int[] columnsTobePresentedForTableIndex)
        {
            _clearStream();
            this._isBuild = true;
            var tableIndex = 0;
            var actualRow = 1;
            foreach (DataTable dataTable in _dataset.Tables)
            {
                //if (tableIndex > 0)
                //    _streamWriter.Write($"////////(Table Number {tableIndex + 1} Start)////////" + _options.NewLine);

                var rowNumber = 1;
                if (columnsTobePresentedForTableIndex.Contains(tableIndex) || _options.HeaderMode == HeaderMode.HeaderPresent)
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
                    _streamWriter.Write(string.Join(_options.Separator, columns) + _options.NewLine);
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
                    _streamWriter.Write(string.Join(_options.Separator, rowValues) + _options.NewLine);
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
            _streamReader = null;
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

        /// <summary>
        /// Reads CSV data from a file with the specified options for processing.
        /// </summary>
        /// <param name="filePath">The path to the file containing the CSV data.</param>
        /// <param name="options">Options specifying how the CSV data should be handled during extraction.</param>
        /// <returns>An instance of <see cref="ICsvExtractor"/> configured based on the CSV data from the file and options.</returns>
        public static ICsvExtractor ReadFile(string filePath, Options options)
        {
            var csvData = File.ReadAllText(filePath);
            return ReadFromText(csvData, options);
        }

        /// <summary>
        /// Reads a CSV file and constructs a DataSet based on specified criteria.
        /// The method allows reading the file row by row until a user-defined condition is met.
        /// </summary>
        /// <param name="filePath">The path to the CSV file to be read.</param>
        /// <param name="readTillCriteria">A lambda expression defining the condition to continue reading rows.</param>
        /// <returns>An ICsvExtractor representing the constructed DataSet based on the file content.</returns>
        public static ICsvExtractor ReadFileTill(string filePath, Func<ReadCriteria, bool> readTillCriteria, Options options)
        {
            // Read all lines from the CSV file
            var csvData = File.ReadAllText(filePath);
            var dataset = new DataSet();
            var isBreaked = false;
            var dataTable = new DataTable();

            string[] rows = csvData.Split(new[] { options.NewLine }, options.RemoveEmptyRows ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None).Skip(options.SkipInitialNumberOfRows).ToArray();

            // Determine the starting index based on HeaderMode
            int startIndex = options.HeaderMode == HeaderMode.HeaderPresent ? 1 : 0;

            // Process the header row if HeaderMode is HeaderPresent
            if (options.HeaderMode == HeaderMode.HeaderPresent && rows.Length > 0)
            {
                // Split the header row into cells based on the separator
                string[] headerCells = rows[0].Split(options.Separator);

                // Add columns to the DataTable based on the header cells
                foreach (string headerCell in headerCells)
                {
                    dataTable.Columns.Add(headerCell.Trim());
                }
            }


            // Iterate through each line in the CSV data
            for (var rowNumber = startIndex; rowNumber < rows.Length; rowNumber++)
            {
                var rowValues = rows[rowNumber].Split(options.Separator);

                // Create criteria with the current row number
                var criteria = new ReadCriteria() { RowNumber = rowNumber + 1 };

                // Adjust the number of columns if necessary
                while (rowValues.Length > dataTable.Columns.Count)
                {
                    dataTable.Columns.Add(new DataColumn($"Column {dataTable.Columns.Count + 1}"));
                }

                var rowCells = new string[rowValues.Length];
                if (readTillCriteria.Invoke(criteria))
                {
                    isBreaked = true;
                    break;
                }
                // Iterate through each column in the current row
                for (int columnNumber = 1; columnNumber <= rowValues.Length; columnNumber++)
                {
                    var value = rowValues[columnNumber - 1];
                    rowCells[columnNumber - 1] = value;

                    // Set the current cell value in the criteria for potential user-defined conditions
                    criteria.Value = value;
                    {
                        // Check the user-defined condition for breaking the loop
                        if (readTillCriteria.Invoke(criteria))
                        {
                            isBreaked = true;
                            break;
                        }
                    }
                }

                // Add the row to the DataTable if there are cell values
                if (rowCells.Length > 0)
                {
                    // Optionally trim data
                    if (options.TrimData)
                    {
                        rowCells = rowCells.Select(cell => cell.Trim()).ToArray();
                    }

                    // Check if the row should be skipped based on the SkipRow delegate
                    if (options.SkipRow != null && options.SkipRow.Invoke(string.Join(options.Separator, rowCells), rowNumber))
                    {
                        continue; // Skip the current row
                    }

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
            // Return an ICsvExtractor representing the constructed DataSet
            return new CsvBuilder(dataset);
        }

        public DataTable[] ToDataTables(params StartEndCriteria[] startAndEndCriterias)
        {
            return this.ToDataTables(false, startAndEndCriterias);
        }

        public DataTable[] ToDataTables(bool skipMatchCriteriaValue, params StartEndCriteria[] startAndEndCriterias)
        {
            var dataTables = new List<DataTable>();

            if (this._dataset.Tables.Count <= 0)
            {
                return Array.Empty<DataTable>();
            }

            if (startAndEndCriterias.Length <= 0)
            {
                var tables = new DataTable[_dataset.Tables.Count];
                _dataset.Tables.CopyTo(tables, 0);
                return tables;
            }

            var tableToRead = _dataset.Tables[0];

            var lockObject = new object();
            Parallel.ForEach(startAndEndCriterias, (startEndCriteria) =>
            {
                var startCriteria = startEndCriteria.StartCriteria;
                var endCriteria = startEndCriteria.EndCriteria;

                var dataTable = new DataTable();
                var isReading = false;
                var isBreaked = false;

                // Loop through rows
                for (int rowNumber = 1; rowNumber <= tableToRead.Rows.Count; rowNumber++)
                {
                    var rowValues = tableToRead.Rows[rowNumber - 1].ItemArray;
                    var cellValues = new string[rowValues.Length];
                    var readCriteria = new ReadCriteria
                    {
                        RowNumber = rowNumber
                    };

                    while (rowValues.Length > dataTable.Columns.Count)
                    {
                        dataTable.Columns.Add(new DataColumn($"Column {dataTable.Columns.Count + 1}"));
                    }

                    if (startCriteria == null || startCriteria.Invoke(readCriteria))
                        isReading = true;

                    for (int columnNumber = 1; columnNumber <= rowValues.Length; columnNumber++)
                    {
                        var cellValue = rowValues[columnNumber - 1];

                        readCriteria.Value = Convert.ToString(cellValue);

                        // Check if the start condition is met
                        if (startCriteria == null || (!isReading && startCriteria.Invoke(readCriteria)))
                        {
                            isReading = true;
                        }

                        if (isReading)
                        {
                            cellValues[columnNumber - 1] = Convert.ToString(cellValue);
                        }

                        // Check if the end condition is met
                        if (endCriteria != null && isReading && endCriteria.Invoke(readCriteria))
                        {
                            isReading = false;
                            isBreaked = true;
                            break; // Exit the loop once end condition is met
                        }
                    }

                    if (cellValues.Length > 0)
                    {
                        var skipingValueCriteria = new ReadCriteria { RowNumber = readCriteria.RowNumber, Value = cellValues.FirstOrDefault() };
                        if (startCriteria != null && (startCriteria.Invoke(skipingValueCriteria) || endCriteria != null && endCriteria.Invoke(skipingValueCriteria)) && skipMatchCriteriaValue)
                        {
                        }
                        else if (isReading)
                        {
                            var row = dataTable.NewRow();
                            row.ItemArray = cellValues;
                            dataTable.Rows.Add(row);
                        }
                    }

                    // Check if the end condition is met
                    if (endCriteria != null && isReading && endCriteria.Invoke(readCriteria))
                    {
                        isReading = false;
                        isBreaked = true;
                        break; // Exit the loop once end condition is met
                    }

                    if (isBreaked)
                        break;
                }

                lock (lockObject)
                {
                    dataTables.Add(dataTable);
                }
            });

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
            this._isBuild = false;
        }

        /// <summary>
        /// Returns a string representation of the CSV extractor.
        /// </summary>
        /// <returns>A string representing the CSV extractor and its current state.</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            _streamReader = new StreamReader(GetStream());
            while (!_streamReader.EndOfStream)
            {
                var line = _streamReader.ReadLine();
                stringBuilder.AppendLine(line);
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the contents of an Excel file to a CSV extractor.
        /// </summary>
        /// <param name="excelFilePath">The path to the Excel file to be converted.</param>
        /// <returns>An instance of <see cref="ICsvExtractor"/> containing the data from the Excel file in CSV format.</returns>
        public static ICsvExtractor ReadExcelFileToCsv(string excelFilePath)
        {
            var dataset = new DataSet();
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                foreach (var worksheet in workbook.Worksheets)
                {
                    var dataTable = new DataTable();
                    var range = worksheet.RangeUsed();
                    foreach (var row in range.RowsUsed())
                    {
                        var values = row.CellsUsed().Select(cell => cell.GetString()).ToArray();

                        while (values.Length > dataTable.Columns.Count)
                        {
                            dataTable.Columns.Add(new DataColumn($"Column {dataTable.Columns.Count + 1}"));
                        }

                        // Truncate or pad the row values to match the number of columns
                        Array.Resize(ref values, dataTable.Columns.Count);

                        var dataTableRow = dataTable.NewRow();
                        dataTableRow.ItemArray = values;
                        dataTable.Rows.Add(dataTableRow);
                    }

                    dataset.Tables.Add(dataTable);
                }
                return new CsvBuilder(dataset);
            }
        }

        /// <summary>
        /// Creates a CSV extractor from a CSV-formatted string without any specific processing options.
        /// </summary>
        /// <param name="csv">The CSV-formatted string to be processed.</param>
        /// <returns>An instance of <see cref="ICsvExtractor"/> with default options based on the provided CSV data.</returns>
        public static ICsvExtractor ReadFromText(string csv)
        {
            return ReadFromText(csv, new Options());
        }

        /// <summary>
        /// Creates a CSV extractor from a CSV-formatted string with the specified options for processing.
        /// </summary>
        /// <param name="csvData">The CSV-formatted string to be processed.</param>
        /// <param name="options">Options specifying how the CSV data should be handled during extraction.</param>
        /// <returns>An instance of <see cref="ICsvExtractor"/> configured based on the provided CSV data and options.</returns>
        public static ICsvExtractor ReadFromText(string csvData, Options options)
        {
            // Split the CSV string into rows
            string[] rows = csvData.Split(new[] { options.NewLine }, options.RemoveEmptyRows ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None).Skip(options.SkipInitialNumberOfRows).ToArray();
            return new CsvBuilder(options, parseIntoTable(options, rows));
        }

        static DataTable parseIntoTable(Options options, string[] rows)
        {
            // Create a DataTable to hold the CSV data
            DataTable dataTable = new DataTable();

            // Determine the starting index based on HeaderMode
            int startIndex = options.HeaderMode == HeaderMode.HeaderPresent ? 1 : 0;

            // Process the header row if HeaderMode is HeaderPresent
            if (options.HeaderMode == HeaderMode.HeaderPresent && rows.Length > 0)
            {
                // Split the header row into cells based on the separator
                string[] headerCells = rows[0].Split(options.Separator);

                // Add columns to the DataTable based on the header cells
                foreach (string headerCell in headerCells)
                {
                    dataTable.Columns.Add(headerCell.Trim());
                }
            }

            // Process the data rows and populate the DataTable
            for (int i = startIndex; i < rows.Length; i++)
            {
                // Split the row into cells based on the separator
                string[] cells = rows[i].Split(options.Separator);

                while (cells.Length > dataTable.Columns.Count)
                {
                    dataTable.Columns.Add(new DataColumn($"Column {dataTable.Columns.Count + 1}"));
                }

                // Truncate or pad the row values to match the number of columns
                Array.Resize(ref cells, dataTable.Columns.Count);

                // Optionally trim data
                if (options.TrimData)
                {
                    cells = cells.Select(cell => cell.Trim()).ToArray();
                }

                // Check if the row should be skipped based on the SkipRow delegate
                if (options.SkipRow != null && options.SkipRow.Invoke(string.Join(options.Separator, cells), i))
                {
                    continue; // Skip the current row
                }

                // Add a new row to the DataTable and fill it with the cell values
                DataRow dataRow = dataTable.NewRow();
                dataRow.ItemArray = cells;
                dataTable.Rows.Add(dataRow);
            }
            return dataTable;
        }
    }
}