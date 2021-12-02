using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace Notebook
{
    public class MsgCheck
    {
        public string session_id { get; set; }
        public int message_id { get; set; }
    }
    public class MsgCreate
    {
        public int source_user { get; set; }
        public int target_user { get; set; }
        public bool item { get; set; }
        public int item_id { get; set; }
        public int collection_id { get; set; }
    }

    public class Message
    {
        public string initial_a { get; set; }
        public string initial_b { get; set; }
        public string header { get; set; }
        public string content { get; set; }
        public int id { get; set; }
        public int item_id { get; set; }
        public int collection_id { get; set; }
        public int type { get; set; }
        public bool seen { get; set; }
    }

    public static class Inbox
    {
        public static JSONResult MarkRead(MsgCheck mc)
        {
            int user_id;

            if (!Auth.ValidateSession(mc.session_id, out user_id, out _))
                return new JSONResult("Error: Invalid Session");

            string q = @"UPDATE messages SET seen = 1 WHERE id = @message_id AND target_user = @user_id";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@message_id", mc.message_id);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return new JSONResult(true);
        }

        public static Message[] GetMessages(int user_id)
        {
            string q = @"SELECT TOP 50
                        users.username,
                        users.first_name,
                        users.last_name,
                        messages.id AS id,
                        CASE WHEN messages.item_id IS NOT NULL THEN 0 ELSE 1 END type,
                        messages.item_id,
                        messages.collection_id,
                        collections.name AS collection_name,
                        items.title,
                        messages.seen

                        FROM messages

                        INNER JOIN users ON users.id = messages.root_user

                        LEFT OUTER JOIN collections ON collections.id = messages.collection_id

                        LEFT OUTER JOIN items ON items.id = messages.item_id

                        WHERE target_user = @user_id

                        ORDER BY datetime DESC";

            List<Message> msgs = new List<Message>();

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        Message m = new Message();

                        m.type = (int)reader["type"];

                        //Item message
                        string first_name = (string)reader["first_name"];
                        string second_name = (string)reader["last_name"];

                        m.initial_a = first_name[0].ToString();
                        m.initial_b = second_name[0].ToString();
                        m.id = (int)reader["id"];
                        m.seen = (bool)reader["seen"];

                        if (m.type == 0)
                        {

                            m.header = (string)reader["username"] + " shared an item with you:";
                            m.content = (string)reader["title"];
                            m.item_id = (int)reader["item_id"];
                        }
                        else
                        {
                            m.header = (string)reader["username"] + " shared a collection with you:";
                            m.content = (string)reader["collection_name"];
                            m.collection_id = (int)reader["collection_id"];
                            m.id = (int)reader["id"];
                        }

                        msgs.Add(m);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return msgs.ToArray();
        }
        public static bool Notify(MsgCreate m)
        {
            string q;
            //Insert message into table
            if (m.item) q = @"INSERT INTO messages (
            type, root_user, target_user, item_id, seen, datetime) VALUES (@type, @root_user, @target_user, @item_id, @seen, GETDATE())";
            else
                q = @"INSERT INTO messages (
            type, root_user, target_user, collection_id, seen datetime) VALUES (@type, @root_user, @target_user, @collection_id, @seen, GETDATE())";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);

                command.Parameters.AddWithValue("@type", m.item ? 0 : 1);
                command.Parameters.AddWithValue("@root_user", m.source_user);
                command.Parameters.AddWithValue("@target_user", m.target_user);
                command.Parameters.AddWithValue("@item_id", m.item_id);
                command.Parameters.AddWithValue("@seen", false);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }

        public static bool AcceptShare(int message_id)
        {
            //Check if message is still valid

            //Modify accepted bit

            return true;
        }
    }
}
