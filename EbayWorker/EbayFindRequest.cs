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
        IEnumerable<string> _errorMessages;

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

        public IEnumerable<string> Errors
        {
            get { return _errorMessages; }
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

        IList<string> GetErrors(XmlDocument xml, string xPath)
        {
            var errors = new List<string>();
            foreach (XmlNode errorNode in xml.SelectNodes(xPath))
                errors.Add(errorNode.InnerText);

            return errors;
        }

        bool IncludeBook(BookModel book, SearchFilter filter)
        {
            var seller = book.Seller;

            if (filter.CheckFeedbackScore && filter.FeedbackScore > seller.FeedbackScore)
                return false;

            if (filter.CheckFeedbackPercent && filter.FeedbackPercent > seller.FeedbackPercent)
                return false;

            if (!IsAllowedSellersFilterApplied && filter.CheckAllowedSellers && filter.AllowedSellers != null && !filter.AllowedSellers.Contains(seller.Name))
                return false;

            if (!IsRestrictedSellersFilterApplied && filter.CheckRestrictedSellers && filter.RestrictedSellers != null && filter.RestrictedSellers.Contains(seller.Name))
                return false;

            return true;
        }

        void ParseResponse(string responseXml, ref BookCollection books)
        {
            // get rid of namespaces
            responseXml = responseXml.Replace(" xmlns=\"", " whocares=\"");

            var xml = new XmlDocument();
            xml.LoadXml(responseXml);

            // trap API error
            var errors = GetErrors(xml, "errorMessage/error/message");
            if (errors.Count > 0)
            {
                _errorMessages = errors;
                _status = SearchStatus.Failed;
                return;
            }

            // trap API method call failure error
            var isComplete = xml.SelectSingleNode("findItemsAdvancedResponse/ack").InnerText.Equals("Success");
            if (!isComplete)
            {
                _errorMessages = GetErrors(xml, "findItemsAdvancedResponse/errorMessage/error/message");
                _status = SearchStatus.Failed;
                return;
            }

            foreach(XmlNode itemNode in xml.SelectNodes("findItemsAdvancedResponse/searchResult/item"))
            {
                var book = new BookModel();
                book.Status = _status;

                try
                {
                    book.Code = itemNode.SelectSingleNode("itemId").InnerText;
                    book.Title = itemNode.SelectSingleNode("title").InnerText;
                    book.Url = new Uri(itemNode.SelectSingleNode("viewItemURL").InnerText);
                    book.Isbn = _isbn;
                    book.Location = itemNode.SelectSingleNode("location").InnerText;
                    book.Seller = new SellerModel
                    {
                        Name = itemNode.SelectSingleNode("sellerInfo/sellerUserName").InnerText,
                        FeedbackScore = long.Parse(itemNode.SelectSingleNode("sellerInfo/feedbackScore").InnerText),
                        FeedbackPercent = decimal.Parse(itemNode.SelectSingleNode("sellerInfo/positiveFeedbackPercent").InnerText)
                    };
                    book.Price = decimal.Parse(itemNode.SelectSingleNode("sellingStatus/convertedCurrentPrice").InnerText);

                    var condition = itemNode.SelectSingleNode("condition/conditionId");
                    if (condition == null)
                        book.Condition = BookCondition.Unknown;
                    else
                    {
                        // exceptional conditions
                        var conditionNumber = int.Parse(condition.InnerText);
                        if (conditionNumber == 2750)
                            conditionNumber = (int)BookCondition.LikeNew;

                        book.Condition = (BookCondition)conditionNumber;
                    }

                    book.Status = SearchStatus.Complete;
                }
                catch(XmlException ex)
                {
                    book.Status = SearchStatus.Failed;
                }

                if (book.Status == SearchStatus.Complete && IncludeBook(book, _filter))
                    books.Add(book);
            }

            _status = SearchStatus.Complete;
        }

        string GetGlobalId(string location)
        {
            if (_locationToGlobalIdMapping.ContainsKey(location))
                return _locationToGlobalIdMapping[location];

            return _locationToGlobalIdMapping["United States"];
        }

        #endregion

        public void GetResponse(bool useProductionEndpoint, ref BookCollection books)
        {
            _status = SearchStatus.Working;
            _errorMessages = null;
            books.Clear();

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

            try
            {
                // write post data
                using (var writer = request.GetRequestStream())
                {
                    writer.Write(data, 0, data.Length);
                }
            }
            catch(WebException ex)
            {
                _errorMessages = new List<string> { ex.Message };
                _status = SearchStatus.Failed;
                return;
            }

            string responseXml;
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    responseXml = reader.ReadToEnd();
                }
            }
            catch(WebException ex)
            {
                if (ex.Response == null)
                {
                    _errorMessages = new List<string> { ex.Message };
                    _status = SearchStatus.Failed;
                    return;
                }

                using (var reader = new StreamReader(ex.Response.GetResponseStream()))
                {
                    responseXml = reader.ReadToEnd();
                }
            }

            ParseResponse(responseXml, ref books);
        }

        public override string ToString()
        {
            return ToXml();
        }
    }
}
