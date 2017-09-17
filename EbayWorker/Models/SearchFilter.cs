using EbayWorker.Helpers.Base;
using System.Collections.Generic;

namespace EbayWorker.Models
{
    public class SearchFilter: NotificationBase
    {
        long _feedbackScore;
        decimal _feedbackPercent;
        bool _checkFeedbackScore, _checkFeedbakcPercent, _checkAllowedSellers, _checkRestrictedSellers;
        HashSet<string> _allowedSellerNames, _restrictedSellerNames;

        #region properties

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
            set { Set("FeedbackPercent", ref _feedbackPercent, value); }
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

        #endregion
    }
}
