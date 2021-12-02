using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public static class Homepage
    {
        public enum HomepageList
        {
            Recent,
            Upcoming
        }

        public static string dateLabel(DateTime t, int type)
        {
            var daydiff_minus = DateTime.Now - t;
            var daydiff_plus = t - DateTime.Now;

            if(type < 2)
            {
                if (daydiff_minus.TotalDays > 10)
                {
                    return t.ToString("dd MMM yy");
                }
                else
                {
                    return Math.Ceiling(daydiff_minus.TotalDays) + " days ago";
                }
            }
            else
            {
                if (daydiff_plus.TotalDays > 10)
                {
                    return t.ToString("dd MMM yy");
                }
                else
                {
                    double d = Math.Ceiling(daydiff_plus.TotalDays);

                    if (d < 0) return $"{-d} days ago";
                    else return $"in {d} days";
                }
            }
        }

        public static string[] OrganiseTags(List<string> tags)
        {
            Dictionary<string, int> pairs = new Dictionary<string, int>();

            foreach(string s in tags)
            {
                if (pairs.ContainsKey(s))
                    pairs[s]++;
                else
                    pairs.Add(s, 1);
            }

            var sorted = pairs.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            return sorted.Keys.ToArray();
        }

         public static HomepageView GetHomepage(int user_id, string username)
        {
            ItemQuery recentsQuery = new ItemQuery
            {
                top_items = 10,
                user_id = user_id,
                order = ItemOrdering.ModifiedRecent,
                collection_id = -1
            };

            ItemQuery upcomingQuery = new ItemQuery
            {
                top_items = 5,
                user_id = user_id,
                order = ItemOrdering.DueSoon,
                collection_id = -1
            };
            
            return new HomepageView
            {
                username = username,
                //tags = GetTags(user_id),
                collections_list = Collections.GetCollections(user_id),
                recents_list = Fetch.Items(recentsQuery, out _),
                upcoming_list = Fetch.Items(upcomingQuery, out _)
            };
        }
    }
}
