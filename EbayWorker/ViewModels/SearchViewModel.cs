using EbayWorker.Models;
using NullVoidCreations.WpfHelpers.Base;
using NullVoidCreations.WpfHelpers.Commands;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Input;

namespace EbayWorker.ViewModels
{
    public class SearchViewModel: ViewModelBase
    {
        const string LINK_SHORTNER_ENDPOINT = "https://api.shorte.st/v1/data/url";
        const string LINK_SHORTNER_TOKEN = "283b6d17cce97d4719d9edd4f9c15035";

        SearchModel _search;
        ICommand _openUrl;

        #region properties

        public SearchModel SearchQuery
        {
            get { return _search; }
            internal set { Set(nameof(SearchQuery), ref _search, value); }
        }

        #endregion

        #region commands

        public ICommand OpenUrlCommand
        {
            get
            {
                if (_openUrl == null)
                    _openUrl = new RelayCommand<Uri>(OpenUrl);

                return _openUrl;
            }
        }

        #endregion

        void OpenUrl(Uri url)
        {
            // shorten URL
            var urlShortner = new UrlShortner(url);
            if(urlShortner.Shorten() && urlShortner.IsShort)
                url = urlShortner.ShortUrl;

            Process.Start(url.AbsoluteUri);
        }
    }
}
