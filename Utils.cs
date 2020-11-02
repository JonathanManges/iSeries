using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iSeriesConnector
{
    public class Utils
    {
        public static DateTime ConvertIntToDate(int dateToConvert, bool centuryMarker)
        {
            if (centuryMarker)
            {
                dateToConvert += 19000000;
            }

            DateTime.TryParseExact(dateToConvert.ToString(), "yyyyddMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime returnValue);

            return returnValue;
        }

        public static DateTime ConvertIntToTime(int timeToConvert)
        {
            DateTime.TryParseExact(timeToConvert.ToString(), "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime returnValue);

            return returnValue;
        }

        public static DateTime ConvertIntToDateTime(int dateToConvert, bool centuryMartker, int timeToConvert)
        {
            if (centuryMartker)
            {
                dateToConvert += 19000000;
            }

            DateTime dateReturnValue = ConvertIntToDate(dateToConvert, centuryMartker);
            DateTime timeReturnValue = ConvertIntToTime(timeToConvert);

            if (dateReturnValue == default || timeReturnValue == default)
            {
                return default;
            }

            DateTime valueToReturn = new DateTime(dateReturnValue.Year, dateReturnValue.Month, dateReturnValue.Day, timeReturnValue.Hour, timeReturnValue.Minute, timeReturnValue.Second);

            return valueToReturn;
        }

        public static int ConvertDateToInt(DateTime dateToConvert, bool centuryMarker)
        {
            if (centuryMarker)
            {
                return int.Parse(dateToConvert.ToString("yyyddMM")) - 19000000;
            }
            else
            {
                return int.Parse(dateToConvert.ToString("yyyyddMM"));
            }
        }

        public static int ConvertTimeToInt(DateTime timeToConvert)
        {
            return int.Parse(timeToConvert.ToString("HHmmss"));
        }
    }
}
