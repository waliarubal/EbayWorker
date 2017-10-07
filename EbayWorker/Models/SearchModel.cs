using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using HtmlAgilityPack;
using System;
using System.Text;
using System.Threading.Tasks;

namespace EbayWorker.Models
{
    public enum Category: int
    {
        Books = 267
    }

    public enum SearchStatus: byte
    {
        NotStarted,
        Working,
        Complete,
        Failed
    }

    public class SearchModel: NotificationBase
    {
        const int ResultsPerPage = 200;

        string _keywoard;
        Category _category;
        SearchStatus _status;
        readonly Type _conditionType;
        BookCollection _books;

        public SearchModel()
        {
            _books = new BookCollection();
            _conditionType = typeof(BookCondition);
        }

        #region properties

        public SearchStatus Status
        {
            get { return _status; }
            private set { Set(nameof(Status), ref _status, value); }
        }

        public string Keywoard
        {
            get { return _keywoard; }
            set { Set(nameof(Keywoard), ref _keywoard, value); }
        }

        public Category Category
        {
            get { return _category; }
            set { Set(nameof(Category), ref _category, value); }
        }

        public BookCollection Books
        {
            get { return _books; }
        }


        #endregion

        internal void Search(ref HtmlDocument parser, SearchFilter filter, int parallelQueries, bool scrapBooksInParallel)
        {
            var client = new ExtendedWebClient(parallelQueries);

            var queryStringBuilder = new StringBuilder();
            queryStringBuilder.AppendFormat("_nkw={0}&", Keywoard);
            queryStringBuilder.AppendFormat("_sacat={0}&", (int)Category);
            queryStringBuilder.AppendFormat("_ipg={0}", ResultsPerPage);
            var location = filter.GetLocation();
            if (location > 0)
                queryStringBuilder.AppendFormat("LH_LocatedIn=1&_salic={0}&LH_SubLocation=1", location);
            if (filter.IsPriceFiltered)
            {
                queryStringBuilder.Append("&_mPrRngCbx=1");
                if (filter.MinimumPrice > 0)
                    queryStringBuilder.AppendFormat("&_udlo={0}", filter.MinimumPrice);
                if (filter.MaximumPrice > 0)
                    queryStringBuilder.AppendFormat("&_udhi={0}", filter.MaximumPrice);
            }
                
            if (filter.IsAuction)
                queryStringBuilder.Append("&LH_Auction=1");
            if (filter.IsBuyItNow)
                queryStringBuilder.Append("&LH_BIN=1");
            if (filter.IsClassifiedAds)
                queryStringBuilder.Append("&LH_CAds=1");

            var url = new UriBuilder();
            url.Scheme = "https";
            url.Host = "www.ebay.com";
            url.Path = "sch/i.html";
            url.Query = queryStringBuilder.ToString();

            Status = SearchStatus.Working;
            _books.Clear();

            var rootNode = Load(ref client, ref parser, url.Uri);
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            // change to inner node to decrease DOM traversal
            rootNode = rootNode.SelectSingleNode(".//ul[@id='ListViewInner']");
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            var nodes = rootNode.SelectNodes(".//li[starts-with(@id,'item')]");
            if (nodes == null || nodes.Count == 0)
            {
                // no listing found
                Status = SearchStatus.Complete;
                return;
            }

            HtmlNode innerNode;
            foreach (HtmlNode node in nodes)
            {
                innerNode = node.SelectSingleNode(".//a[@class='vip']");
                if (innerNode == null)
                    continue;

                var book = new BookModel();
                book.Title = innerNode.InnerText;
                book.Url = new Uri(innerNode.Attributes["href"].Value);

                // last part of URL stores eBay item code
                var urlParts = book.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (urlParts.Length > 0)
                    book.Code = urlParts[urlParts.Length - 1];

                // extract location
                innerNode = node.SelectSingleNode(".//ul[starts-with(@class,'lvdetails')]/li");
                if (innerNode != null)
                {
                    var bookLocation = innerNode.InnerText;
                    if (string.IsNullOrEmpty(bookLocation))
                        continue;
                    else
                        bookLocation = bookLocation.Trim().Remove(0, "From ".Length);

                    // ignore books which don't match location filter
                    if (!string.IsNullOrEmpty(filter.Location) && !bookLocation.Equals(filter.Location))
                        continue;

                    book.Location = bookLocation.Trim().Replace("From ", string.Empty);
                }

                // eBay shows advertisements, ignore them
                if (book.Url.Host.Equals(url.Host, StringComparison.InvariantCultureIgnoreCase))
                    _books.Add(book);
            }

            // process each book in parallel
            if (scrapBooksInParallel)
            {
                var parallelOptions = new ParallelOptions();
                parallelOptions.MaxDegreeOfParallelism = parallelQueries;

                Parallel.ForEach(_books.Items, parallelOptions, (currentBook) => ProcessBook(currentBook, filter, parallelQueries));
            }
            else
            {
                for(var index = _books.Count -1; index >= 0; index--)
                {
                    var book = _books[index];
                    ProcessBook(book, filter);
                }
            }

            // apply filter
            for(var index = _books.Count -1; index >= 0; index--)
            {
                var book = _books[index];
                if (book.Status == SearchStatus.Complete && IncludeBook(book, filter) == false)
                    _books.Remove(book);
            }

            // mark query complete only when data for all books is scraped
            if (Status != SearchStatus.Failed)
                Status = SearchStatus.Complete;
        }

