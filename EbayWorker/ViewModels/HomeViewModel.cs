using EbayWorker.Models;
using EbayWorker.Views;
using Microsoft.Win32;
using NullVoidCreations.WpfHelpers;
using NullVoidCreations.WpfHelpers.Base;
using NullVoidCreations.WpfHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace EbayWorker.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        const char SEPARATOR = 'μ';

        readonly string _settingsFile;
        string _inputFilePath, _outputDirectoryPath, _executionTime, _statusMessage;
        int _parallelQueries, _executedQueries;
        bool _failedQueriesOnly, _excludeEmptyResults, _groupByCondition, _groupByStupidLogic, _addPercentOfPice;
        SearchFilter _filter;
        List<SearchModel> _searchQueries;
        HashSet<string> _appIds;
        decimal _addToPrice;
        System.Threading.CancellationTokenSource _cancellationToken;
        Timer _timer;
        Stopwatch _stopWatch;
        static object _syncLock;
        AssemblyInformation _assemblyInfo;

        const string SETTINGS_FILE_NAME = "Settings.set.aes";
        const string SETTINGS_FILE_PASSWORD = "$admin@12345#";

        CommandBase _saveSettings, _loadSettings, _cancelSearch, _selectInputFile, _selectOutputDirectory, _search, _showSearchQuery, _selectAllowedSellers, _selectRestrictedSellers, _clearAllowedSellers, _clearRestrictedSellers, _manageAppIds;

        #region constructor/destructor

        static HomeViewModel()
        {
            _syncLock = new object();
        }

        public HomeViewModel()
        {
            _parallelQueries = 5;
            _executionTime = "00:00:00";
            _settingsFile = Path.Combine(App.Current.GetStartupDirectory(), SETTINGS_FILE_NAME);
        }

        #endregion

        #region properties

        public string ExecutionTime
        {
            get { return _executionTime; }
            private set { Set(nameof(ExecutionTime), ref _executionTime, value); }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
            private set { Set(nameof(StatusMessage), ref _statusMessage, value); }
        }

        public AssemblyInformation AssemblyInfo
        {
            get
            {
                if (_assemblyInfo == null)
                    _assemblyInfo = new AssemblyInformation(Assembly.GetExecutingAssembly());

                return _assemblyInfo;
            }
        }

        public decimal AddToPrice
        {
            get { return _addToPrice; }
            set
            {
                if (AddPercentOfPrice)
                {
                    if (value > 100m)
                        value = 100m;
                    else if (value < 0m)
                        value = 0m;
                }

                Set(nameof(AddToPrice), ref _addToPrice, value);
            }
        }

        public bool AddPercentOfPrice
        {
            get { return _addPercentOfPice; }
            set
            {
                Set(nameof(AddPercentOfPrice), ref _addPercentOfPice, value);

                if (AddPercentOfPrice)
                {
                    if (AddToPrice > 100m)
                        AddToPrice = 100m;
                    else if (AddToPrice < 0m)
                        AddToPrice = 0m;
                }
            }
        }

        public int ExecutedQueries
        {
            get { return _executedQueries; }
            private set { Set(nameof(ExecutedQueries), ref _executedQueries, value); }
        }

        public int MaxParallelQueries
        {
            get { return 18; }
        }

        public SearchFilter Filter
        {
            get
            {
                if (_filter == null)
                {
                    _filter = new SearchFilter();
                    _filter.LoadDefaults();
                }

                return _filter;
            }
        }

        public string InputFilePath
        {
            get { return _inputFilePath; }
            private set { Set(nameof(InputFilePath), ref _inputFilePath, value); }
        }

        public string OutputDirectoryPath
        {
            get { return _outputDirectoryPath; }
            private set { Set(nameof(OutputDirectoryPath), ref _outputDirectoryPath, value); }
        }

        public int ParallelQueries
        {
            get { return _parallelQueries; }
            set
            {
                if (value <= 0)
                    value = 1;
                else if (value > MaxParallelQueries)
                    value = MaxParallelQueries;

                Set(nameof(ParallelQueries), ref _parallelQueries, value);
            }
        }

        public List<SearchModel> SearchQueries
        {
            get { return _searchQueries; }
            private set { Set(nameof(SearchQueries), ref _searchQueries, value); }
        }

        public bool FailedQueriesOnly
        {
            get { return _failedQueriesOnly; }
            set { Set(nameof(FailedQueriesOnly), ref _failedQueriesOnly, value); }
        }

        public bool ExcludeEmptyResults
        {
            get { return _excludeEmptyResults; }
            set { Set(nameof(ExcludeEmptyResults), ref _excludeEmptyResults, value); }
        }

        public bool GroupByCondition
        {
            get { return _groupByCondition; }
            set { Set(nameof(GroupByCondition), ref _groupByCondition, value); }
        }

        public bool GroupByStupidLogic
        {
            get { return _groupByStupidLogic; }
            set { Set(nameof(GroupByStupidLogic), ref _groupByStupidLogic, value); }
        }

        #endregion

        #region commands

        public ICommand SelectInputFileCommand
        {
            get
            {
                if (_selectInputFile == null)
                    _selectInputFile = new RelayCommand(SelectInputFile);

                return _selectInputFile;
            }
        }

        public ICommand SaveSettingsCommand
        {
            get
            {
                if (_saveSettings == null)
                    _saveSettings = new RelayCommand(SaveSettings);

                return _saveSettings;
            }
        }

        public ICommand LoadSettingsCommand
        {
            get
            {
                if (_loadSettings == null)
                    _loadSettings = new RelayCommand(LoadSettings);

                return _loadSettings;
            }
        }

        public ICommand SelectOutputDirectoryCommand
        {
            get
            {
                if (_selectOutputDirectory == null)
                {
                    _selectOutputDirectory = new RelayCommand(() => OutputDirectoryPath = SelectDirectory());
                    _selectOutputDirectory.IsSynchronous = true;
                }

                return _selectOutputDirectory;
            }
        }

        public ICommand SearchCommand
        {
            get
            {
                if (_search == null)
                    _search = new RelayCommand(Search);

                return _search;
            }
        }

        public ICommand ShowSearchQueryCommand
        {
            get
            {
                if (_showSearchQuery == null)
                {
                    _showSearchQuery = new RelayCommand<SearchModel>(ShowSearchQuery);
                    _showSearchQuery.IsSynchronous = true;
                }

                return _showSearchQuery;
            }
        }

        public ICommand SelectAllowedSellersCommand
        {
            get
            {
                if (_selectAllowedSellers == null)
                    _selectAllowedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Filter.AllowedSellers = sellers);

                return _selectAllowedSellers;
            }
        }

        public ICommand SelectRestrictedSellersCommand
        {
            get
            {
                if (_selectRestrictedSellers == null)
                    _selectRestrictedSellers = new RelayCommand<object, HashSet<string>>(SelectSellers, (sellers) => Filter.RestrictedSellers = sellers);

                return _selectRestrictedSellers;
            }
        }

        public ICommand ClearAllowedSellersCommand
        {
            get
            {
                if (_clearAllowedSellers == null)
                    _clearAllowedSellers = new RelayCommand(() => Filter.AllowedSellers = null);

                return _clearAllowedSellers;
            }
        }

        public ICommand ClearRestrictedSellersCommand
        {
            get
            {
                if (_clearRestrictedSellers == null)
                    _clearRestrictedSellers = new RelayCommand(() => Filter.RestrictedSellers = null);

                return _clearRestrictedSellers;
            }
        }

        public ICommand CancelSearchCommand
        {
            get
            {
                if (_cancelSearch == null)
                    _cancelSearch = new RelayCommand(() => _cancellationToken.Cancel(false));

                return _cancelSearch;
            }
        }

        public ICommand ManageAppIdsCommand
        {
            get
            {
                if (_manageAppIds == null)
                    _manageAppIds = new RelayCommand(ManageAppIds) { IsSynchronous = true };

                return _manageAppIds;
            }
        }

        #endregion

        void ManageAppIds()
        {
            var viewModel = new AppIdViewModel(_appIds);
            var manageAppIds = new AppIdView();
            manageAppIds.DataContext = viewModel;
            manageAppIds.ShowDialog();

            _appIds.Clear();
            foreach (var appId in viewModel.AppIds)
                _appIds.Add(appId);
        }

        void ShowSearchQuery(SearchModel searchQuery)
        {
            var search = new SearchView(searchQuery);
            search.ShowDialog();
        }

        void SaveSettings()
        {
            var settings = new SettingsManager();
            settings.SetValue(nameof(AddToPrice), AddToPrice);
            settings.SetValue(nameof(AddPercentOfPrice), AddPercentOfPrice);
            settings.SetValue(nameof(ParallelQueries), ParallelQueries);
            settings.SetValue(nameof(FailedQueriesOnly), FailedQueriesOnly);
            settings.SetValue(nameof(ExcludeEmptyResults), ExcludeEmptyResults);
            settings.SetValue(nameof(GroupByCondition), GroupByCondition);
            settings.SetValue(nameof(GroupByStupidLogic), GroupByStupidLogic);
            settings.SetValue(nameof(Filter.Location), Filter.Location);
            settings.SetValue(nameof(Filter.CheckFeedbackScore), Filter.CheckFeedbackScore);
            settings.SetValue(nameof(Filter.FeedbackScore), Filter.FeedbackScore);
            settings.SetValue(nameof(Filter.CheckFeedbackPercent), Filter.CheckFeedbackPercent);
            settings.SetValue(nameof(Filter.FeedbackPercent), Filter.FeedbackPercent);
            settings.SetValue(nameof(Filter.IsPriceFiltered), Filter.IsPriceFiltered);
            settings.SetValue(nameof(Filter.MinimumPrice), Filter.MinimumPrice);
            settings.SetValue(nameof(Filter.MaximumPrice), Filter.MaximumPrice);
            settings.SetValue(nameof(Filter.CheckAllowedSellers), Filter.CheckAllowedSellers);
            settings.SetValue(nameof(Filter.AllowedSellers), EnumerableToString(Filter.AllowedSellers));
            settings.SetValue(nameof(Filter.CheckRestrictedSellers), Filter.CheckRestrictedSellers);
            settings.SetValue(nameof(Filter.RestrictedSellers), EnumerableToString(Filter.RestrictedSellers));
            settings.SetValue(nameof(Filter.IsAuction), Filter.IsAuction);
            settings.SetValue(nameof(Filter.IsBuyItNow), Filter.IsBuyItNow);
            settings.SetValue(nameof(Filter.IsClassifiedAds), Filter.IsClassifiedAds);
            settings.SetValue(nameof(_appIds), EnumerableToString(_appIds));
            settings.Save(_settingsFile, SETTINGS_FILE_PASSWORD);
        }

        void LoadSettings()
        {
            var settings = new SettingsManager();
            settings.Load(_settingsFile, SETTINGS_FILE_PASSWORD);
            AddToPrice = settings.GetValue<decimal>(nameof(AddToPrice));
            AddPercentOfPrice = settings.GetValue<bool>(nameof(AddPercentOfPrice));
            ParallelQueries = settings.GetValue(nameof(ParallelQueries), 5);
            FailedQueriesOnly = settings.GetValue<bool>(nameof(FailedQueriesOnly));
            ExcludeEmptyResults = settings.GetValue<bool>(nameof(ExcludeEmptyResults));
            GroupByCondition = settings.GetValue<bool>(nameof(GroupByCondition));
            GroupByStupidLogic = settings.GetValue<bool>(nameof(GroupByStupidLogic));
            Filter.Location = settings.GetValue(nameof(Filter.Location), "United States");
            Filter.CheckFeedbackScore = settings.GetValue<bool>(nameof(Filter.CheckFeedbackScore));
            Filter.FeedbackScore = settings.GetValue<long>(nameof(Filter.FeedbackScore));
            Filter.CheckFeedbackPercent = settings.GetValue<bool>(nameof(Filter.CheckFeedbackPercent));
            Filter.FeedbackPercent = settings.GetValue<decimal>(nameof(Filter.FeedbackPercent));
            Filter.IsPriceFiltered = settings.GetValue<bool>(nameof(Filter.IsPriceFiltered));
            Filter.MinimumPrice = settings.GetValue<decimal>(nameof(Filter.MinimumPrice));
            Filter.MaximumPrice = settings.GetValue<decimal>(nameof(Filter.MaximumPrice));
            Filter.CheckAllowedSellers = settings.GetValue<bool>(nameof(Filter.CheckAllowedSellers));
            Filter.AllowedSellers = StringToEnumerable(settings.GetValue<string>(nameof(Filter.AllowedSellers)));
            Filter.CheckRestrictedSellers = settings.GetValue<bool>(nameof(Filter.CheckRestrictedSellers));
            Filter.RestrictedSellers = StringToEnumerable(settings.GetValue<string>(nameof(Filter.RestrictedSellers)));
            Filter.IsAuction = settings.GetValue<bool>(nameof(Filter.IsAuction));
            Filter.IsBuyItNow = settings.GetValue(nameof(Filter.IsBuyItNow), true);
            Filter.IsClassifiedAds = settings.GetValue<bool>(nameof(Filter.IsClassifiedAds));
            _appIds = StringToEnumerable(settings.GetValue<string>(nameof(_appIds)));

            // TODO: remove this option to make app generic
            GroupByStupidLogic = true;
        }

        string EnumerableToString(IEnumerable<string> values)
        {
            var builder = new StringBuilder();
            foreach (var value in values)
                builder.AppendFormat("{0}{1}", value, SEPARATOR);
            return builder.ToString();
        }

        HashSet<string> StringToEnumerable(string values)
        {
            var data = new HashSet<string>();
            if (string.IsNullOrEmpty(values))
                return data;

            foreach (var value in values.Split(new[] { SEPARATOR }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (data.Contains(value))
                    continue;

                data.Add(value);
            }

            return data;
        }

        HashSet<string> SelectSellers(object parameter)
        {
            var fileName = SelectFile();
            if (string.IsNullOrEmpty(fileName))
                return null;

            var sellerNames = SplitData(fileName);
            var sellers = new HashSet<string>();
            foreach (var sellerName in sellerNames)
                if (!sellers.Contains(sellerName))
                    sellers.Add(sellerName);

            return sellers;
        }

        void StartTimer()
        {
            ExecutionTime = "00:00:00";
            ExecutedQueries = 0;

            if (_timer == null && _stopWatch == null)
            {
                _stopWatch = new Stopwatch();

                _timer = new Timer();
                _timer.Elapsed += (sender, e) =>
                {
                    var time = _stopWatch.Elapsed;
                    ExecutionTime = string.Format("{0:00}:{1:00}:{2:00}", time.Hours, time.Minutes, time.Seconds);
                };
                _timer.Interval = 500;
            }

            _stopWatch.Start();
            _timer.Start();
        }

        void StopTimer()
        {
            _stopWatch.Stop();
            _stopWatch.Reset();
            _timer.Stop();
        }

        void Search()
        {
            if (SearchQueries == null)
            {
                MessageBox.Show("Input file with search keywoards not selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (_appIds == null || _appIds.Count == 0)
            {
                MessageBox.Show("eBay App ID (Client ID) not added.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StartTimer();

            // output file names
            string fileName, notCompletedFileName;
            if (string.IsNullOrEmpty(OutputDirectoryPath))
            {
                fileName = null;
                notCompletedFileName = null;
            }
            else
            {
                var fileTime = DateTime.Now.ToFileTimeUtc();
                fileName = Path.Combine(OutputDirectoryPath, string.Format("{0}.csv", fileTime));
                notCompletedFileName = Path.Combine(OutputDirectoryPath, string.Format("{0}_not_completed.txt", fileTime));
            }

            _cancellationToken = new System.Threading.CancellationTokenSource();

            var parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = _cancellationToken.Token;
            parallelOptions.MaxDegreeOfParallelism = ParallelQueries;

            try
            {
                Parallel.ForEach(SearchQueries, parallelOptions, query =>
                {
                    if (parallelOptions.CancellationToken.IsCancellationRequested)
                        return;

                    var status = query.Status;
                    if (status == SearchStatus.Working)
                        return;

                    StatusMessage = string.Format("Gathering data for search keywoard {0}...", query.Keywoard);

                    if (FailedQueriesOnly)
                    {
                        if (status != SearchStatus.Complete)
                            query.Search(Filter, parallelOptions.CancellationToken);
                    }
                    else
                        query.Search(Filter, parallelOptions.CancellationToken);

                    WriteOutput(fileName, query);

                    ExecutedQueries += 1;
                });
            }
            catch(OperationCanceledException)
            {
                // cancelled by user, do nothing
            }

            // create file with search keywoards which failed to complete
            if (notCompletedFileName != null)
            {
                var notCompletedKeywoards = new StringBuilder();
                foreach (var query in SearchQueries)
                {
                    if (query.Status != SearchStatus.Complete)
                        notCompletedKeywoards.AppendLine(query.Keywoard);
                }
                if (notCompletedKeywoards.Length > 0)
                    File.WriteAllText(notCompletedFileName, notCompletedKeywoards.ToString());
            }

            if (ExcludeEmptyResults)
            {
                for (var index = SearchQueries.Count - 1; index >= 0; index--)
                {
                    var query = SearchQueries[index];
                    if (query.Status == SearchStatus.Complete && query.Books.Count == 0)
                        SearchQueries.RemoveAt(index);
                }
                RaisePropertyChanged(nameof(SearchQueries));
            }

            _cancellationToken.Dispose();
            StopTimer();
        }

        void WriteOutput(string fileName, SearchModel query)
        {
            if (fileName == null || query.Status != SearchStatus.Complete)
                return;

            string contents;
            if (GroupByCondition)
                contents = query.Books.ToCsvStringGroupedByCondition(AddToPrice, AddPercentOfPrice);
            else if (GroupByStupidLogic)
                contents = query.Books.ToCsvStringGroupedByConditionStupidLogic(AddToPrice, AddPercentOfPrice, query.Keywoard);
            else
                contents = query.Books.ToCsvString(AddToPrice, AddPercentOfPrice);

            lock (_syncLock)
            {
                File.AppendAllText(fileName, contents);
            }
        }

        void SelectInputFile()
        {
            var fileName = SelectFile();
            if (string.IsNullOrEmpty(fileName))
                return;

            InputFilePath = fileName;

            var keywoards = SplitData(fileName);
            var searchQueries = new List<SearchModel>();
            foreach (var keywoard in keywoards)
            {
                var keyWoard = keywoard.Trim('\r', '\n', ' ', '\t');
                if (string.IsNullOrWhiteSpace(keyWoard))
                    continue;

                var search = new SearchModel(_appIds);
                search.Keywoard = keyWoard;
                search.Category = Category.Books;
                searchQueries.Add(search);
            }
            SearchQueries = searchQueries;
            ExecutionTime = "00:00:00";
            ExecutedQueries = 0;
        }

        string SelectDirectory()
        {
            var directoryBrowser = new Forms.FolderBrowserDialog();
            directoryBrowser.ShowNewFolderButton = true;
            directoryBrowser.Description = "Select Directory";
            if (directoryBrowser.ShowDialog() == Forms.DialogResult.OK)
                return directoryBrowser.SelectedPath;

            return null;
        }

        string SelectFile()
        {
            var openFile = new OpenFileDialog();
            openFile.AddExtension = true;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.Filter = "Text Files|*.txt|CSV Files|*.csv";
            openFile.FilterIndex = 0;
            openFile.Multiselect = false;
            if (openFile.ShowDialog() == true)
                return openFile.FileName;

            return null;
        }

        string[] SplitData(string fileName)
        {
            var fileInfo = new FileInfo(fileName);

            string[] keywoards = null;
            switch (fileInfo.Extension)
            {
                case ".txt":
                    keywoards = File.ReadAllLines(fileInfo.FullName);
                    break;

                case ".csv":
                    keywoards = File.ReadAllText(fileInfo.FullName).Split(',');
                    break;
            }
            return keywoards;
        }
    }
}
