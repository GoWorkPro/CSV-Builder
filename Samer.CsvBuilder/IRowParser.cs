using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public interface IRowParser
    {
        /// <summary>
        /// Parses a CSV row into a collection of cell values based on the specified options.
        /// </summary>
        /// <param name="row">The CSV row to parse.</param>
        /// <param name="options">The options for CSV parsing.</param>
        /// <returns>An IEnumerable<string> representing the parsed cell values.</returns>
        IEnumerable<string> Parse(string row, Options options, int rowNumber);
    }
}
