using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarcodeInputAssist
{
    /// <summary>
    /// QueryNumber.xaml 的交互逻辑
    /// </summary>
    public partial class QueryName : Window
    {
        List<string> names = null;
        public QueryName()
        {
            InitializeComponent();
        }

     
        public QueryName(List<string> existNames)
            :this()
        {
            names = existNames;
            txtPlateName.Focus();
            lstAssays.SelectedIndex = 0;
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string sNewPlateName = txtPlateName.Text.Trim();
            if(sNewPlateName == "")
            {
                SetHint("板名不得为空");
                return;
            }
            if(names.Contains(sNewPlateName))
            {
                SetHint("板名已经存在！");
                return;
            }

            if(lstAssays.SelectedIndex == -1)
            {
                SetHint("请选择一个Assay！");
                return;
            }

            PlateName = sNewPlateName;
            AssayName = (string)((ListBoxItem)lstAssays.SelectedItem).Content;
            this.DialogResult = true;
            this.Close();
        }

        private void SetHint(string s)
        {
            txtHint.Content = s;
            txtHint.Foreground = new SolidColorBrush(Colors.Red);
        }

        public string AssayName { get; set; }
        public string PlateName { get; set; }
    }
}
