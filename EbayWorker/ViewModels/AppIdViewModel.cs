using NullVoidCreations.WpfHelpers.Base;
using NullVoidCreations.WpfHelpers.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace EbayWorker.ViewModels
{
    class AppIdViewModel: ViewModelBase
    {
        ObservableCollection<string> _appIds;
        string _appId;

        ICommand _addAppId, _deleteAppId;

        #region constructor/destructor

        public AppIdViewModel()
        {
            _appIds = new ObservableCollection<string>();
        }

        public AppIdViewModel(IEnumerable<string> appIds): this()
        {
            foreach (var appId in appIds)
                AppIds.Add(appId);
        }

        #endregion

        #region properties

        public ObservableCollection<string> AppIds
        {
            get { return _appIds; }
        }

        public string AppId
        {
            get { return _appId; }
            set { Set(nameof(AppId), ref _appId, value); }
        }

        #endregion

        #region commands

        public ICommand AddAppIdCommand
        {
            get
            {
                if (_addAppId == null)
                    _addAppId = new RelayCommand(AddAppId) { IsSynchronous = true };

                return _addAppId;
            }
        }

        public ICommand DeleteAppIdCommand
        {
            get
            {
                if (_deleteAppId == null)
                    _deleteAppId = new RelayCommand<string>(DeleteAppId) { IsSynchronous = true };

                return _deleteAppId;
            }
        }

        #endregion

        void AddAppId()
        {
            if (string.IsNullOrWhiteSpace(AppId))
            {
                MessageBox.Show("App ID not entered.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (AppIds.Contains(AppId))
            {
                MessageBox.Show("App ID already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            AppIds.Add(AppId);
            AppId = null;
        }

        void DeleteAppId(string appId)
        {
            AppIds.Remove(appId);
        }
    }
}
