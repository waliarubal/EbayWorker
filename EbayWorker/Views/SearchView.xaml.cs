using EbayWorker.Models;
using EbayWorker.ViewModels;
using System.Windows;

namespace EbayWorker.Views
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    public partial class SearchView : Window
    {
        public SearchView()
        {
            InitializeComponent();
        }

        public SearchView(SearchModel searchQuery): this()
        {
            var viewModel = DataContext as SearchViewModel;
            if (viewModel != null)
                viewModel.SearchQuery = searchQuery;
        }
    }
}
