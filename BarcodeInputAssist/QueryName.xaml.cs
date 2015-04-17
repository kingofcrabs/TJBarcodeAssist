using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;

namespace BarcodeInputAssist
{
    /// <summary>
    /// QueryNumber.xaml 的交互逻辑
    /// </summary>
    public partial class QueryName : Window
    {
        List<string> names;
        public QueryName()
        {
            InitializeComponent();
        }

        public QueryName(List<string> names)
            :this()
        {
            this.names = names;
            FormattedHeaders = ReadConfig();
            lvFormats.ItemsSource = FormattedHeaders;
            lvFormats.SelectedIndex = 0;
            txtPlateName.Focus();
        }

        private List<FormattedHeader> ReadConfig()
        {
            List<FormattedHeader> formattedHeaders = new List<FormattedHeader>();
            string configFile = FolderHelper.GetConfigFolder() + "fileDefinition.txt";
            string[] allLines = File.ReadAllLines(configFile);
            for (int i = 1; i < allLines.Length; i++ )
            {
                formattedHeaders.Add(new FormattedHeader(allLines[i],true));
            }
            return formattedHeaders;
        }

        public List<FormattedHeader> FormattedHeaders{ get; set; }
        public Dictionary<CellPosition, string> PredefinedBarcodes { get; set; }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".doc";
            dlg.Filter = "Word Files (*.doc)|*.doc"; 
            var result = (bool)dlg.ShowDialog();
            if (!result)
                return;
            txtCheckList.Text = dlg.FileName;
            ReadPredefinedBarcodes(dlg.FileName);
            int positive = 0;
            int negative = 0;
            int ladder = 0;
            int sample = 0;
            ParseBarcodes(ref positive, ref negative, ref ladder, ref sample);
            SetHint(string.Format("定义文件有样品{0}个,ladder{1}个,阳性对照{2}个,阴性对照{3}个", sample, ladder, positive, negative));
        }

        private void ParseBarcodes(ref int positive, ref int negative, ref int ladder, ref int sample)
        {
            List<string> allBarcodes = PredefinedBarcodes.Select(x => x.Value).ToList();
            foreach(var barcode in allBarcodes)
            {
                if(barcode.Contains("阳性"))
                {
                    positive++;
                }
                else if( barcode.Contains("阴性"))
                {
                    negative++;
                }
                else if( barcode.Contains("ladder"))
                {
                    negative++;
                }
                else if( barcode.Trim() != "")
                {
                    sample++;
                }
            }

        }

        private void ReadPredefinedBarcodes(string sDocFilePath)
        {
            List<Microsoft.Office.Interop.Word.Range> TablesRanges = new List<Microsoft.Office.Interop.Word.Range>();
            Microsoft.Office.Interop.Word._Application wordApp = new Microsoft.Office.Interop.Word.Application();
            Microsoft.Office.Interop.Word._Document doc = wordApp.Documents.OpenNoRepairDialog(FileName: sDocFilePath, ConfirmConversions: false, ReadOnly: true, AddToRecentFiles: false, NoEncodingDialog: true);
            
            for (int iCounter = 1; iCounter <= doc.Tables.Count; iCounter++)
            {
                Microsoft.Office.Interop.Word.Range TRange = doc.Tables[iCounter].Range;
                TablesRanges.Add(TRange);
            }
            
            CellPosition curPosition = new CellPosition(0);
            for (int par = 1; par <= doc.Paragraphs.Count; par++)
            {
                Microsoft.Office.Interop.Word.Range r = doc.Paragraphs[par].Range;
                foreach (Microsoft.Office.Interop.Word.Range range in TablesRanges)
                {
                    if (r.Start >= range.Start && r.Start <= range.End)
                    {
                        string text = r.Text.Trim();
                        if (text.Length == 3 && char.IsLetter(text[0]) && char.IsDigit(text[1]) && char.IsDigit(text[2]))
                        {
                            curPosition = new CellPosition(text);
                            continue;
                        }
                        string org = "";
                        if (PredefinedBarcodes.ContainsKey(curPosition))
                            org = PredefinedBarcodes[curPosition];
                        PredefinedBarcodes[curPosition] = org + text;
                    }
                }
            }
            doc.Close(Type.Missing, Type.Missing, Type.Missing);
            wordApp.Quit(Type.Missing);
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

            if (lvFormats.SelectedIndex == -1)
            {
                SetHint("请选择一个Assay！");
                return;
            }

            PlateName = sNewPlateName;
            SelectedFormat = (FormattedHeader)(lvFormats.SelectedItem);
            this.DialogResult = true;
            this.Close();
        }

        private void SetHint(string s)
        {
            txtHint.Content = s;
            txtHint.Foreground = new SolidColorBrush(Colors.Red);
        }

        public FormattedHeader SelectedFormat { get; set; }
        public string PlateName { get; set; }
        public string CheckDocPath { get; set; }

     
    }
}
