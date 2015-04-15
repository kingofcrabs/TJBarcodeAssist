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
using System.Windows.Shapes;

namespace BarcodeInputAssist
{
    /// <summary>
    /// MergeForm.xaml 的交互逻辑
    /// </summary>
    public partial class MergeForm : Window
    {
        ObservableCollection<PlateInfo> plateInfos;
        PlateInfo firstPlate;
        PlateInfo secondPlate;
        public MergeForm()
        {
            InitializeComponent();
        }

        public MergeForm(ObservableCollection<PlateInfo> plates)
            :this()
        {
            this.plateInfos = plates;
            lstSrcPlates.Items.Clear();
            lstSrcPlates.ItemsSource = plateInfos;
        }

        public PlateInfo MergedPlate { get; set; }
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if(firstPlate == null || secondPlate == null)
            {
                SetHint("请选中两块板子做Merge！");
                return;
            }

            if(firstPlate.SampleDescription != secondPlate.SampleDescription)
            {
                SetHint("两块板的Assay类型不一样,您可以通过颜色区分。");
                return;
            }

            bool containsValSecondHalf = SecondHalfContainsValue(firstPlate);
            if(containsValSecondHalf)
            {
                SetHint("第一块板整版都设置过条码，不能Merge！");
                return;
            }
            
            containsValSecondHalf = SecondHalfContainsValue(secondPlate);
            if (containsValSecondHalf)
            {
                SetHint("第二块板整版都设置过条码，不能Merge！");
                return;
            }

            string assayName = firstPlate.SampleDescription.Split('\t').First();
            MergedPlate = new PlateInfo(firstPlate.Name + "_" + secondPlate.Name, assayName, true);
           
            MergedPlate.CopyFrom(firstPlate, true);
            MergedPlate.CopyFrom(secondPlate, false);
            this.DialogResult = true;
            this.Close();

        }

        private bool SecondHalfContainsValue(PlateInfo plateInfo)
        {
            if(plateInfo.PlateDescription.Contains("merged"))
                return true;
            foreach(var barcodeDef in plateInfo.BarcodeDefinitions)
            {
                if (barcodeDef.Key.WellID <= 48)
                    continue;
                if (barcodeDef.Value != "")
                    return true;
            }
            return false;
        }

        private void SetHint(string s)
        {
            txtHint.Content = s;
            txtHint.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void btnAddPlate2_Click(object sender, RoutedEventArgs e)
        {
            AddHalf(true);
        }

        private void btnAddPlate1_Click(object sender, RoutedEventArgs e)
        {
            AddHalf(false);
        }

        private void AddHalf(bool firstHalf)
        {
            if(lstSrcPlates.SelectedItem == null)
            {
                SetHint("请选中一块板子！");
                return;
            }
            PlateInfo curPlateInfo = (PlateInfo)lstSrcPlates.SelectedItem;
            string sName = curPlateInfo.Name;
            if(firstHalf)
            {
                firstPlate = curPlateInfo;
                txtFirstHalf.Text = sName;
            }
            else
            {
                secondPlate = curPlateInfo;
                txtSecondHalf.Text = sName;
            }
        }
    }
}
