using S7Simulator.Wpf.Models;
using S7Simulator.Wpf.ViewModels;
using S7Simulator.Wpf.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace S7Simulator.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView lv && lv.SelectedItem is DbInfo db)
        {
            if (DataContext is MainViewModel vm)
            {
                // Open Detail Window
                // Ideally this should be a service, but for now we keep it here
                new DBDetailWindow(db, vm.Memory) { Owner = this }.ShowDialog();
                
                // Refresh list if needed (though ObservableCollection should handle it if items changed, 
                // but if properties changed inside items, we might need to trigger update. 
                // For now, let's assume DB structure doesn't change in DetailWindow, only values)
            }
        }
    }
}
