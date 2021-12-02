using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public enum ItemOrdering
    {
        ModifiedRecent,
        DueSoon,
        AtoZ
    }

    public class ItemQueryClient
    {
        public bool multiple { get; set; }
        public int item_id { get; set; }
        public string session_id { get; set; }
        public int collection_id { get; set; }
        public int top_items { get; set; }
        public string tag { get; set; }
        public string search_term { get; set; }
        public ItemOrdering order { get; set; }

        public bool Validate(out ItemQuery q)
        {
            int user_id;

            if(Auth.ValidateSession(session_id, out user_id, out _))
            {
                q = new ItemQuery
                {
                    user_id = user_id,
                    collection_id = collection_id,
                    multiple = multiple,
                    item_id = item_id,
                    order = order,
                    top_items = top_items,
                    tag = tag,
                    search_term = search_term
                };

                return true;
            }

            else
            {
                q = null;

                return false;
            }
        }
    }

    public class ItemQuery
    {
        public bool multiple { get; set; }
        public int item_id { get; set; }
        public int user_id { get; set; }
        public string tag { get; set; }
        public string search_term { get; set; }
        public int collection_id { get; set; }
        public int top_items { get; set; }
        public ItemOrdering order { get; set; }
    }

    public class TableItem
    {
        public int id { get; set; }
        public string title { get; set; }
        public int item_type { get; set; }
        public int shared_with { get; set; }
        public string modified_on { get; set; }
        public string created_on { get; set; }
        public string collection { get; set; }
        public string due { get; set; }
        public int todo_complete { get; set; }
    }
    
    public class FetchResult
    {
        public TableItem[] results { get; set; }
        public string[] tags { get; set; }
    }

    public static class Fetch
    {
        public static FetchResult GetItems(ItemQuery q)
        {
            string[] tags;

            var i = Items(q, out tags);

            return new FetchResult
            {
                results = i,
                tags = tags
            };
        }

        public static TableItem[] Items(ItemQuery q, out string[] tags)
        {
            List<string> list_tags = new List<string>();

            string top = q.top_items > 0 ? $"TOP {q.top_items}" : "";
            string coll = q.collection_id > -1 ? $"AND (root_id = {q.collection_id} OR collection_id = {q.collection_id} OR moved_collection = {q.collection_id})" : "";
            string tag = !String.IsNullOrWhiteSpace(q.tag) ? $"AND (',' + RTRIM(tag_ids) + ',') LIKE '%,' + @tag_name + ',%'" : "";
            string ordering = "";
            string sch = !String.IsNullOrWhiteSpace(q.search_term) ?
                $"AND (items.title LIKE '%' + @search_term + '%' OR tag_ids LIKE '%' + @search_term + '%')" : "";

            switch (q.order)
            {
                case ItemOrdering.ModifiedRecent:
                    ordering += "ORDER BY modified_on DESC";
                    break;
                case ItemOrdering.DueSoon:
                    ordering += "AND due_date > GETDATE() ORDER BY due_date ASC";
                    break;
                case ItemOrdering.AtoZ:
                    ordering += "ORDER BY title ASC";
                    break;
            }

            string query = @$"SELECT {top}
                            items.title,
                            items.text,
                            items.due_date,
                            items.created_on,
                            items.created_by,
                            items.modified_on,
                            items.item_type,
                            items.id,
                            items.tag_ids,
                            items.completed_on,
                            CASE WHEN cs.shared_user IS NOT NULL THEN 1 ELSE 0 END AS shared_collection,
                            CASE WHEN shares.user_id IS NOT NULL THEN 1 ELSE 0 END AS shared_item,
                            COALESCE(
                            (SELECT collections.name FROM collections WHERE collections.id = cs.root_id),
                            CASE WHEN shares.user_id IS NOT NULL THEN COALESCE((SELECT collections.name FROM collections WHERE shares.moved_collection = collections.id),'Shared') ELSE NULL END,
                            (SELECT collections.name FROM collections WHERE collections.id = items.collection_id)) AS collection
                            FROM items
                            LEFT OUTER JOIN collection_shares cs ON cs.root_id = items.collection_id AND cs.shared_user = @user_id

                            INNER JOIN collections ON collections.id = items.collection_id

                            LEFT OUTER JOIN shares ON shares.item_id = items.id AND shares.user_id = @user_id
                            WHERE deleted = 0 AND (items.created_by = @user_id OR cs.shared_user = @user_id OR shares.user_id = @user_id OR collections.user_id = @user_id) 
            {coll}
            {tag}
            {sch}
            {ordering}
            ";

            List<TableItem> items = new List<TableItem>();

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@user_id", q.user_id);
                if(tag != "") command.Parameters.AddWithValue("@tag_name", q.tag);
                if (sch != "") command.Parameters.AddWithValue("@search_term", q.search_term);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        DateTime modified = (DateTime)reader["modified_on"];
                        DateTime created = (DateTime)reader["created_on"];

                        int modified_ago = (DateTime.Now - modified).Days;

                        string str_tags = (string)reader["tag_ids"];

                        if (str_tags.Length > 0) list_tags.AddRange(str_tags.Split(','));


                        items.Add(new TableItem
                        {
                            id = (int)reader["id"],
                            title = (string)reader["title"],
                            item_type = (int)reader["item_type"],
                            shared_with = (int)reader["created_by"] == q.user_id ? 0: 1,
                            collection = (string)reader["collection"],
                            modified_on = modified_ago > 0 ? $"{modified_ago} days ago" : "Today",
                            created_on = created.ToString("dd MMMM yy"),
                            todo_complete = reader["completed_on"] == DBNull.Value ? 0 : 1,
                            due = reader["due_date"] == DBNull.Value ? "" : ((DateTime)reader["due_date"]).ToString("ddd dd MMM HH:mm")
                        }) ;
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            tags = Homepage.OrganiseTags(list_tags);

            return items.ToArray();
        }
    }


}
