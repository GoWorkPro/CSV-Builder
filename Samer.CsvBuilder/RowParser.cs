using System;
using System.Collections.Generic;
using System.Text;

namespace GoWorkPro.CsvBuilder
{
    public class RowParser : IRowParser
    {
        public virtual IEnumerable<string> Parse(string row, Options options, int rowNumber)
        {
            char separator = options.Separator;
            bool inQuote = false;
            bool escaped = false;
            int start = 0;


            List<string> result = new List<string>();

            // Handle the case where the row is empty
            if (row.Length <= 0)
            {
                result.Add(row);
                return result;
            }

            // Iterate through each character in the row for parsing
            for (int i = 0; i < row.Length; i++)
            {
                char currentChar = row[i];

                // Handle backslash escaping of cell separator
                if (options.AllowBackslashToEscapeCellSeparator && (currentChar == '\\' || (escaped && currentChar == separator)))
                {
                    escaped = !escaped;
                    continue;
                }
                // Handle double quote for quoted values
                else if (currentChar == '"')
                {
                    inQuote = !inQuote;
                    escaped = false;

                    if (inQuote)
                        continue;
                }

                // Check for cell separator and add parsed cell to the result
                if ((!escaped && !inQuote) && currentChar == separator)
                {
                    result.Add(row.Substring(start, i - start));
                    start = i + 1;
                }

                // Handle the last cell in the row
                if (i == row.Length - 1)
                {
                    result.Add(row.Substring(start));
                }
            }
            // Return the parsed cell values
            return result;
        }
    }
}
