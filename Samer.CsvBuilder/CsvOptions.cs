﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public class Options
    {
        /// <summary>
        /// Gets or sets the number of initial rows to skip while processing the CSV data.
        /// </summary>
        public int SkipInitialNumberOfRows { get; set; } = 0;

        /// <summary>
        /// Gets or sets a delegate function to determine whether to skip a specific row during processing.
        /// </summary>
        public Func<string, int, bool>? SkipRow { get; set; }

        /// <summary>
        /// Gets or sets the character used to separate values within each row of the CSV data.
        /// </summary>
        public char Separator { get; set; } = ',';

        /// <summary>
        /// Gets or sets a boolean indicating whether to trim leading and trailing whitespaces from each data cell.
        /// </summary>
        public bool TrimData { get; set; }

        /// <summary>
        /// Gets or sets the mode for handling the header row in the CSV data.
        /// </summary>
        public HeaderMode HeaderMode { get; set; } = HeaderMode.NoPresent;

        /// <summary>
        /// Gets or sets the string representing the newline character used in the CSV data.
        /// </summary>
        public string NewLine { get; set; } = Environment.NewLine;

        /// <summary>
        /// Gets or sets a boolean indicating whether to remove rows from the CSV data that are empty after processing.
        /// </summary>
        public bool RemoveEmptyRows { get; set; } = false;

        /// <summary>
        /// Gets or sets a boolean indicating whether a backslash `\` can be used to escape the cell separator character within a cell value.
        /// </summary>
        public bool AllowBackslashToEscapeCellSeparator { get; set; } = true;

        /// <summary>
        /// Gets or sets a IRowParser that allows custom logic for parsing a CSV row into cells. 
        /// If set, this custom parsing logic takes precedence over the default parsing logic. 
        /// The function is expected to take a CSV row as input and return an enumerable collection of cell values.
        /// </summary>
        [DisallowNull, NotNull]
        public IRowParser RowParser { get; set; } = new RowParser();


    }


    public enum HeaderMode
    {
        /// <summary>
        /// Indicates that the CSV data contains a header row, and the first row should be treated as column names.
        /// </summary>
        HeaderPresent,

        /// <summary>
        /// Indicates that there is no header row in the CSV data, and columns should be assigned generic names.
        /// </summary>
        NoPresent
    }

}
