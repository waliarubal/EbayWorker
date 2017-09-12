using EbayWorker.Helpers.Base;

namespace EbayWorker.Models
{
    public class SellerModel: NotificationBase
    {
        string _name;
        long _feedbackScore;
        decimal _feedbackPercent;

        #region properties

        public string Name
        {
            get { return _name; }
            set { Set("Name", ref _name, value); }
        }

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

        #endregion
    }
}
