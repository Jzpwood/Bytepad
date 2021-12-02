using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Notebook
{
    public class CalendarRequest
    {
        public string session_id { get; set; }
        public int month { get; set; }
        public int year { get; set; }
    }
    public class CalendarObject
    {
        public int user_id { get; set; }
        public CalendarWeek[] weeks { get; set; } = new CalendarWeek[6];
    }

    public class CalendarWeek
    {
        public CalendarDay[] days { get; set; } = new CalendarDay[7];
    }

    public class CalendarDay
    {
        public bool today { get; set; }
        public int day_number { get; set; }
        public int day_of_week { get; set; }
        public int week_number { get; set; }
        public bool relevant_month { get; set; }
        public string date { get; set; }
        public CalendarEvent[] events { get; set; }
        public string full_date { get; set; }
        public bool this_week { get; set; }
    }

    public class CalendarEvent
    {
        public int item_id { get; set; }
        public string title { get; set; }
        public DateTime due_date { get; set; }
        public int recurring { get; set; }
        public string time { get; set; }
        public bool todo { get; set; }
    }

    public static class Calendar
    {
        static CalendarObject transformCalendar(List<CalendarEvent> events, DateTime start, DateTime finish, int month)
        {
            CalendarObject o = new CalendarObject();

            var tdy = DateTime.Now.Date;

            var eof_week = tdy.AddDays(7);

            for(int w = 0; w < 6; w++)
            {
                o.weeks[w] = new CalendarWeek();

                for(int d = 0; d < 7; d++)
                {
                    var date_cursor = start.AddDays(7 * w + d);

                    List<CalendarEvent> date_events = new List<CalendarEvent>();

                    foreach(CalendarEvent e in events)
                    {
                        if (e.due_date.Date == date_cursor.Date)
                        {
                            e.time = e.due_date.ToString("HH:mm");
                            date_events.Add(e);
                        }
                        
                    }

                    CalendarDay day = new CalendarDay
                    {
                        day_number = date_cursor.Day,
                        day_of_week = adjustDay(date_cursor.DayOfWeek),
                        week_number = w,
                        date = date_cursor.ToShortDateString(),
                        relevant_month = (date_cursor.Month == month),
                        today = (tdy == date_cursor.Date),
                        full_date = date_cursor.ToString("dddd d\"th\" MMM"),
                        this_week = (date_cursor.Date >= tdy && date_cursor.Date < eof_week)
                    };

                    if(date_events.Count > 0)
                    {
                        day.events = date_events.ToArray();
                    }

                    o.weeks[w].days[d] = day;
                }
            }

            return o;
        }

        static int adjustDay(DayOfWeek d)
        {
            int n = (int)d;

            if(n == 0) return 6;

            return n - 1;
        }

        public static CalendarObject getCalendar(int user_id, CalendarRequest c)
        {
            CalendarObject cal;

            var day_first_cm = new DateTime(c.year, c.month, 1);
            var day_first_calendar = day_first_cm.AddDays(-adjustDay(day_first_cm.DayOfWeek));
            var day_last_calendar = day_first_calendar.AddDays(41);

            //Get non-recurring events

            string q = @"SELECT 
                        items.id,
                        collection_id,
                        title,
                        items.item_type,
                        due_date
                        FROM items
                        LEFT OUTER JOIN collection_shares cs ON cs.root_id = items.collection_id AND cs.shared_user = @user_id
                        INNER JOIN collections ON collections.id = items.collection_id
                        LEFT OUTER JOIN shares ON shares.item_id = items.id AND shares.user_id = @user_id
                        WHERE 
                            deleted = 0 
                        AND 
                            (items.created_by = @user_id OR cs.shared_user = @user_id OR shares.user_id = @user_id OR collections.user_id = @user_id)
                        AND (item_type = 2 OR (item_type = 1 AND due_date IS NOT NULL AND completed_on IS NULL))
                        AND due_date BETWEEN @start_date AND @end_date
                        AND reccurence_type = 0
                        ORDER BY due_date ASC
";

            List<CalendarEvent> events = new List<CalendarEvent>();

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@start_date", day_first_calendar);
                command.Parameters.AddWithValue("@end_date", day_last_calendar.AddDays(1));

                connection.Open();
                SqlDataReader r = command.ExecuteReader();
                try
                {

                    while (r.Read())
                    {
                        events.Add(new CalendarEvent
                        {
                            todo = (int)r["item_type"] == 1 ? true : false,
                            title = (string)r["title"],
                            item_id = (int)r["id"],
                            due_date = (DateTime)r["due_date"]
                        });
                    }
                }
                finally
                {
                    r.Close();
                }
            }

            //Get all recurring events
            string qr = @"SELECT 
                        id,
                        collection_id,
                        title,
                        due_date,
                        reccurence_type
                        FROM items
                        WHERE
                        item_type = 2
                        AND deleted = 0
                        AND reccurence_type > 0
                        AND created_by = @user_id
                        AND due_date > @start_date AND due_date < @end_date";

            List<CalendarEvent> events_rec = new List<CalendarEvent>();

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                var rec_start = DateTime.Now.AddDays(-50);

                SqlCommand command = new SqlCommand(qr, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@start_date", rec_start);
                command.Parameters.AddWithValue("@end_date", day_last_calendar.AddDays(1));

                connection.Open();
                SqlDataReader r = command.ExecuteReader();
                try
                {

                    while (r.Read())
                    {
                        events_rec.Add(new CalendarEvent
                        {
                            title = (string)r["title"],
                            item_id = (int)r["id"],
                            due_date = (DateTime)r["due_date"],
                            recurring = (int)r["reccurence_type"]
                        });
                    }
                }
                finally
                {
                    r.Close();
                }
            }

            //Generate instances until end date

            foreach(CalendarEvent e in events_rec)
            {
                var rec_type = (RecurringType)e.recurring;

                if (e.due_date < day_last_calendar.AddDays(1) && e.due_date > day_first_calendar)
                    events.Add(e);

                CalendarEvent instance = new CalendarEvent{
                    due_date = e.due_date,
                    item_id = e.item_id,
                    recurring = e.recurring,
                    title = e.title
                };
                
                while(instance.due_date < day_last_calendar.AddDays(1))
                {
                    switch (rec_type)
                    {
                        case RecurringType.Daily:
                            instance.due_date = instance.due_date.AddDays(1);
                            break;
                        case RecurringType.Every_other_day:
                            instance.due_date = instance.due_date.AddDays(2);
                            break;
                        case RecurringType.Four_Weekly:
                            instance.due_date = instance.due_date.AddDays(28);
                            break;
                        case RecurringType.Monthly:
                            instance.due_date = instance.due_date.AddMonths(1);
                            break;
                        case RecurringType.Thirty_Days:
                            instance.due_date = instance.due_date.AddDays(30);
                            break;
                        case RecurringType.Two_Weekly:
                            instance.due_date = instance.due_date.AddDays(14);
                            break;
                        case RecurringType.Weekly:
                            instance.due_date = instance.due_date.AddDays(7);
                            break;
                    }

                    if (instance.due_date < day_last_calendar.AddDays(1) && instance.due_date > day_first_calendar)
                        events.Add(new CalendarEvent
                        {
                            due_date = instance.due_date,
                            item_id = instance.item_id,
                            recurring = instance.recurring,
                            title = instance.title
                        });
                }
            }

            cal = transformCalendar(events, day_first_calendar, day_last_calendar, day_first_cm.Month);

            return cal;
        }
    }
}
