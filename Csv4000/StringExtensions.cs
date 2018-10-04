using System.Text;

namespace Csv4000
{
    public static class StringExtensions
    {
        /// <summary>
        /// Turn a string into a CSV cell output
        /// https://stackoverflow.com/questions/6377454/escaping-tricky-string-to-csv-format
        /// </summary>
        /// <param name="value">String to output</param>
        /// <returns>The CSV cell formatted string</returns>
        public static string ToCsvString(this object value)
        {
            if (value == null)
                return string.Empty;

            if (value is float floatValue)
                return floatValue.ToString();

            var stringValue = value.ToString();

            bool mustQuote =
                stringValue.Contains(",")
                || stringValue.Contains("\"")
                || stringValue.Contains("\r")
                || stringValue.Contains("\n");

            if (mustQuote)
            {
                StringBuilder stringBuilder = new StringBuilder();

                stringBuilder.Append("\"");
                foreach (char nextChar in stringValue)
                {
                    stringBuilder.Append(nextChar);
                    if (nextChar == '"')
                        stringBuilder.Append("\"");
                }
                stringBuilder.Append("\"");

                return stringBuilder.ToString();
            }

            return stringValue;
        }
    }
}
