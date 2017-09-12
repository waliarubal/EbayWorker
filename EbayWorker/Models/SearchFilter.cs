using EbayWorker.Helpers.Base;
using System.Collections.Generic;

namespace EbayWorker.Models
{
    public class SearchFilter: NotificationBase
    {
        long _feedbackScore;
        decimal _feedbackPercent;
        bool _isFilterEnabled;
        HashSet<string> _allowedSellerNames, _restrictedSellerNames;

        #region properties

        public long FeedbackScore
        {
            get { return _feedbackScore; }
            set { Set("FeedbackScore", ref _feedbackScore, value); }
        }

        public decimal FeedbackPercent
        {
            get { return _feedbackPercent; }
            set { Set("FeedbackPercent", ref _feedbackPercent, value); }
        }

        public bool IsFilterEnabled
        {
            get { return _isFilterEnabled; }
            set { Set("IsFilterEnabled", ref _isFilterEnabled, value); }
        }

        public HashSet<string> AllowedSellers
        {
            get { return _allowedSellerNames; }
            set { Set("AllowedSellers", ref _allowedSellerNames, value); }
        }

        public HashSet<string> RestrictedSellers
        {
            get { return _restrictedSellerNames; }
            set { Set("RestrictedSellers", ref _restrictedSellerNames, value); }
        }

        #endregion
    }
}
