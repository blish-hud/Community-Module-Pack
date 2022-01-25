using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universal_Search_Module.Controls.SearchResultItems;

namespace Universal_Search_Module.Services.SearchHandler {
    public abstract class SearchHandler {
        public const int MAX_RESULT_COUNT = 3;

        public abstract Task Initialize(Action<string> progress);

        public abstract IEnumerable<SearchResultItem> Search(string searchText);
    }

}
