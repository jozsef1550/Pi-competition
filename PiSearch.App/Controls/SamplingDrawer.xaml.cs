using System.Windows.Controls;

namespace PiSearch.App.Controls;

/// <summary>
/// Animated side-panel showing the character-to-number map for the current
/// sampling key. DataContext should be bound to
/// <see cref="ViewModels.SearchKeyViewModel.CharacterMap"/>.
/// </summary>
public partial class SamplingDrawer : UserControl
{
    public SamplingDrawer()
    {
        InitializeComponent();
    }
}
