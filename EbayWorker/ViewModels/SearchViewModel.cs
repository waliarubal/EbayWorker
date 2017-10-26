using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using EbayWorker.Models;
using System;
using System.Diagnostics;

namespace EbayWorker.ViewModels
{
    public class SearchViewModel: ViewModelBase
    {
        SearchModel _search;
        CommandBase _openUrl;

        #region properties

        public SearchModel SearchQuery
        {
            get { return _search; }
            internal set { Set(nameof(SearchQuery), ref _search, value); }
        }

        #endregion

        #region commands

        public CommandBase OpenUrlCommand
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
            Process.Start(url.AbsoluteUri);
        }
    }
}
