namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    public partial class SearchModelOptions
    {

        /// <summary>
        /// Initializes a new instance of the SearchOptions class.
        /// </summary>
        /// <param name="modelFilterType">Possible values include: 'interface',
        /// 'capabilityModel'</param>
        public SearchModelOptions(string searchKeyword = default(string), string modelFilterType = default(string), string continuationToken = default(string), int? pageSize = default(int?))
        {
            SearchKeyword = searchKeyword;
            ModelFilterType = modelFilterType;
            ContinuationToken = continuationToken;
            PageSize = pageSize;
        }

        public string SearchKeyword { get; set; }

        public string ModelFilterType { get; set; }

        public string ContinuationToken { get; set; }

        public int? PageSize { get; set; }

    }
}
