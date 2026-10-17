using System.Windows;
using WinTuner.Gui.ViewModels;

namespace WinTuner.Gui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void OnPreview(object sender, RoutedEventArgs e) => _viewModel.Preview();

    private void OnApply(object sender, RoutedEventArgs e) => _viewModel.Apply();

    private void OnRollback(object sender, RoutedEventArgs e) => _viewModel.Rollback();
}
