using EbayWorker.Models;
using NullVoidCreations.WpfHelpers.Base;
using NullVoidCreations.WpfHelpers.Commands;
using System;
using System.Diagnostics;
using System.Windows.Input;

namespace EbayWorker.ViewModels
{
    public class SearchViewModel: ViewModelBase
    {
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
            Process.Start(url.AbsoluteUri);
        }
    }
}
