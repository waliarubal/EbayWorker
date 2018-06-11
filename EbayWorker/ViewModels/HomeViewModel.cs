using EbayWorker.Models;
using EbayWorker.Views;
using HtmlAgilityPack;
using Microsoft.Win32;
using NullVoidCreations.WpfHelpers;
using NullVoidCreations.WpfHelpers.Base;
using NullVoidCreations.WpfHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Forms = System.Windows.Forms;

namespace EbayWorker.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        readonly string _settingsFile;
        string _inputFilePath, _outputDirectoryPath, _executionTime;
        int _parallelQueries, _executedQueries;
        bool _failedQueriesOnly, _scrapBooksInParallel, _excludeEmptyResults, _groupByCondition, _groupByStupidLogic, _addPercentOfPice, _autoRetry;
        SearchFilter _filter;
        List<SearchModel> _searchQueries;
        decimal _addToPrice;
        System.Threading.CancellationTokenSource _cancellationToken;
        Timer _timer;
        Stopwatch _stopWatch;
        static object _syncLock;

        const string SETTINGS_FILE_NAME = "Settings.config";
        const string SETTINGS_FILE_PASSWORD = "$admin@12345#";

        CommandBase _cancelSearch, _selectInputFile, _selectOutputDirectory, _search, _showSearchQuery, _selectAllowedSellers, _selectRestrictedSellers, _clearAllowedSellers, _clearRestrictedSellers;

        static HomeViewModel()
        {
            _syncLock = new object();
        }

        public HomeViewModel()
        {
            _parallelQueries = 5;
            _executionTime = "00:00:00";
            _settingsFile = Path.Combine(App.Current.GetStartupDirectory(), SETTINGS_FILE_NAME);

            // TODO: remove this option to make app generic
            _groupByStupidLogic = true;
        }

        #region properties

        public string ExecutionTime
        {
            get { return _executionTime; }
            private set { Set(nameof(ExecutionTime), ref _executionTime, value); }
        }

        public decimal AddToPrice
        {
            get { return _addToPrice; }
            set { Set(nameof(AddToPrice), ref _addToPrice, value); }
        }

        public bool AddPercentOfPrice
        {
            get { return _addPercentOfPice; }
            set { Set(nameof(AddPercentOfPrice), ref _addPercentOfPice, value); }
        }

        public int ExecutedQueries
        {
            get { return _executedQueries; }
            private set { Set(nameof(ExecutedQueries), ref _executedQueries, value); }
        }

        public int MaxParallelQueries
        {
            get { return 20; }
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
            set { Set(nameof(ParallelQueries), ref _parallelQueries, value); }
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

        public bool AutoRetry
        {
            get { return _autoRetry; }
            set { Set(nameof(AutoRetry), ref _autoRetry, value); }
        }

        public bool ScrapBooksInParallel
        {
            get { return _scrapBooksInParallel; }
            set { Set(nameof(ScrapBooksInParallel), ref _scrapBooksInParallel, value); }
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

        #endregion

        void SaveSettings()
        {
            var settings = new SettingsManager();
            settings.SetValue(nameof(AddToPrice), AddToPrice);
            settings.SetValue(nameof(AddPercentOfPrice), AddPercentOfPrice);
            settings.SetValue(nameof(ParallelQueries), ParallelQueries);
            settings.SetValue(nameof(FailedQueriesOnly), FailedQueriesOnly);
            settings.SetValue(nameof(AutoRetry), AutoRetry);
            settings.SetValue(nameof(ScrapBooksInParallel), ScrapBooksInParallel);
            settings.SetValue(nameof(ExcludeEmptyResults), ExcludeEmptyResults);
            settings.SetValue(nameof(GroupByCondition), GroupByCondition);
            settings.SetValue(nameof(GroupByStupidLogic), GroupByStupidLogic);
            settings.SetValue(nameof(Filter.Location), Filter.Location);
            settings.SetValue(nameof(Filter.CheckFeedbackScore), Filter.CheckFeedbackScore);
            settings.SetValue(nameof(Filter.FeedbackScore), Filter.FeedbackScore);
            settings.SetValue(nameof(Filter.CheckFeedbackPercent), Filter.FeedbackPercent);
            settings.SetValue(nameof(Filter.FeedbackPercent), Filter.FeedbackPercent);
            settings.SetValue(nameof(Filter.IsPriceFiltered), Filter.IsPriceFiltered);
            settings.SetValue(nameof(Filter.MinimumPrice), Filter.MinimumPrice);
            settings.SetValue(nameof(Filter.MaximumPrice), Filter.MaximumPrice);
            settings.SetValue(nameof(Filter.CheckAllowedSellers), Filter.CheckAllowedSellers);
            settings.SetValue(nameof(Filter.AllowedSellers), Filter.AllowedSellers);
            settings.SetValue(nameof(Filter.CheckRestrictedSellers), Filter.CheckRestrictedSellers);
            settings.SetValue(nameof(Filter.RestrictedSellers), Filter.RestrictedSellers);
            settings.SetValue(nameof(Filter.IsAuction), Filter.IsAuction);
            settings.SetValue(nameof(Filter.IsBuyItNow), Filter.IsBuyItNow);
            settings.SetValue(nameof(Filter.IsClassifiedAds), Filter.IsClassifiedAds);
            settings.Save(_settingsFile, SETTINGS_FILE_PASSWORD);
        }

        void LoadSettings()
        {
            var settings = new SettingsManager();
            settings.Load(_settingsFile, SETTINGS_FILE_PASSWORD);
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

        void ShowSearchQuery(SearchModel searchQuery)
        {
            var search = new SearchView(searchQuery);
            search.ShowDialog();
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
                return;

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

                    var parser = new HtmlDocument();
                    if (FailedQueriesOnly)
                    {
                        if (status != SearchStatus.Complete)
                            query.Search(ref parser, Filter, ParallelQueries, ScrapBooksInParallel, AutoRetry, parallelOptions.CancellationToken);
                    }
                    else
                        query.Search(ref parser, Filter, ParallelQueries, ScrapBooksInParallel, AutoRetry, parallelOptions.CancellationToken);

                    WriteOutput(fileName, query);

                    ExecutedQueries += 1;
                });
            }
            catch(OperationCanceledException)
            {
                // do nothing, cancelled by user
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

                var search = new SearchModel();
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
