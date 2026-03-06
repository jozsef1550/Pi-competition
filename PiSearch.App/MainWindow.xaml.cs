using System.Windows;
using System.Windows.Media.Animation;
using PiSearch.App.ViewModels;

namespace PiSearch.App;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// Creates the MainViewModel and manages the animated Sampling Drawer.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _drawerOpen;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel(Dispatcher);
        DataContext = _vm;

        // Wire drawer toggle: open/close storyboard based on IsDrawerOpen changes
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsDrawerOpen))
                AnimateDrawer(_vm.IsDrawerOpen);
        };
    }

    private void AnimateDrawer(bool open)
    {
        if (open == _drawerOpen) return;
        _drawerOpen = open;

        var sb = (Storyboard)Resources[open ? "DrawerOpenSB" : "DrawerCloseSB"];
        sb.Begin(this);
    }
}