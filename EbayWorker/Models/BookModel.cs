using EbayWorker.Helpers.Base;
using System;

namespace EbayWorker.Models
{
    public enum BookCondition: byte
    {
        Unknown,
        BrandNew,
        LikeNew,
        VeryGood,
        Good,
        Acceptable
    }

    public class BookModel: NotificationBase
    {
        Uri _url;
        string _code, _isbn, _title, _location;
        decimal _price;
        BookCondition _condition;
        SearchStatus _status;
        SellerModel _seller;

        #region properties

        public SearchStatus Status
        {
            get { return _status; }
            set { Set(nameof(Status), ref _status, value); }
        }

        public SellerModel Seller
        {
            get { return _seller; }
            set { Set(nameof(Seller), ref _seller, value); }
        }

        public Uri Url
        {
            get { return _url; }
            set { Set(nameof(Url), ref _url, value); }
        }

        public string Code
        {
            get { return _code; }
            set { Set(nameof(Code), ref _code, value); }
        }

        public string Isbn
        {
            get { return _isbn; }
            set { Set(nameof(Isbn), ref _isbn, value); }
        }

        public string Title
        {
            get { return _title; }
            set { Set(nameof(Title), ref _title, value); }
        }

        public string Location
        {
            get { return _location; }
            set { Set(nameof(Location), ref _location, value); }
        }

        public decimal Price
        {
            get { return _price; }
            set { Set(nameof(Price), ref _price, value); }
        }

        public BookCondition Condition
        {
            get { return _condition; }
            set { Set(nameof(Condition), ref _condition, value); }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0} ({1})", Title, Code);
        }

    }
}
