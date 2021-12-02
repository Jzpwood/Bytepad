using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public static class Items
    {
        public static JSONResult ItemModify(ItemAction i)
        {
            int user_id;

            if (!Auth.ValidateSession(i.session_id, out user_id, out _))
                return new JSONResult("Error: Invalid session");

            var o = Auth.CheckOwnership(user_id, i.item_id);

            if(i.delete && o == OwnershipType.OwnerCreator)
            {
                using (SqlConnection connection = new SqlConnection(Query.connectionString))
                {
                    SqlCommand command = new SqlCommand(Query.deleteItem, connection);
                    command.Parameters.AddWithValue("@item_id", i.item_id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return new JSONResult(true);
            }
            else if(i.delete && o == OwnershipType.SharedItem)
            {
                Share.RemoveSharedUsersItem(new List<int> { user_id }, i.item_id);

                return new JSONResult(true);
            }
            else if (i.undelete && o == OwnershipType.OwnerCreator)
            {
                using (SqlConnection connection = new SqlConnection(Query.connectionString))
                {
                    SqlCommand command = new SqlCommand(Query.undeleteItem, connection);
                    command.Parameters.AddWithValue("@item_id", i.item_id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return new JSONResult(true);
            }
            else if ((i.complete || i.uncomplete) && (o == OwnershipType.OwnerCreator || o == OwnershipType.SharedCollection || o == OwnershipType.SharedItem))
            {
                using (SqlConnection connection = new SqlConnection(Query.connectionString))
                {
                    SqlCommand command = new SqlCommand(Query.completeItem, connection);
                    command.Parameters.AddWithValue("@item_id", i.item_id);
                    if (i.uncomplete)
                        command.Parameters.AddWithValue("@completed_on", DBNull.Value);
                    else
                        command.Parameters.AddWithValue("@completed_on", DateTime.Now);
                    connection.Open();
                    command.ExecuteNonQuery();
                }

                return new JSONResult(true);
            }

            return new JSONResult(false);
        }

        public static JSONResult GetItem(ItemQuery q)
        {
            var owned = Auth.CheckOwnership(q.user_id, q.item_id);

            if(owned == OwnershipType.NotOwned)
            {
                return new JSONResult("Error: You do not have permission to view this item");
            }

            bool foundItem = false;
            bool deleted = false;
            Item i = new Item();

            JSONResult res = new JSONResult(true, i);

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.getItemSingle, connection);
               command.Parameters.AddWithValue("@item_id", q.item_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        deleted = (bool)reader["deleted"];
                        foundItem = true;
                        i.id = (int)reader["id"];
                        i.type = (int)reader["item_type"];
                        i.created_by = (string)reader["created_by"];
                        i.collection = (string)(reader["name"] ?? "");
                        i.collection_id = (int)reader["collection_id"];
                        i.tags = String.IsNullOrWhiteSpace((string)reader["tags"]) ? null :((string)reader["tags"]).Split(',');
                        i.created_on = (string)reader["created_date"];
                        i.modified_on = (string)reader["modified_date"];
                        i.title = (string)reader["title"];
                        i.content = (string)reader["text"];
                        i.due_date = (string)reader["due_date"];
                        if (!String.IsNullOrWhiteSpace(i.due_date)) i.friendly_due_date = DateTime.Parse(i.due_date).ToString("ddd dd MMMM yyyy HH:mm");
                        i.recurrence_type = (int)reader["reccurence_type"];
                        i.remind = (int)reader["remind"];
                        i.remind_before_hrs = (int)reader["remind_before_hrs"];
                        i.completed_on = reader["completed_on"] == DBNull.Value ? "" : ((DateTime)reader["completed_on"]).ToString();
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            if (deleted) return new JSONResult("Error: This item has been deleted");

            if (foundItem)
            {
                //Check if user is allowed to edit
                i.locked_out = !Lock.Request(new LockRequestID { item_id = i.id, user_id = q.user_id });

                i.collections = Collections.GetCollections(q.user_id);

                if(owned == OwnershipType.SharedItem)
                {
                    //Get moved collection if it exists
                    string q_moved = @"SELECT moved_collection FROM shares WHERE item_id = @item_id AND user_id = @user_id";
                    bool moved = false;

                    using (SqlConnection connectionb = new SqlConnection(Query.connectionString))
                    {
                        SqlCommand commandb = new SqlCommand(q_moved, connectionb);
                        commandb.Parameters.AddWithValue("@item_id", q.item_id);
                        commandb.Parameters.AddWithValue("@user_id", q.user_id);
                        connectionb.Open();
                        SqlDataReader readerb = commandb.ExecuteReader();
                        try
                        {
                            while (readerb.Read())
                            {
                                if (readerb["moved_collection"] != DBNull.Value)
                                {
                                    i.collection_id = (int)readerb["moved_collection"];
                                    moved = true;
                                }
                            }
                        }
                        finally
                        {
                            readerb.Close();
                        }
                    }
                    //Else set collection ID to -1
                    if (!moved) i.collection_id = -1;
                }

                if (owned == OwnershipType.SharedCollection || owned == OwnershipType.SharedItem)
                {
                    i.secondary_owner = 1;

                    if(owned == OwnershipType.SharedItem)
                    {
                        i.shared_item = 1;
                    }
                }
                else
                {
                    List<string> sharedUsers = new List<string>();

                    using (SqlConnection connection = new SqlConnection(Query.connectionString))
                    {
                        SqlCommand command = new SqlCommand(Query.getSharedUsers, connection);
                        command.Parameters.AddWithValue("@item_id", q.item_id);
                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        try
                        {
                            while (reader.Read())
                            {
                                sharedUsers.Add((string)reader["username"]);
                            }
                        }
                        finally
                        {
                            reader.Close();
                        }
                    }

                    if (sharedUsers.Count > 0)
                        i.shared_users = sharedUsers.ToArray();

                }
            }

            if (!foundItem)
            {
                res.ok = false;
                res.error = "Error: Item not found";
            }

            return res;
        }

        public static JSONResult UpdateItem(Item i, bool append, OwnershipType ownership, int user_id)
        {
            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(append ? (ownership == OwnershipType.OwnerCreator ? Query.updateItem : Query.updateItemShared) : Query.createItem, connection);

                if (!append) command.Parameters.AddWithValue("@created_by", user_id);

                //Item id
                //Append only
                if (append) command.Parameters.AddWithValue("@item_id", i.id);

                //Item type
                command.Parameters.AddWithValue("@item_type", i.type);

                CollectionDetails cd = Collections.GetInfo(user_id, i.collection_id);

                //Is item shared or owned?
                if (ownership == OwnershipType.OwnerCreator || !append)
                {
                    //Check if user owns collection
                    if (cd.has_access)
                    {
                        //Check if item is already shared and trying to move to a shared category
                        if (cd.shared && i.shared_users != null)
                        {
                            return new JSONResult("Error: An item cannot be shared with individual users and also belong to a shared category");
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@collection_id", i.collection_id);
                        }
                    }
                    else
                    {
                        return new JSONResult("Error: You do not have access to this collection");
                    }
                }
                else if (ownership == OwnershipType.SharedCollection)
                {
                    //Do not allow user to move this item
                    return new JSONResult("Error: You cannot move an item in a shared collection which you do not own");
                }
                else if (ownership == OwnershipType.SharedItem)
                {
                    if (i.collection_id == -1)
                    {
                        //Clear moved collection field
                        Collections.MoveCollectionShared(user_id, i.id, -1);
                    }
                    else
                    {
                        //Check is user owns collection and collection is not shared
                        if (!cd.owned || cd.shared)
                        {
                            return new JSONResult("Error: You cannot move a shared item to a shared collection");
                        }
                        else
                        {
                            //Update moved collection
                            Collections.MoveCollectionShared(user_id, i.id, i.collection_id);
                        }

                    }
                }

                //Tags
                command.Parameters.AddWithValue("@tags", Join(i.tags));

                //Title
                command.Parameters.AddWithValue("@title", i.title);

                //Content
                command.Parameters.AddWithValue("@text", i.content);

                //Due date
                //Type 1 or 2 only
                if((ItemType)i.type == ItemType.Event && String.IsNullOrWhiteSpace(i.due_date))
                {
                    return new JSONResult("Error: Event date and time cannot be empty");
                }

                DateTime due_date;
                
                if(!String.IsNullOrWhiteSpace(i.due_date) && !DateTime.TryParse(i.due_date, out due_date))
                {
                    return new JSONResult("Error: Please enter a valid date and time");
                }

                command.Parameters.AddWithValue("@due_date",
                    (!String.IsNullOrWhiteSpace(i.due_date)) ? DateTime.Parse(i.due_date) : (object)DBNull.Value
                    );

                //Recurrence type
                //Type 2 only
                command.Parameters.AddWithValue("@recurrence_type",
                ((ItemType)i.type == ItemType.Event) ? i.recurrence_type : 0
                );

                //Remind
                //Type 2 only
                command.Parameters.AddWithValue("@remind",
                ((ItemType)i.type == ItemType.Event) ? i.remind : 0);

                //Remind before hrs
                //Type 2 only
                command.Parameters.AddWithValue("@remind_before_hrs", i.remind_before_hrs);

                connection.Open();

                if (!append)
                {
                    SqlDataReader reader = command.ExecuteReader();

                    try
                    {
                        while (reader.Read())
                        {
                            i.id = Convert.ToInt32(reader[0]); ;
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
                else
                {
                    command.ExecuteNonQuery();
                }

                //Configure sharing
                JSONResult s = Share.ItemConfigure(i.id, user_id, i.shared_users);

                if (!s.ok) return s;
            }

            return new JSONResult(true);
        }

        public static string Join(string[] arr)
        {
            string result = "";

            if (arr == null)
                return "";

            for(int i = 0; i < arr.Length; i++)
            {
                if(!String.IsNullOrWhiteSpace(arr[i]))
                {
                    result += arr[i];
                    if (i < arr.Length - 1) result += ',';
                }
            }

            return result;
        }
    }    
}
