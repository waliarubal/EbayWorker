using EbayWorker.Helpers.Base;
using System.Collections.Generic;

namespace EbayWorker.Models
{
    public class SearchFilter: NotificationBase
    {
        long _feedbackScore;
        decimal _feedbackPercent, _priceMaximum;
        bool _checkFeedbackScore, _checkFeedbakcPercent, _checkAllowedSellers, _checkRestrictedSellers, _isAuction, _isBuyItNow, _isClassifiedAds;
        HashSet<string> _allowedSellerNames, _restrictedSellerNames;
        string _location;
        readonly Dictionary<string, string> _locations;


        public SearchFilter()
        {
            _locations = new Dictionary<string, string>
            {
                { "US Only", "-1&saslc=1" },
                { "Worldwide", "-1&saslc=2" },
                { "North America", "-1&saslc=3" },
                { "South America", "-1&saslc=4" },
                { "Europe", "-1&saslc=5" },
                { "Asia", "-1&saslc=6" }
            };
        }

        #region properties

        public IEnumerable<string> Locations
        {
            get { return _locations.Keys; }
        }

        public string Location
        {
            get { return _location; }
            set { Set("Location", ref _location, value); }
        }

        public bool CheckFeedbackScore
        {
            get { return _checkFeedbackScore; }
            set { Set("CheckFeedbackScore", ref _checkFeedbackScore, value); }
        }

        public long FeedbackScore
        {
            get { return _feedbackScore; }
            set { Set("FeedbackScore", ref _feedbackScore, value); }
        }

        public bool CheckFeedbackPercent
        {
            get { return _checkFeedbakcPercent; }
            set { Set("CheckFeedbackPercent", ref _checkFeedbakcPercent, value); }
        }

        public decimal FeedbackPercent
        {
            get { return _feedbackPercent; }
            set
            {
                if (value < 0m)
                    value = 0m;
                else if (value > 99.99m)
                    value = 99.99m;

                Set("FeedbackPercent", ref _feedbackPercent, value);
            }
        }

        public decimal MaximumPrice
        {
            get { return _priceMaximum; }
            set { Set("MaximumPrice", ref _priceMaximum, value); }
        }

        public bool CheckAllowedSellers
        {
            get { return _checkAllowedSellers; }
            set { Set("CheckAllowedSellers", ref _checkAllowedSellers, value); }
        }

        public HashSet<string> AllowedSellers
        {
            get { return _allowedSellerNames; }
            set { Set("AllowedSellers", ref _allowedSellerNames, value); }
        }

        public bool CheckRestrictedSellers
        {
            get { return _checkRestrictedSellers; }
            set { Set("CheckRestrictedSellers", ref _checkRestrictedSellers, value); }
        }

        public HashSet<string> RestrictedSellers
        {
            get { return _restrictedSellerNames; }
            set { Set("RestrictedSellers", ref _restrictedSellerNames, value); }
        }

        public bool IsAuction
        {
            get { return _isAuction; }
            set
            {
                Set("IsAuction", ref _isAuction, value);
                if (value)
                    IsClassifiedAds = false;
            }
        }

        public bool IsBuyItNow
        {
            get { return _isBuyItNow; }
            set
            {
                Set("IsBuyItNow", ref _isBuyItNow, value);
                if (value)
                    IsClassifiedAds = false;
            }
        }

        public bool IsClassifiedAds
        {
            get { return _isClassifiedAds; }
            set
            {
                Set("IsClassifiedAds", ref _isClassifiedAds, value);
                if (value)
                {
                    IsBuyItNow = false;
                    IsAuction = false;
                }
            }
        }

        #endregion

        internal string GetLocation()
        {
            if (string.IsNullOrEmpty(Location))
                return null;

            return _locations[Location];
        }
    }
}
