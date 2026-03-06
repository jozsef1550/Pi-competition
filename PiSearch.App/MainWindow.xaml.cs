using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media.Animation;
using PiSearch.App.ViewModels;

namespace PiSearch.App;

/// <summary>
/// Interaction logic for MainWindow.xaml.
/// Creates the MainViewModel and manages the animated Sampling Drawer.
/// </summary>
// CA1001: _vm is disposed in the Closed event handler; WPF Window cannot implement IDisposable.
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "Disposal of _vm is handled in the Closed event; WPF Window cannot implement IDisposable.")]
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

        // Dispose the ViewModel (and any running search) when the window closes
        Closed += (_, _) => _vm.Dispose();
    }

    private void AnimateDrawer(bool open)
    {
        if (open == _drawerOpen) return;
        _drawerOpen = open;

        var sb = (Storyboard)Resources[open ? "DrawerOpenSB" : "DrawerCloseSB"];
        sb.Begin(this);
    }
}