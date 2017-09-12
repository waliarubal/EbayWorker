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
        string _code, _isbn, _title;
        decimal _price;
        BookCondition _condition;
        SellerModel _seller;

        #region properties

        public SellerModel Seller
        {
            get { return _seller; }
            set { Set("Seller", ref _seller, value); }
        }

        public Uri Url
        {
            get { return _url; }
            set { Set("Url", ref _url, value); }
        }

        public string Code
        {
            get { return _code; }
            set { Set("Code", ref _code, value); }
        }

        public string Isbn
        {
            get { return _isbn; }
            set { Set("Isbn", ref _isbn, value); }
        }

        public string Title
        {
            get { return _title; }
            set { Set("Title", ref _title, value); }
        }

        public decimal Price
        {
            get { return _price; }
            set { Set("Price", ref _price, value); }
        }

        public BookCondition Condition
        {
            get { return _condition; }
            set { Set("Condition", ref _condition, value); }
        }

        #endregion

    }
}
