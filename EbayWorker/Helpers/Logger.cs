using SharpRaven;
using SharpRaven.Data;
using System;

namespace EbayWorker.Helpers
{
    sealed class Logger
    {
        const string DNS = "https://44fcbfe77619495c9d86f4654cb8a173:65224e2c09254683ac56661940a1208d@sentry.io/234336";
        const string ENVIRONMENT = "Producton";
        const string VERSION = "17.10.24.0";

        static object _syncRoot;
        static Logger _instance;

        readonly RavenClient _ravenClient;

        #region constructor/destructor

        static Logger()
        {
            _syncRoot = new object();
        }

        private Logger()
        {
            _ravenClient = new RavenClient(DNS);
            _ravenClient.Compression = true;
            _ravenClient.Environment = ENVIRONMENT;
            _ravenClient.Release = VERSION;
        }

        #endregion

        #region properties

        public static Logger Instance
        {
            get
            {
                lock(_syncRoot)
                {
                    if (_instance == null)
                        _instance = new Logger();

                    return _instance;
                }
            }
        }

        #endregion

        public void LogException(Exception ex)
        {
            _ravenClient.Capture(new SentryEvent(ex));
        }

        public void LogMessage(string messageFormat, params object[] messageParts)
        {
            _ravenClient.Capture(new SentryEvent(new SentryMessage(messageFormat, messageParts)));
        }
    }
}