        void ProcessBook(BookModel currentBook, SearchFilter filter, int pallelWebRequests = 1)
        {
            var bookParser = new HtmlDocument();
            var client = new ExtendedWebClient(pallelWebRequests);

            currentBook.Status = SearchStatus.Working;
            var rootNode = Load(ref client, ref bookParser, currentBook.Url);
            if (rootNode == null)
            {
                currentBook.Status = Status = SearchStatus.Failed;
                return;
            }

            var htmlNode = rootNode.SelectSingleNode(".//div[@id='BottomPanel']//h2[@itemprop='productID']");
            if (htmlNode != null)
                currentBook.Isbn = htmlNode.InnerText;

            // change to inner node to decrease DOM traversal
            rootNode = rootNode.SelectSingleNode(".//div[@id='CenterPanelInternal']");
            if (rootNode == null)
            {
                currentBook.Status = Status = SearchStatus.Failed;
                return;
            }

            var innerRoot = rootNode.SelectSingleNode(".//div[@id='LeftSummaryPanel']");
            if (innerRoot == null)
            {
                currentBook.Status = Status = SearchStatus.Failed;
                return;
            }

            htmlNode = innerRoot.SelectSingleNode(".//div[@itemprop='itemCondition']");
            if (htmlNode != null)
            {
                BookCondition condition;
                if (Enum.TryParse(htmlNode.InnerText.Replace(" ", string.Empty), out condition))
                    currentBook.Condition = condition;
            }

            var nodes = innerRoot.SelectNodes(".//span[@itemprop='price']");
            if (nodes != null)
            {
                // in case of books with both buy-now and bid-now option, pick buy now price
                htmlNode = nodes[filter.IsBuyItNow && nodes.Count > 1 ? 1 : 0];

                currentBook.Price = decimal.Parse(htmlNode.Attributes["content"].Value);

                // try to extract price if current price is not in USD
                htmlNode = htmlNode.ParentNode.SelectSingleNode(".//span[@id='convbinPrice']");
                if (htmlNode != null && htmlNode.HasChildNodes)
                    currentBook.Price = ExtractDecimal(htmlNode.FirstChild.InnerText);
            }
            else
            {
                // retrieve discounted price
                htmlNode = rootNode.SelectSingleNode(".//span[@id='mm-saleDscPrc']");
                if (htmlNode != null)
                    currentBook.Price = ExtractDecimal(htmlNode.InnerText);
            }

            innerRoot = rootNode.SelectSingleNode(".//div[@id='RightSummaryPanel']");
            if (innerRoot == null)
            {
                currentBook.Status = Status = SearchStatus.Failed;
                return;
            }

            // seller details
            currentBook.Seller = new SellerModel();

            htmlNode = innerRoot.SelectSingleNode(".//span[@class='mbg-nw']");
            if (htmlNode != null)
                currentBook.Seller.Name = htmlNode.InnerText;

            htmlNode = innerRoot.SelectSingleNode(".//a[starts-with(@title,'feedback score:')]");
            if (htmlNode != null)
                currentBook.Seller.FeedbackScore = long.Parse(htmlNode.InnerText);

            htmlNode = innerRoot.SelectSingleNode(".//div[@id='si-fb']");
            if (htmlNode != null)
            {
                var parts = htmlNode.InnerText.Split('%');
                if (parts.Length > 0)
                    currentBook.Seller.FeedbackPercent = decimal.Parse(parts[0]);
            }

            currentBook.Status = SearchStatus.Complete;
        }

        bool IncludeBook(BookModel book, SearchFilter filter)
        {
            var seller = book.Seller;

            if (filter.CheckFeedbackScore && filter.FeedbackScore > seller.FeedbackScore)
                return false;

            if (filter.CheckFeedbackPercent && filter.FeedbackPercent > seller.FeedbackPercent)
                return false;

            if (filter.CheckAllowedSellers && filter.AllowedSellers != null && !filter.AllowedSellers.Contains(seller.Name))
                return false;

            if (filter.CheckRestrictedSellers && filter.RestrictedSellers != null && filter.RestrictedSellers.Contains(seller.Name))
                return false;

            return true;
        }

        decimal ExtractDecimal(string text)
        {
            var priceBuilder = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '.' || (c >= '0' && c <= '9'))
                    priceBuilder.Append(c);
            }
            if (priceBuilder.Length > 0)
                return decimal.Parse(priceBuilder.ToString());

            return default(decimal);
        }

        HtmlNode Load(ref ExtendedWebClient client, ref HtmlDocument parser, Uri uri)
        {
            try
            {
                var html = client.DownloadString(uri);
                parser.LoadHtml(html);
                return parser.DocumentNode;
            }
            catch
            {
                Status = SearchStatus.Failed;
                return null;
            }
        }

    }
}
