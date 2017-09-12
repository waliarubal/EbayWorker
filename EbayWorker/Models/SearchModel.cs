using EbayWorker.Helpers.Base;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

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

        internal void Search(ref HtmlWeb parser)
        {
            var url = new UriBuilder();
            url.Scheme = "https";
            url.Host = "www.ebay.com";
            url.Path = "sch/i.html";
            url.Query = string.Format("_nkw={0}&_sacat={1}", Keywoard, (int)Category);

            Status = SearchStatus.Working;
            Reset();

            var rootNode = Load(ref parser, url.Uri);
            if (rootNode == null)
                return;

            foreach (var node in rootNode.SelectNodes("//a[@class='vip']"))
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

            HtmlNode htmlNode;
            foreach(var book in Books)
            {
                rootNode = Load(ref parser, book.Url);
                if (rootNode == null)
                    continue;

                htmlNode = rootNode.SelectSingleNode("//div[@itemprop='itemCondition']");
                if (htmlNode != null)
                {
                    book.Condition = (BookCondition)Enum.Parse(_conditionType, htmlNode.InnerText.Replace(" ", string.Empty));
                    switch (book.Condition)
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

                htmlNode = rootNode.SelectSingleNode("//span[@itemprop='price']");
                if (htmlNode != null)
                    book.Price = decimal.Parse(htmlNode.Attributes["content"].Value);
                else
                {
                    // retrieve discounted price
                    htmlNode = rootNode.SelectSingleNode("//span[@id='mm-saleDscPrc']");
                    if (htmlNode != null)
                    {
                        var priceBuilder = new StringBuilder();
                        foreach(char c in htmlNode.InnerText)
                        {
                            if (char.IsNumber(c) || char.IsDigit(c))
                                priceBuilder.Append(c);
                        }
                        if (priceBuilder.Length > 0)
                            book.Price = decimal.Parse(priceBuilder.ToString());
                    }
                }

                htmlNode = rootNode.SelectSingleNode("//h2[@itemprop='productID']");
                if (htmlNode != null)
                    book.Isbn = htmlNode.InnerText;
            }

            Status = SearchStatus.Complete;
        }

        HtmlNode Load(ref HtmlWeb parser, Uri uri)
        {
            try
            {
                return parser.Load(uri).DocumentNode;
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
