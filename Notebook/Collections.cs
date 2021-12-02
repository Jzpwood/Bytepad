using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public class CollectionCommit
    {
        public string session_id { get; set; }
        public bool append { get; set; }
        public bool delete { get; set; }
        public string name { get; set; }
        public int id { get; set; }
    }

    public class CollectionShareModify
    {
        public string username {  get; set; }
        public bool add { get; set; }
        public int collection_id { get; set; }
    }

    public class CollectionDetails
    {
        public bool owned { get; set; }
        public bool has_access { get; set; }
        public bool shared { get; set; }
    }

    public static class Collections
    {
        public static bool MoveCollectionShared(int user_id, int item_id, int collection_id)
        {
            string q = @"UPDATE shares SET moved_collection = @collection_id WHERE item_id = @item_id AND user_id = @user_id ";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                if(collection_id == -1)
                    command.Parameters.AddWithValue("@collection_id", DBNull.Value);
                else
                    command.Parameters.AddWithValue("@collection_id", collection_id);

                command.Parameters.AddWithValue("@item_id", item_id);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }
        public static CollectionDetails GetInfo(int user_id, int collection_id)
        {
            CollectionDetails details = new CollectionDetails();

            string q = @"
                SELECT users.id,
                CASE WHEN (SELECT collections.id FROM collections WHERE collections.user_id = @user_id AND id = @collection_id) IS NOT NULL THEN 1 ELSE 0 END AS owned,
                CASE WHEN (SELECT collection_shares.root_id FROM collection_shares WHERE collection_shares.shared_user = @user_id AND collection_shares.root_id = @collection_id) IS NOT NULL THEN 1 ELSE 0 END AS shared_with,
                CASE WHEN (SELECT COUNT(*) FROM collection_shares WHERE collection_shares.root_id = @collection_id) > 0 THEN 1 ELSE 0 END AS shared
                FROM users
                WHERE users.id = @user_id
            ";

            bool owned = false;
            bool shared_with = false;
            bool shared = false;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@collection_id", collection_id);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        owned = (int)reader["owned"] == 1 ? true : false;
                        shared_with = (int)reader["shared_with"] == 1 ? true : false;
                        shared = (int)reader["shared"] == 1 ? true : false;
                    }
                }
                finally
                {
                    reader.Close();

                    details.owned = owned;
                    details.has_access = owned || shared_with;
                    details.shared = shared;
                }
            }

            return details;
        }

        public static JSONResult AddUpdateCollection(CollectionCommit c)
        {
            int user_id;

            if (!Auth.ValidateSession(c.session_id, out user_id, out _))
                return new JSONResult("Error: Invalid Session");

            if (c.delete)
            {
                RemoveCollection(user_id, c.id);
                return new JSONResult(true);
            }

            string new_name = c.name.Trim();

            if (!Validation.Name(new_name))
                return new JSONResult("Error: Invalid Name");

            if (c.append)
            {
                if (!UpdateCollection(user_id, c))
                    return new JSONResult("Error: Cannot change the name of this collection");
            }
            else
            {
                CreateCollection(user_id, c.name);
            }

            return new JSONResult(true);
        }

        public static void CreateCollection(int user_id, string name)
        {
            using(SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.createCollection, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@name", name);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        public static bool UpdateCollection(int user_id, CollectionCommit c)
        {
            CollectionDetails d = GetInfo(user_id, c.id);

            if (!d.owned)
                return false;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.updateCollectionName, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@name", c.name);
                command.Parameters.AddWithValue("@id", c.id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }

        public static bool RemoveCollection(int id, int collection_id)
        {
            CollectionDetails cd = GetInfo(id, collection_id);

            if (!cd.owned)
                return false;

            string q = @"UPDATE items SET deleted = 1 WHERE collection_id = @collection_id;
                        DELETE FROM collection_shares WHERE collection_shares.root_id = @collection_id;
                        UPDATE shares SET shares.moved_collection = NULL WHERE shares.moved_collection = @collection_id;
                        DELETE FROM collections WHERE collections.id = @collection_id;";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(q, connection);
                command.Parameters.AddWithValue("@collection_id", collection_id);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return true;
        }

        public static Collection[] GetCollections(int user_id)
        {
            List<Collection> collections = new List<Collection>();

            //Fetch user owned collections

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.getCollectionsQuery, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        collections.Add(new Collection((string)reader["name"], (int)reader["id"], false));
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            //Fetch shared collections

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.getSharedCollectionsQuery, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        collections.Add(new Collection((string)reader["name"], (int)reader["id"], true));
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return collections.ToArray();
        }
    }
}
