using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public enum sortMode
    {
        DateModified,
        DateDue
    }
    public class QueryBuilder
    {
        public int optionTop = 0;
        public string optionCollection;
        public string optionTags;
        public int[] optionTypes;
        public int userID;

        public sortMode orderMode;

        public string QueryList { get { return GetQueryList(); } }
        public string QuerySingle { get { return GetQuerySingle(); } }

        public QueryBuilder()
        {
        }

        public string GetQuerySingle()
        {
            return "";
        }

        public string GetQueryList()
        {
            string top = optionTop > 0 ? "TOP " + optionTop : "";

            string query = $@"
                SELECT {top} items.id, 
                (CASE WHEN item_type = 0 THEN 'item-tag-note' WHEN item_type = 1 THEN 'item-tag-todo' WHEN item_type = 2 THEN 'item-tag-event' END) AS item_type, title,
                convert(varchar, due_date, 6) AS date, CASE WHEN shares.id IS NOT NULL THEN 1 ELSE 0 END AS shared_with
                FROM items INNER JOIN users ON created_by = users.id
                LEFT OUTER JOIN shares ON shares.item_id = items.id
                WHERE (users.id = @user_id OR shares.user_id = 2) AND item_type = 2 
                    AND due_date >= (SELECT CAST(GETDATE() AS Date ))
	            ORDER BY due_date ASC
            ";


            return "";
        }
    }
}
