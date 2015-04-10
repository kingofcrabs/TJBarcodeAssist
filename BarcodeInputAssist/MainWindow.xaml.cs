using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BarcodeInputAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<PlateInfo> plates = new ObservableCollection<PlateInfo>();
        public MainWindow()
        {
            InitializeComponent();
            lstboxPlates.ItemsSource = plates;
            this.KeyDown += MainWindow_KeyDown;
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            switch(e.Key)
            {
                case Key.N:
                    OnNewPlate();
                    break;
                case Key.M:
                    OnMergePlates();
                    break;
                case Key.L:
                    OnLoadPlate();
                    break;
                default:
                    break;
            }
      
        }

        private void OnLoadPlate()
        {
            throw new NotImplementedException();
        }

        private void OnMergePlates()
        {
            throw new NotImplementedException();
        }

        private void OnNewPlate()
        {
            Utility.InitDataGridView(dataGridView, 48);
        }

        private void lstboxPlates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }



    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
