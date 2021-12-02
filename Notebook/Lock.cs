using Microsoft.Data.SqlClient;
using System;

namespace Notebook
{
    public class LockRequestSession
    {
        public string session_id { get; set; }
        public int item_id { get; set; }
    }
    public class LockRequestID
    {
        public int user_id { get; set; }
        public int item_id { get; set; }
    }
    public static class Lock
    {
        public static bool RequestSession(LockRequestSession req)
        {
            int user_id;

            if (!Auth.ValidateSession(req.session_id, out user_id, out _))
                return false;

            return Request(new LockRequestID { user_id = user_id, item_id = req.item_id }); 
        }

        public static bool Request(LockRequestID req)
        {
            if (Auth.CheckOwnership(req.user_id, req.item_id) == OwnershipType.NotOwned)
                return false;

            //Check if document is locked out
            var l = GetLockout(req.item_id);

            //Check if lockout belongs to user or not locked out
            if(l == null || l.Item1 == req.user_id)
            {
                //Reissue
                SetLockout(req.user_id, req.item_id);
                return true;
            }
            else if((DateTime.Now - l.Item2).TotalMinutes > 1)
            {
                //Lockout has expired, pass to new user
                SetLockout(req.user_id, req.item_id);
                return true;
            }
            return false;
        }

        public static void SetLockout(int user_id, int item_id)
        {
            string q = @"UPDATE items SET locked_out_by = @user_id, locked_out_on = GETDATE() WHERE id = @item_id";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void Unlock(int item_id)
        {
            string q = @"UPDATE items SET locked_out_by = NULL, locked_out_on = NULL WHERE id = @item_id";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public static Tuple<int, DateTime> GetLockout(int item_id)
        {
            Tuple<int, DateTime> result = null;

            string q = @"SELECT locked_out_by, locked_out_on FROM items WHERE items.id = @item_id";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        if (reader["locked_out_by"] != DBNull.Value)
                        {
                            result = new Tuple<int, DateTime>((int)reader["locked_out_by"], (DateTime)reader["locked_out_on"]);
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            return result;
        }
    }
}
