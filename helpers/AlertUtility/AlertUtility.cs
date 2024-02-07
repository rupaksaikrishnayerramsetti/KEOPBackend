using System;
using System.Globalization;

namespace KEOPBackend.helpers.AlertUtility
{
    public class AlertUtility
    {
        public string ModifyDate(string date)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
            {
                return dateTime.ToString("MMMM d, yyyy");
            }
            return "Invalid Date";
        }

        public string ModifyTime(string time)
        {
            if (DateTime.TryParse(time, out DateTime dateTime))
            {
                return dateTime.ToString("h:mm tt");
            }
            return "Invalid Time";
        }

        public string GenerateGoogleCalendarLink(string title, string date, string time, string description = "")
        {
            if (DateTime.TryParse($"{date} {time}", out DateTime startDateTime))
            {
                DateTime endDateTime = startDateTime.AddMinutes(30);
                string startDateTimeStr = startDateTime.ToString("yyyyMMddTHHmmssZ");
                string endDateTimeStr = endDateTime.ToString("yyyyMMddTHHmmssZ");
                title = Uri.EscapeDataString(title);
                description = Uri.EscapeDataString(description);
                string eventLink = $"https://www.google.com/calendar/render?action=TEMPLATE&text={title}&dates={startDateTimeStr}/{endDateTimeStr}&details={description}&sf=true&output=xml";
                return eventLink;
            }
            return "Invalid Date or Time";
        }
    }
}
