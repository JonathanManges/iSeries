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
        public static DateTime ConvertToDate(int dateToConvert, bool centuryMarker)
        {
            if (centuryMarker)
            {
                dateToConvert += 19000000;
            }

            DateTime.TryParseExact(dateToConvert.ToString(), "yyyyddMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime returnValue);

            return returnValue;
        }

        public static DateTime ConvertToTime(int timeToConvert)
        {
            DateTime.TryParseExact(timeToConvert.ToString(), "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime returnValue);

            return returnValue;
        }

        public static DateTime ConvertToDateTime(int dateToConvert, bool centuryMartker, int timeToConvert)
        {
            if (centuryMartker)
            {
                dateToConvert += 19000000;
            }

            DateTime dateReturnValue = ConvertToDate(dateToConvert, centuryMartker);
            DateTime timeReturnValue = ConvertToTime(timeToConvert);

            if (dateReturnValue == default || timeReturnValue == default)
            {
                return default;
            }

            DateTime valueToReturn = new DateTime(dateReturnValue.Year, dateReturnValue.Month, dateReturnValue.Day, timeReturnValue.Hour, timeReturnValue.Minute, timeReturnValue.Second);

            return valueToReturn;
        }
    }
}
