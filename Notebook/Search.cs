namespace Notebook
{
    public class searchRequest
    {
        public string session_id { get; set; }
        public string search_text { get; set; }
    }

    public class searchResult
    {

    }

    public static class Search
    {
        public static searchResult Get(searchRequest req)
        {
            searchResult result = new searchResult();

            return result;
        }
    }
}
