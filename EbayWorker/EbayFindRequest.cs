using EbayWorker.Models;
using NullVoidCreations.WpfHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace EbayWorker
{
    class EbayFindRequest
    {
        const string ENDPOINT_PRODUCTION = "https://svcs.ebay.com/services/search/FindingService/v1";
        const string ENDPOINT_SANDBOX = "https://svcs.sandbox.ebay.com/services/search/FindingService/v1";
        const string DATA_FORMAT = "XML";

        readonly string _isbn, _appId;
        readonly SearchFilter _filter;
        bool _isAllowedSellersFilterApplied, _isRestrictedSellersFilterApplied;
        static readonly Dictionary<string, string> _locationToGlobalIdMapping;
        SearchStatus _status;
        string _errorMessage;

        #region constructor/destructor

        static EbayFindRequest()
        {
            _locationToGlobalIdMapping = new Dictionary<string, string>
            {
                { "United States", "EBAY-US" },
                { "Canada", "EBAY-ENCA" },
                { "United Kingdom", "EBAY-GB" }
            };
        }


        public EbayFindRequest(string appId, string isbn, SearchFilter filter)
        {
            _appId = appId;
            _isbn = isbn;
            _filter = filter;
        }

        #endregion

        #region properties

        public bool IsAllowedSellersFilterApplied
        {
            get { return _isAllowedSellersFilterApplied; }
        }

        public bool IsRestrictedSellersFilterApplied
        {
            get { return _isRestrictedSellersFilterApplied; }
        }

        public SearchStatus Status
        {
            get { return _status; }
        }

        #endregion

        #region private methods

        string ToXml()
        {
            var xmlBuilder = new StringBuilder();
            xmlBuilder.AppendLineFormatted("<?xml version='1.0' encoding='utf - 8'?>");
            xmlBuilder.AppendLineFormatted("<findItemsAdvancedRequest xmlns='http://www.ebay.com/marketplace/search/v1/services'>");
            xmlBuilder.AppendLineFormatted("  <categoryId>{0}</categoryId>", (int)Category.Books);
            xmlBuilder.AppendLineFormatted("  <descriptionSearch>{0}</descriptionSearch>", false);

            xmlBuilder.AppendLineFormatted("  <itemFilter>");
            xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "ListedIn");
            xmlBuilder.AppendLineFormatted("    <value>{0}</value>", GetGlobalId(_filter.Location));
            xmlBuilder.AppendLineFormatted("  </itemFilter>");

            /*
            xmlBuilder.AppendLineFormatted("  <itemFilter>");
            xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "ListingType");
            if (_filter.IsAuction && !_filter.IsBuyItNow)
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", "Auction");
            if (!_filter.IsAuction && _filter.IsBuyItNow)
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", "FixedPrice");
            if (_filter.IsAuction && _filter.IsBuyItNow)
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", "AuctionWithBIN");
            if (_filter.IsClassifiedAds)
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", "Classified");
            xmlBuilder.AppendLineFormatted("  </itemFilter>");
            */

            if (_filter.IsPriceFiltered)
            {
                xmlBuilder.AppendLineFormatted("  <itemFilter>");
                xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "MinPrice");
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", _filter.MinimumPrice);
                xmlBuilder.AppendLineFormatted("  </itemFilter>");
                xmlBuilder.AppendLineFormatted("  <itemFilter>");
                xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "MaxPrice");
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", _filter.MaximumPrice);
                xmlBuilder.AppendLineFormatted("  </itemFilter>");
            }

            if (_filter.CheckFeedbackScore)
            {
                xmlBuilder.AppendLineFormatted("  <itemFilter>");
                xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "FeedbackScoreMin");
                xmlBuilder.AppendLineFormatted("    <value>{0}</value>", _filter.FeedbackScore);
                xmlBuilder.AppendLineFormatted("  </itemFilter>");
            }

            if (_filter.CheckAllowedSellers && 
                _filter.AllowedSellers != null && 
                _filter.AllowedSellers.Count > 0 &&
                _filter.AllowedSellers.Count <= 100)
            {
                _isAllowedSellersFilterApplied = true;

                xmlBuilder.AppendLineFormatted("  <itemFilter>");
                xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "Seller");
                foreach (var seller in _filter.RestrictedSellers)
                    xmlBuilder.AppendLineFormatted("    <value>{0}</value>", seller);
                xmlBuilder.AppendLineFormatted("  </itemFilter>");
            }
            
            if (_filter.CheckRestrictedSellers && 
                _filter.RestrictedSellers != null && 
                _filter.RestrictedSellers.Count > 0 &&
                _filter.RestrictedSellers.Count <= 100)
            {
                _isRestrictedSellersFilterApplied = true;

                xmlBuilder.AppendLineFormatted("  <itemFilter>");
                xmlBuilder.AppendLineFormatted("    <name>{0}</name>", "ExcludeSeller");
                foreach(var seller in _filter.RestrictedSellers)
                    xmlBuilder.AppendLineFormatted("    <value>{0}</value>", seller);
                xmlBuilder.AppendLineFormatted("  </itemFilter>");
            }

            xmlBuilder.AppendLineFormatted("  <keywords>{0}</keywords>", _isbn);
            xmlBuilder.AppendLineFormatted("  <outputSelector>{0}</outputSelector>", "ConditionHistogram");
            xmlBuilder.AppendLineFormatted("  <outputSelector>{0}</outputSelector>", "SellerInfo");
            xmlBuilder.AppendLineFormatted("  <paginationInput>");
            xmlBuilder.AppendLineFormatted("    <entriesPerPage>{0}</entriesPerPage>", 100);
            xmlBuilder.AppendLineFormatted("  </paginationInput>");
            xmlBuilder.AppendLineFormatted("  <sortOrder>{0}</sortOrder>", "PricePlusShippingLowest");
            xmlBuilder.AppendLineFormatted("</findItemsAdvancedRequest>");
            return xmlBuilder.ToString();
        }

        void ParseResponse(string responseXml)
        {
            var xml = new XmlDocument();
            xml.LoadXml(responseXml);

            var namespaceManager = new XmlNamespaceManager(xml.NameTable);
            namespaceManager.AddNamespace(string.Empty, "http://www.ebay.com/marketplace/search/v1/services");

            var root = xml.DocumentElement;

            _status =root.SelectSingleNode("ack").InnerText.Equals("Success") ? SearchStatus.Complete : SearchStatus.Failed;

            if (_status == SearchStatus.Failed)
            {
                return;
            }
        }

        string GetGlobalId(string location)
        {
            if (_locationToGlobalIdMapping.ContainsKey(location))
                return _locationToGlobalIdMapping[location];

            return _locationToGlobalIdMapping["United States"];
        }

        #endregion

        public void GetResponse(bool useProductionEndpoint)
        {
            _status = SearchStatus.Working;
            _errorMessage = null;

            var endpoint = useProductionEndpoint ? new Uri(ENDPOINT_PRODUCTION) : new Uri(ENDPOINT_SANDBOX);

            var data = Encoding.UTF8.GetBytes(ToXml());

            var request = (HttpWebRequest)WebRequest.Create(endpoint);
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = "text/xml";
            request.ContentLength = data.Length;
            request.Headers.Add("X-EBAY-SOA-SERVICE-NAME", "FindingService");
            request.Headers.Add("X-EBAY-SOA-GLOBAL-ID", GetGlobalId(_filter.Location));
            request.Headers.Add("X-EBAY-SOA-MESSAGE-ENCODING", "UTF-8");
            request.Headers.Add("X-EBAY-SOA-OPERATION-NAME", "findItemsAdvanced");
            request.Headers.Add("X-EBAY-SOA-REQUEST-DATA-FORMAT", DATA_FORMAT);
            request.Headers.Add("X-EBAY-SOA-RESPONSE-DATA-FORMAT", DATA_FORMAT);
            request.Headers.Add("X-EBAY-SOA-SECURITY-APPNAME", _appId);

            // write post data
            using (var writer = request.GetRequestStream())
            {
                writer.Write(data, 0, data.Length);
            }

            string responseXml;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                responseXml = reader.ReadToEnd();
            }

            ParseResponse(responseXml);
        }

        public override string ToString()
        {
            return ToXml();
        }
    }
}
