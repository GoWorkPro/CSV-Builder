using System.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
namespace GoWorkPro.CsvBuilder
{
    /// <summary>
    /// <author>Samer Shahbaz</author>
    /// <createDate>15/11/2023</createDate>
    /// <description>This utility is developed to build CSVs using DataTables.</description>
    /// <email>extentions@goworkpro.com</email>
    /// </summary>
    public class CsvBuilder : ICsvExtractor, ICsvBuilder
    {
        readonly DataSet _dataset;
        readonly MemoryStream _stream;
        readonly StreamWriter _streamWriter;
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
                    throw new InvalidOperationException("ValueRenderEvent must be set before calling the Build method.");
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

        public static ICsvBuilder Datasets(params List<string>[] rows)
        {
            DataSet dataSet = new();
            int maxColumns = (from x in rows
                       select x.Count into x
                       orderby x descending
                       select x).FirstOrDefault();
            DataTable dataTable = new();
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
                    if (_valueRenderEvent != null)
                        _streamWriter.WriteLine(string.Join(",", columns));
                    else
                        _streamWriter.WriteLine(string.Join(",", columns.Select(value => $"\"{Convert.ToString(value).Replace("\"", "\"\"")}\"")));
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
                            rowValues.Add($"\"{Convert.ToString(cellValue).Replace("\"", "\"\"")}\"");
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
        public MemoryStream GetStream() { _streamWriter.Flush(); _stream.Position = 0; return _stream; }
        public void SaveAsFile(string filePath)
        {
            // Save the stream content to a file
            using FileStream fileStream = File.Create(filePath);
            _streamWriter.Flush();
            _stream.Position = 0;
            _stream.CopyTo(fileStream);
        }
    }
}