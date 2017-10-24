using GoogleAnalytics.Core;
using System;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace EbayWorker.Helpers
{
    /// <summary>
    /// This class provides platform information required in Google Analytics.
    /// </summary>
    class PlatformInfoProvider : IPlatformInfoProvider
    {
        const string USER_AGENT = "Mozilla/5.0 (compatible, MSIE 11, Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

        string _clientId;
        readonly int? _bitsPerPixel;
        readonly Dimensions _resolution, _viewPortResolution;
        readonly string _language;

        public event EventHandler ScreenResolutionChanged;
        public event EventHandler ViewPortResolutionChanged;

        #region constructor/destructor

        public PlatformInfoProvider()
        {
            var screen = Screen.PrimaryScreen;
            var culture = CultureInfo.InstalledUICulture;

            _clientId = Guid.NewGuid().ToString("N");
            _bitsPerPixel = screen.BitsPerPixel;
            _resolution =  new Dimensions(screen.WorkingArea.Width, screen.WorkingArea.Height);
            _viewPortResolution = new Dimensions(screen.Bounds.Width, screen.Bounds.Height);
            _language = culture.TwoLetterISOLanguageName;
        }

        #endregion

        #region properties

        public string AnonymousClientId
        {
            get { return _clientId; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Clinet ID can't be empty.");

                _clientId = value;
            }
        }

        public int? ScreenColorDepthBits
        {
            get { return _bitsPerPixel; }
        }

        public Dimensions ScreenResolution
        {
            get { return _resolution; }
        }

        public string UserLanguage
        {
            get { return _language; }
        }

        public Dimensions ViewPortResolution
        {
            get { return _viewPortResolution; }
        }

        #endregion

        public string GetUserAgent()
        {
            return USER_AGENT;
        }

        public void OnTracking()
        {
            
        }

        void RaiseResolutionChanged()
        {
            var srcHandler = ScreenResolutionChanged;
            if (srcHandler != null)
                srcHandler.Invoke(this, EventArgs.Empty);

            var vprcHandler = ViewPortResolutionChanged;
            if (vprcHandler != null)
                vprcHandler.Invoke(this, EventArgs.Empty);
        }
    }

    sealed class Analytics
    {
        const string PROPERTY_ID = "UA-108545684-1";
        const string APP_ID = "EbayWorker";
        const string APP_NAME = "eBay Worker";
        const string APP_VERSION = "17.10.24.22";

        static object _syncLock;
        static Analytics _instance;
        Tracker _tracker;
        readonly TrackerManager _manager;

        #region constructor/destructor

        static Analytics()
        {
            _syncLock = new object();
        }

        private Analytics()
        {
            var platformInfo = new PlatformInfoProvider();

            _manager = new TrackerManager(platformInfo);
            _manager.IsDebugEnabled = false;
        }

        #endregion

        #region properties

        public static Analytics Instance
        {
            get
            {
                lock(_syncLock)
                {
                    if (_instance == null)
                        _instance = new Analytics();

                    return _instance;
                }
            }
        }

        #endregion

        public void StartSession()
        {
            if (_tracker != null)
                EndSession();

            _tracker = _manager.GetTracker(PROPERTY_ID);
            _tracker.AppId = APP_ID;
            _tracker.AppName = APP_NAME;
            _tracker.AppVersion = APP_VERSION;
            _tracker.SetStartSession(true);
        }

        public void EndSession()
        {
            if (_tracker == null)
                return;

            _tracker.SetEndSession(false);
            _manager.CloseTracker(_tracker);
        }

        public void TrackException(Exception ex)
        {
            var exBuilder = new StringBuilder();
            exBuilder.AppendFormat("Message: {0}{1}", ex.Message, Environment.NewLine);
            exBuilder.AppendFormat("Stack Trace: {0}", ex.StackTrace);

            _tracker.SendException(ex.ToString(), false);
        }

        public void TrackScreenView(string screenName)
        {
            _tracker.SendView(screenName);
        }

        public void TrackTiming(TimeSpan time, string category, string variable, string label)
        {
            _tracker.SendTiming(time, category, variable, label);
        }
    }
}
