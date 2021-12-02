using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public class ItemShares
    {
        public int item_id { get; set; }
        public int[] share_ids { get; set; }
    }
    public class UsrValidate
    {
        public string session_id { get; set; }
        public string username { get; set; }
    }

    public static class Share
    {
        public static JSONResult ItemConfigure(int item_id, int user_id, string[] usernames)
        {
            //Check if user has permission to change sharing
            var ownership = Auth.CheckOwnership(user_id, item_id);

            if (ownership != OwnershipType.OwnerCreator)
                return new JSONResult(true);

            List<int> new_user_ids = GetUserIDsFromName(usernames, user_id);

            JSONResult r = SetItemShares(item_id, new_user_ids, user_id);

            return r;
        }

        static JSONResult SetItemShares(int item_id, List<int> shared_ids, int user_id)
        {
            //Get list of currently shared users

            List<int> currently_shared = GetSharedUsersItem(item_id);

            List<int> remove = new List<int>();
            List<int> append = new List<int>();

            //TODO: Make sure item does not belong to a shared collection
            if (IsInSharedCollection(item_id) && shared_ids.Count > 0)
                return new JSONResult("Error: You cannot share items with individual users if they belong to a shared collection");

            //Add or remove users
            foreach (int i in currently_shared)
            {
                if (!shared_ids.Contains(i))
                {
                    remove.Add(i);
                }
            }

            foreach (int i in shared_ids)
            {
                if (!currently_shared.Contains(i))
                {
                    append.Add(i);
                }
            }

            RemoveSharedUsersItem(remove, item_id);
            AddSharedUsersItem(append, item_id);

            //TODO: Notify users via message protocol
            foreach(int i in append)
            {
                Inbox.Notify(new MsgCreate
                {
                    target_user = i,
                    source_user = user_id,
                    collection_id = 0,
                    item_id = item_id,
                    item = true
                });
            }

            return new JSONResult(true);
        }

        public static JSONResult ValidateUser(UsrValidate v)
        {
            var result = new JSONResult(false);

            if (!Auth.ValidateSession(v.session_id, out _, out _))
                return result;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.checkUsername, connection);
                command.Parameters.AddWithValue("@username", v.username);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    if (reader.HasRows) result = new JSONResult(true);
                }
                finally
                {
                    reader.Close();
                }
            }

            return result;
        }

        public static void RemoveSharedUsersItem(List<int> users, int item_id)
        {
            string q = "DELETE FROM shares WHERE item_id = @item_id AND user_id IN (@users)";
            string usrs = String.Join(',', users);

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                command.Parameters.AddWithValue("@users", usrs);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        static void AddSharedUsersItem(List<int> users, int item_id)
        {
            string q = "INSERT INTO shares (item_id, user_id) VALUES (@item_id, @user_id)";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                command.Parameters.AddWithValue("@user_id", "");

                foreach (int user in users)
                {
                    command.Parameters[1].Value = user;
                    command.ExecuteNonQuery();
                }
            }
        }

        static List<int> GetSharedUsersItem(int item_id)
        {
            //Return list of user ids which the item has been shared with
            List<int> ids = new List<int>();

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.getSharedUsers, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        ids.Add((int)reader["id"]);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return ids;
        }

        static bool IsInSharedCollection(int item_id)
        {
            bool result = false;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.isInSharedCollection, connection);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        result = true;
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return result;
        }

        static List<int> GetUserIDsFromName(string[] usernames, int user_id)
        {
            //List of usernames
            List<int> ids = new List<int>();

            if (usernames == null) return ids;

            string q = "SELECT id FROM users WHERE username IN (@usernames)";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.AddArrayParameters("@usernames", usernames);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        if((int)reader["id"] != user_id)
                            ids.Add((int)reader["id"]);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return ids;
        }
    }
}