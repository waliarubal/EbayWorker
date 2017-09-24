using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
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
        List<BookModel> _books;
        int _brandNew, _likeNew, _veryGood, _good, _acceptable;
        readonly Type _conditionType;

        public SearchModel()
        {
            _books = new List<BookModel>();
            _conditionType = typeof(BookCondition);
        }

        #region properties

        public SearchStatus Status
        {
            get { return _status; }
            private set { Set("Status", ref _status, value); }
        }

        public int BrandNewCount
        {
            get { return _brandNew; }
            private set { Set("BrandNewCount", ref _brandNew, value); }
        }

        public int LikeNewCount
        {
            get { return _likeNew; }
            private set { Set("LikeNewCount", ref _likeNew, value); }
        }

        public int VeryGoodCount
        {
            get { return _veryGood; }
            private set { Set("VeryGoodCount", ref _veryGood, value); }
        }

        public int GoodCount
        {
            get { return _good; }
            private set { Set("GoodCount", ref _good, value); }
        }

        public int AcceptableCount
        {
            get { return _acceptable; }
            private set { Set("AcceptableCount", ref _acceptable, value); }
        }

        public string Keywoard
        {
            get { return _keywoard; }
            set { Set("Keywoard", ref _keywoard, value); }
        }

        public Category Category
        {
            get { return _category; }
            set { Set("Category", ref _category, value); }
        }

        public IEnumerable<BookModel> Books
        {
            get { return _books; }
        }

        #endregion

        internal void Search(ref HtmlDocument parser, SearchFilter filter, int parallelQueries)
        {
            var client = new ExtendedWebClient(parallelQueries);

            var queryStringBuilder = new StringBuilder();
            queryStringBuilder.AppendFormat("_nkw={0}&", Keywoard);
            queryStringBuilder.AppendFormat("_sacat={0}&", (int)Category);
            queryStringBuilder.AppendFormat("_ipg={0}", ResultsPerPage);
            var location = filter.GetLocation();
            if (location > 0)
                queryStringBuilder.AppendFormat("LH_LocatedIn=1&_salic={0}&LH_SubLocation=1", location);
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
            Reset();

            var rootNode = Load(ref client, ref parser, url.Uri);
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            // change to inner node to decrease DOM traversal
            rootNode = rootNode.SelectSingleNode("//div[@id='CenterPanel']");
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            var nodes = rootNode.SelectNodes("//a[@class='vip']");
            if (nodes == null || nodes.Count == 0)
            {
                // no listing found
                Status = SearchStatus.Complete;
                return;
            }

            foreach (var node in nodes)
            {
                var book = new BookModel();
                book.Title = node.InnerText;
                book.Url = new Uri(node.Attributes["href"].Value);

                // last part of URL stores eBay item code
                var urlParts = book.Url.AbsolutePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (urlParts.Length > 0)
                    book.Code = urlParts[urlParts.Length - 1];

                // eBay shows advertisements, ignore them
                if (book.Url.Host.Equals(url.Host, StringComparison.InvariantCultureIgnoreCase))
                    AddBook(book);
            }

            // process each book in parallel
            var parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = parallelQueries;

            Parallel.ForEach(_books, parallelOptions, (currentBook) => ProcessBook(currentBook, filter, parallelQueries));

            // mark query complete only when data for all books is scraped
            if (Status != SearchStatus.Failed)
                Status = SearchStatus.Complete;
        }

        void ProcessBook(BookModel currentBook, SearchFilter filter, int pallelWebRequests)
        {
            var bookParser = new HtmlDocument();
            var client = new ExtendedWebClient(pallelWebRequests);

            var rootNode = Load(ref client, ref bookParser, currentBook.Url);
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            var htmlNode = rootNode.SelectSingleNode("//div[@id='BottomPanel']//h2[@itemprop='productID']");
            if (htmlNode != null)
                currentBook.Isbn = htmlNode.InnerText;

            // change to inner node to decrease DOM traversal
            rootNode = rootNode.SelectSingleNode("//div[@id='CenterPanelInternal']");
            if (rootNode == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            var innerRoot = rootNode.SelectSingleNode("//div[@id='LeftSummaryPanel']");
            if (innerRoot == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            htmlNode = innerRoot.SelectSingleNode("//div[@itemprop='itemCondition']");
            if (htmlNode != null)
            {
                BookCondition condition;
                if (Enum.TryParse(htmlNode.InnerText.Replace(" ", string.Empty), out condition))
                    currentBook.Condition = condition;

                switch (currentBook.Condition)
                {
                    case BookCondition.BrandNew:
                        BrandNewCount++;
                        break;

                    case BookCondition.LikeNew:
                        LikeNewCount++;
                        break;

                    case BookCondition.VeryGood:
                        VeryGoodCount++;
                        break;

                    case BookCondition.Good:
                        GoodCount++;
                        break;

                    case BookCondition.Acceptable:
                        AcceptableCount++;
                        break;
                }
            }

            htmlNode = innerRoot.SelectSingleNode("//span[@itemprop='price']");
            if (htmlNode != null)
            {
                currentBook.Price = decimal.Parse(htmlNode.Attributes["content"].Value);

                // try to extract price if current price is not in USD
                htmlNode = htmlNode.ParentNode.SelectSingleNode("//span[@id='convbinPrice']");
                if (htmlNode != null && htmlNode.HasChildNodes)
                    currentBook.Price = ExtractDecimal(htmlNode.FirstChild.InnerText);
            }
            else
            {
                // retrieve discounted price
                htmlNode = rootNode.SelectSingleNode("//span[@id='mm-saleDscPrc']");
                if (htmlNode != null)
                    currentBook.Price = ExtractDecimal(htmlNode.InnerText);
            }

            innerRoot = rootNode.SelectSingleNode("//div[@id='RightSummaryPanel']");
            if (innerRoot == null)
            {
                Status = SearchStatus.Failed;
                return;
            }

            // seller details
            currentBook.Seller = new SellerModel();

            htmlNode = innerRoot.SelectSingleNode("//span[@class='mbg-nw']");
            if (htmlNode != null)
                currentBook.Seller.Name = htmlNode.InnerText;

            htmlNode = innerRoot.SelectSingleNode("//a[starts-with(@title,'feedback score:')]");
            if (htmlNode != null)
                currentBook.Seller.FeedbackScore = long.Parse(htmlNode.InnerText);

            htmlNode = innerRoot.SelectSingleNode("//div[@id='si-fb']");
            if (htmlNode != null)
            {
                var parts = htmlNode.InnerText.Split('%');
                if (parts.Length > 0)
                    currentBook.Seller.FeedbackPercent = decimal.Parse(parts[0]);
            }

            if (!IncludeBook(currentBook, filter))
                RemoveBook(currentBook);
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

            if (filter.MaximumPrice > 0 && book.Price > filter.MaximumPrice)
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

        void Reset()
        {
            _books.Clear();
            RaisePropertyChanged("Books");
            BrandNewCount = LikeNewCount = VeryGoodCount = GoodCount = AcceptableCount = 0;
        }

        void AddBook(BookModel book)
        {
            _books.Add(book);
            RaisePropertyChanged("Books");
        }

        void RemoveBook(BookModel book)
        {
            switch (book.Condition)
            {
                case BookCondition.BrandNew:
                    if (BrandNewCount > 0)
                        BrandNewCount--;
                    break;

                case BookCondition.LikeNew:
                    if (LikeNewCount > 0)
                        LikeNewCount--;
                    break;

                case BookCondition.VeryGood:
                    if (VeryGoodCount > 0)
                        VeryGoodCount--;
                    break;

                case BookCondition.Good:
                    if (GoodCount > 0)
                        GoodCount--;
                    break;

                case BookCondition.Acceptable:
                    if (AcceptableCount > 0)
                        AcceptableCount--;
                    break;
            }
            _books.Remove(book);
            RaisePropertyChanged("Books");
        }

    }
}
