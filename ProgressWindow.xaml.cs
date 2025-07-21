using System.Windows;
using System.Windows.Controls;

namespace AutoCAD.IPPlugin.Net8
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>

    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
            this.Title = "Loading DWG...";
        }
    }
}
