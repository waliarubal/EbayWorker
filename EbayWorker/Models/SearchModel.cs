using NullVoidCreations.WpfHelpers.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace EbayWorker.Models
{
    public enum Category : int
    {
        Books = 267
    }

    public enum SearchStatus : byte
    {
        NotStarted,
        Working,
        Complete,
        Failed,
        Cancelled
    }

    public class SearchModel : NotificationBase
    {
        const bool USE_PRODUCTION = true;

        string _keywoard;
        Category _category;
        SearchStatus _status;
        readonly Type _conditionType;
        BookCollection _books;
        readonly IEnumerable<string> _appIds;

        public SearchModel(IEnumerable<string> appIds)
        {
            _appIds = appIds;
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

        internal void Search(SearchFilter filter, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Status = SearchStatus.Cancelled;
                return;
            }

            foreach(var appId in _appIds)
            {
                if (Status == SearchStatus.Working || Status == SearchStatus.Complete)
                    break;

                Status = SearchStatus.Working;
                _books.Clear();

                var findRequest = new EbayFindRequest(appId, Keywoard, filter);
                findRequest.GetResponse(USE_PRODUCTION, ref _books);
                Status = findRequest.Status;
            }
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

    }
}
