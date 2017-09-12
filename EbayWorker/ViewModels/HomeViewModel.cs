using EbayWorker.Helpers;
using EbayWorker.Helpers.Base;
using EbayWorker.Models;
using EbayWorker.Views;
using HtmlAgilityPack;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace EbayWorker.ViewModels
{
    public class HomeViewModel: ViewModelBase
    {
        string _inputFilePath, _outputDirectoryPath;
        List<SearchModel> _searchQueries;
        CommandBase _selectInputFile, _search, _showSearchQuery;

        #region properties

        public string InputFilePath
        {
            get { return _inputFilePath; }
            private set { Set("InputFilePath", ref _inputFilePath, value); }
        }

        public string OutputDirectoryPath
        {
            get { return _outputDirectoryPath; }
            private set { Set("OutputDirectoryPath", ref _outputDirectoryPath, value); }
        }

        public List<SearchModel> SearchQueries
        {
            get { return _searchQueries; }
            private set { Set("SearchQueries", ref _searchQueries, value); }
        }

        #endregion

        #region commands

        public CommandBase SelectInputFileCommand
        {
            get
            {
                if (_selectInputFile == null)
                    _selectInputFile = new RelayCommand(SelectInputFile);

                return _selectInputFile;
            }
        }

        public CommandBase SearchCommand
        {
            get
            {
                if (_search == null)
                    _search = new RelayCommand(Search);

                return _search;
            }
        }

        public CommandBase ShowSearchQueryCommand
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

        #endregion

        void ShowSearchQuery(SearchModel searchQuery)
        {
            var search = new SearchView(searchQuery);
            search.ShowDialog();
        }

        void Search()
        {
            if (SearchQueries == null)
                return;

            var parser = new HtmlWeb();
            foreach(var query in SearchQueries)
            {
                query.Search(ref parser);
            }
        }


        void SelectInputFile()
        {
            var openFile = new OpenFileDialog();
            openFile.AddExtension = true;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;
            openFile.Filter = "Text Files|*.txt";
            openFile.FilterIndex = 0;
            openFile.Multiselect = false;
            if (openFile.ShowDialog() != true)
                return;

            InputFilePath = openFile.FileName;

            var isbns = File.ReadAllLines(InputFilePath);
            var searchQueries = new List<SearchModel>();
            foreach(var isbn in isbns)
            {
                var search = new SearchModel();
                search.Keywoard = isbn;
                search.Category = Category.Books;
                searchQueries.Add(search);
            }
            SearchQueries = searchQueries;
        }
    }
}
