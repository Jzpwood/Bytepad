using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public class Query
    {
        public const string connectionString = "Data Source=localhost\\SQLEXPRESS;Database=bytepad;Trusted_Connection=True;";
        public const string loginRequestQuery = "SELECT id, username, password_hash FROM users WHERE username = @login_username AND password_hash = @password_hash";
        public const string sessionRequestQuery = "SELECT session_id, user FROM sessions WHERE session_id = @session_id";
        public const string newSessionQuery = "INSERT INTO sessions (session_id, user_id, expiry_datetime) VALUES (@session_id, @user, @expiry)";
        public const string logEventQuery = "INSERT INTO logs (event_type, event_details, event_datetime) VALUES (@type, @details, @datetime)";
        public const string validateSessionQuery = @"SELECT session_id, user_id, username FROM sessions 
                                                    INNER JOIN users ON users.id = user_id
                                                     WHERE session_id = @session_id AND expiry_datetime > GETDATE()";
        public const string getCollectionsQuery = @"SELECT id, name FROM collections WHERE user_id = @user_id ORDER BY name ASC";
        public const string getSharedCollectionsQuery = @"SELECT name, collections.id FROM collection_shares
                                                    INNER JOIN collections ON collections.id = collection_shares.root_id
                                                    WHERE shared_user = @user_id AND accepted = 1";
        public const string invalidateSessionQuery = "UPDATE sessions SET expiry_datetime = GETDATE() WHERE session_id = @session_id";
        public const string get5Recent = @"SELECT TOP 5 items.item_type as type_id, items.id, (CASE WHEN item_type = 0 THEN 'item-tag-note' WHEN item_type = 1 THEN 'item-tag-todo' WHEN item_type = 2 THEN 'item-tag-event' END) AS item_type, title, COALESCE(due_date, modified_on) AS date, CASE WHEN shares.id IS NOT NULL THEN 1 ELSE 0 END AS shared_with
                                            FROM items 
                                            INNER JOIN users ON created_by = users.id 
                                            LEFT OUTER JOIN shares ON shares.item_id = items.id
                                            WHERE (users.id = @user_id OR shares.user_id = @user_id)
                                            ORDER BY modified_on DESC";
        
        public const string get5Upcoming = @"SELECT TOP 5 items.item_type as type_id, items.id, 
                                            (CASE WHEN item_type = 0 THEN 'item-tag-note' WHEN item_type = 1 THEN 'item-tag-todo' WHEN item_type = 2 THEN 'item-tag-event' END) AS item_type, title,
                                             due_date AS date, CASE WHEN shares.id IS NOT NULL THEN 1 ELSE 0 END AS shared_with
                                            FROM items INNER JOIN users ON created_by = users.id
                                            LEFT OUTER JOIN shares ON shares.item_id = items.id
                                            WHERE (users.id = @user_id OR shares.user_id = @user_id) AND item_type = 2 
                                                AND due_date >= (SELECT CAST(GETDATE() AS Date ))
	                                        ORDER BY due_date ASC
                                    
                                            ";

        public const string getItemSingle = @"SELECT 
                                            items.id,
                                            item_type,
                                            (SELECT users.username FROM users WHERE users.id = created_by) AS created_by,
                                            collections.name,
                                            collections.id collection_id,
                                            tag_ids tags,
                                            convert(varchar, items.created_on, 6) AS created_date,
                                            convert(varchar, items.modified_on, 6) AS modified_date,
                                            items.title,
                                            text,
                                            COALESCE(FORMAT(due_date, 'yyyy-MM-ddTHH:mm'),'') AS due_date,
                                            reccurence_type,
                                            remind,
                                            remind_before_hrs,
                                            completed_on,
                                            deleted

                                              FROM [bytepad].[dbo].[items]

                                              INNER JOIN collections ON collections.id = collection_id

                                              WHERE items.id = @item_id";

        public const string checkUsername = @"SELECT id FROM users WHERE username = @username";

        public const string updateItem = @"UPDATE items
                                            SET
                                            item_type = @item_type,
                                            collection_id = @collection_id,
                                            tag_ids = @tags,
                                            modified_on = GETDATE(),
                                            title = @title,
                                            text = @text,
                                            due_date = @due_date,
                                            reccurence_type = @recurrence_type,
                                            remind = @remind,
                                            remind_before_hrs = @remind_before_hrs
                                            WHERE id = @item_id";

        public const string updateItemShared = @"UPDATE items
                                            SET
                                            item_type = @item_type,
                                            tag_ids = @tags,
                                            modified_on = GETDATE(),
                                            title = @title,
                                            text = @text,
                                            due_date = @due_date,
                                            reccurence_type = @recurrence_type,
                                            remind = @remind,
                                            remind_before_hrs = @remind_before_hrs
                                            WHERE id = @item_id";

        public const string createItem = @"INSERT INTO items (item_type, created_by, collection_id, tag_ids, created_on, modified_on, title, text, due_date, reccurence_type, remind, remind_before_hrs, deleted)
                                            VALUES(@item_type, @created_by, @collection_id, @tags, GETDATE(), GETDATE(), @title, @text, @due_date, @recurrence_type, @remind, @remind_before_hrs, 0) SELECT SCOPE_IDENTITY();";

        public const string validateOwnership = @"SELECT items.id,

                                                CASE WHEN
                                                items.created_by = @user_id THEN 1
                                                WHEN
                                                (SELECT shared_user FROM collection_shares WHERE shared_user = @user_id AND items.collection_id = root_id) IS NOT NULL
												OR
												(SELECT collections.user_id FROM collections WHERE collections.id = items.collection_id) = @user_id THEN 2
                                                WHEN
                                                (SELECT shares.user_id FROM shares WHERE shares.user_id = @user_id AND shares.item_id = items.id) = @user_id THEN 3
                                                ELSE 0 END AS owned

                                                FROM items

                                                WHERE items.id = @item_id";

        public const string getAllTags = @"SELECT tag_ids FROM items WHERE

                                            tag_ids IS NOT NULL AND

                                            (created_by = @user_id

                                            OR

                                            id IN (SELECT item_id FROM shares WHERE user_id = @user_id))";
        public const string createCollection = @"INSERT INTO collections (name, user_id, created_on) VALUES (@name, @user_id, GETDATE())";
        public const string updateCollectionName = @"UPDATE collections 
                                                    SET name = @name
                                                    WHERE user_id = @user_id AND id = @id";
        public const string deleteItem = @"UPDATE ITEMS SET deleted = 1 WHERE id = @item_id";
        public const string undeleteItem = @"UPDATE ITEMS SET deleted = 0 WHERE id = @item_id";
        public const string completeItem = @"UPDATE ITEMS SET completed_on = @completed_on WHERE id = @item_id";
        public const string getSharedUsers = @"SELECT 
                                            shares.user_id AS id,
                                            users.username AS username

                                            FROM items

                                            INNER JOIN shares ON items.id = shares.item_id
                                            INNER JOIN users ON users.id = shares.user_id

                                            WHERE items.id = @item_id";
        public const string isInSharedCollection = @"SELECT items.id FROM items
                                            INNER JOIN collection_shares ON items.collection_id = collection_shares.root_id
                                            WHERE items.id = @item_id";
    }

}
