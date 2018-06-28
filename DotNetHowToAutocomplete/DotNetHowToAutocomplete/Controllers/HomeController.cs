using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Configuration;

namespace AutocompleteTutorial.Controllers
{
    public class HomeController : Controller
    {
        private static SearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;
        private static string IndexName = "nycjobs";

        public static string errorMessage;

        private void InitSearch()
        {
                string searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
                string apiKey = ConfigurationManager.AppSettings["SearchServiceApiKey"];

                // Create a reference to the NYCJobs index
                _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
                _indexClient = _searchClient.Indexes.GetClient(IndexName);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult IndexJavaScript()
        {
            return View();
        }

        public ActionResult Suggest(bool highlights, bool fuzzy, string term)
        {
            InitSearch();

            // Call suggest API and return results
            SuggestParameters sp = new SuggestParameters()
            {
                UseFuzzyMatching = fuzzy,
                Top = 5
            };

            if (highlights)
            {
                sp.HighlightPreTag = "<b>";
                sp.HighlightPostTag = "</b>";
            }

            DocumentSuggestResult suggestResult = _indexClient.Documents.Suggest(term, "sg",sp);

            // Convert the suggest query results to a list that can be displayed in the client.
            List<string> suggestions = suggestResult.Results.Select(x => x.Text).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = suggestions
            };
        }

        public ActionResult AutoComplete(string term)
        {
            InitSearch();
            //Call autocomplete API and return results
            AutocompleteParameters ap = new AutocompleteParameters()
            {
                AutocompleteMode = AutocompleteMode.OneTermWithContext,
                UseFuzzyMatching = false,
                Top = 5
            };
            AutocompleteResult autocompleteResult = _indexClient.Documents.Autocomplete(term, "sg", ap);

            // Conver the Suggest results to a list that can be displayed in the client.
            List<string> autocomplete = autocompleteResult.Results.Select(x => x.Text).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = autocomplete
            };
        }

        public ActionResult Facets()
        {
            InitSearch();

            // Call suggest API and return results
            SearchParameters sp = new SearchParameters()
            {
                Facets = new List<string> { "agency,count:500" },
                Top = 0
            };


            DocumentSearchResult searchResult = _indexClient.Documents.Search("*", sp);

            // Convert the suggest query results to a list that can be displayed in the client.

            List<string> facets = searchResult.Facets["agency"].Select(x => x.Value.ToString()).ToList();
            return new JsonResult
            {
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Data = facets
            };
        }
    }
}