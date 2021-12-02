using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public static class Extensions
    {
        public static void AddArrayParameters<T>(this SqlCommand cmd, string name, IEnumerable<T> values)
        {
            name = name.StartsWith("@") ? name : "@" + name;
            var names = string.Join(", ", values.Select((value, i) => {
                var paramName = name + i;
                cmd.Parameters.AddWithValue(paramName, value);
                return paramName;
            }));
            cmd.CommandText = cmd.CommandText.Replace(name, names);
        }
    }

    public enum ApplicationEvent
    {
        UserLogin_Success,
        UserLogin_Fail,
        UserLogin_Logout,
        UserLogin_Auto,

        Item_Add,
        Item_Append,
        Item_Delete,
        Item_Complete,

        Category_Create,
        Category_Delete,

        Category_AddShare,
        Category_RemoveShare,

        Homepage_Access,

        ApplicationError
    }

    public static class Event
    {
        public static bool Log(ApplicationEvent event_type, string detail)
        {
            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.logEventQuery, connection);
                command.Parameters.AddWithValue("@type", event_type.ToString());
                command.Parameters.AddWithValue("@details", !String.IsNullOrWhiteSpace(detail) ? detail : "");
                command.Parameters.AddWithValue("@datetime", DateTime.Now);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }
    }
}
