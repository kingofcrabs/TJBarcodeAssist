using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace BarcodeInputAssist
{
    /// <summary>
    /// Interaction logic for QueryType.xaml
    /// </summary>
    public partial class QueryType : Window
    {
        public QueryType()
        {
            InitializeComponent();
            lvFormats.ItemsSource = Utility.ReadConfig();
        }


        public FormattedHeader SelectedFormat { get; set; }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if(lvFormats.SelectedItem == null)
            {
                SetHint("请选择一种文件类型！");
                return;
            }
            SelectedFormat = (FormattedHeader)lvFormats.SelectedItem;
            this.Close();

        }
        private void SetHint(string s, bool bRed = true)
        {
            txtHint.Content = s;
            var color = bRed ? Colors.Red : Colors.Blue;
            txtHint.Foreground = new SolidColorBrush(color);
        }
    }
}
