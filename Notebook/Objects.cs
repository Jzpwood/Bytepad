using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public class JSONResult
    {
        public bool ok { get; set; }
        public string error { get; set; }

        public object data { get; set; }

        public JSONResult(bool ok)
        {
            this.ok = ok;
        }

        public JSONResult(bool ok, object data)
        {
            this.data = data;
            this.ok = ok;
        }

        public JSONResult(string error)
        {
            this.error = error;
            ok = false;
        }
    }
    public class ItemCommit
    {
        public string session_id { get; set; }
        public bool append { get; set; }
        public bool close { get; set; }
        public Item item { get; set; }
    }

    public class ItemAction
    {
        public string session_id { get; set; }
        public int item_id { get; set; }
        public bool delete { get; set; }
        public bool undelete { get; set; }
        public bool complete { get; set; }
        public bool uncomplete { get; set; }
    }

    public enum RecurringType
    {
        None,
        Daily,
        Every_other_day,
        Weekly,
        Two_Weekly,
        Four_Weekly,
        Thirty_Days,
        Monthly
    }

    public enum ItemType
    {
        Note,
        Todo,
        Event
    }

    public class ItemRequest
    {
        public int list { get; set; }
        public string session_id { get; set; }
        public int item_id { get; set; }
        public int item_type { get; set; }
        public int category { get; set; }
        public string search_str { get; set; }
        public string[] tags { get; set; }
        public string date_start { get; set; }
        public string date_end { get; set; }
    }

    public class Item
    {
        public int id { get; set; }
        public int type { get; set; }
        public string created_by { get; set; }
        public string collection { get; set; }
        public int collection_id { get; set; }
        public string[] tags { get; set; }
        public string created_on { get; set; }
        public string modified_on { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string due_date { get; set; }
        public string[] shared_users { get; set; }
        public int recurrence_type { get; set; }
        public int remind { get; set; }
        public int remind_before_hrs { get; set; }
        public string completed_on { get; set; }
        public int secondary_owner { get; set; }
        public int shared_item { get; set; }
        public Collection[] collections { get; set; }
        public string friendly_due_date { get; set; }
        public bool locked_out { get; set; }
    }

    public class LoginRequest
    {
        public string login_username { get; set; }
        public string login_password { get; set; }
    }

    public class HomepageView
    {
        public string username { get; set; }
        public string[] tags { get; set; }
        public Collection[] collections_list { get; set; }
        public TableItem[] recents_list { get; set; }
        public TableItem[] upcoming_list { get; set; }
        public HomepageView()
        {

        }
    }

    public class Collection
    {
        public string name { get; set; }
        public int id { get; set; }
        public bool shared { get; set; }

        public Collection(string name, int id, bool shared)
        {
            this.id = id;
            this.name = name;
            this.shared = shared;
        }
    }

    public class ListedItem
    {
        public string type { get; set; }
        public string name { get; set; }
        public string date { get; set; }
        public int id { get; set; }
        public int shared { get; set; }
        public ListedItem(int id, string type, string name, string date, int shared)
        {
            this.name = name;
            this.type = type;
            this.date = date;
            this.id = id;
            this.shared = shared;
        }
        public ListedItem()
        {
        }
    }
}
