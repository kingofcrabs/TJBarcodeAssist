using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BarcodeInputAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<PlateInfo> plates = new ObservableCollection<PlateInfo>();
        TraceListener _textBoxListener;
        PlateInfo curPlateInfo = null;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.KeyUp += MainWindow_KeyUp;
            dataGridView.CellValidated += dataGridView_CellValidated;
            LoadAlisNames();
            LoadAssays();
            mainGrid.Background = new SolidColorBrush(Colors.LightGreen);
            mainGrid.Background.Opacity = 0.3;
            dataGridView.SelectionChanged += dataGridView_SelectionChanged;
            ShowLog(ConfigurationManager.AppSettings["showLog"]);
         }

        private void LoadAssays()
        {
            string sConfigFolder = FolderHelper.GetConfigFolder();
            string assaysFile = sConfigFolder + "\\assays.txt";
            var assays = File.ReadAllLines(assaysFile).ToList();
            lstAssays.ItemsSource = assays;
            lstAssays.SelectedIndex = 0;
        }
        private void LoadAlisNames()
        {
            string sConfigFolder = FolderHelper.GetConfigFolder();
            string sPosConfig = sConfigFolder + "\\positive.txt";
            string sNegConfig = sConfigFolder + "\\negative.txt";
            var negFiles = File.ReadAllLines(sNegConfig).ToList();
            var posFiles = File.ReadAllLines(sPosConfig).ToList();
            cmbboxNegative.ItemsSource = negFiles;
            cmbboxPositive.ItemsSource = posFiles;
            cmbboxNegative.SelectedIndex = 0;
            cmbboxPositive.SelectedIndex = 0;
        }

        void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView.SelectedCells.Count == 0 || lstAssays.SelectedIndex == -1)
                return;
            SetSelectedCell2CurrentAssay();
            //dataGridView.ClearSelection();
        }

        private void SetSelectedCell2CurrentAssay()
        {
            string assayName = (string)lstAssays.SelectedItem;
            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                cell.Value = assayName;
            }
        }

        private void ShowLog(string s)
        {
            bool bShow = bool.Parse(s);
            var visible = bShow ? Visibility.Visible : Visibility.Hidden;
            txtLog.Visibility = visible;
            lblLog.Visibility = visible;
        }

        private void lstAssays_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dataGridView.ClearSelection();
        }

        //move to the next cell user wants
        void MainWindow_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && dataGridView.Focused)
            {
                if (dataGridView.CurrentCell.RowIndex == 7)
                {
                    int rowIndex = 0;
                    int colIndex = dataGridView.CurrentCell.ColumnIndex + 1;
                    colIndex = Math.Min(dataGridView.Columns.Count - 1, colIndex);
                    dataGridView.CurrentCell = dataGridView.Rows[rowIndex].Cells[colIndex];
                }
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Trace.Listeners.Clear();
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            WriteAllPlates2File();
        }
        private void AddTracer()
        {
            _textBoxListener = new TextBoxTraceListener(txtLog);
            _textBoxListener.Name = "Textbox";
            _textBoxListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;
            Trace.Listeners.Add(_textBoxListener);
        }

        private void WriteAllPlates2File()
        {
            foreach(var plateInfo in plates)
            {
                PlateLayoutDefFile layoutDefFile = new PlateLayoutDefFile();
                Trace.WriteLine(string.Format("将板子{0}写到文件", plateInfo.Name));
                string workingFolder = Utility.GetSaveFolder();
                string dstFile = workingFolder+ plateInfo.Name + ".txt";
                if(File.Exists(dstFile))
                {
                    Trace.WriteLine(string.Format("文件已经存在于{0}", dstFile));
                    //throw new Exception(string.Format("文件已经存在于{0}",dstFile));
                }
                layoutDefFile.Write(dstFile, plateInfo);
            }
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
            System.Threading.Thread.Sleep(100);
            this.Visibility = System.Windows.Visibility.Visible;
            this.WindowState = System.Windows.WindowState.Normal;
            lstboxPlates.Items.Clear();
            lstboxPlates.ItemsSource = plates;
            AddTracer();
        }

        void dataGridView_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            var cell = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex];
            Dictionary<string, string> map = new Dictionary<string, string>();
            map.Add("+", (string)cmbboxPositive.SelectedItem);
            map.Add("-", (string)cmbboxNegative.SelectedItem);
            string cellVal = cell.FormattedValue.ToString();
            if (map.ContainsKey(cellVal))
                cell.Value = map[cellVal];
        }


        private void SetHint(string s)
        {
            txtHint.Text = s;
            txtHint.Foreground = new SolidColorBrush(Colors.Red);
        }

   
        private void lstboxPlates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lstAssays.SelectedIndex = -1;
            dataGridView.ClearSelection();
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            curPlateInfo = (PlateInfo)lstboxPlates.SelectedItem;
            Utility.InitDataGridView(dataGridView, curPlateInfo.BarcodeDefinitions.Count);
            Utility.UpdateDataGridView(dataGridView, curPlateInfo);
            Trace.WriteLine(string.Format("Selection changed to plate: {0}",curPlateInfo.Name));
        }



        #region menu events
        private void btnAddPlate_Click(object sender, RoutedEventArgs e)
        {
            AddPlate();
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            LoadPlate();
        }

        private void MergePlates()
        {
            Trace.WriteLine("Merge plate");
            SetHint("");
            if (plates.Count < 2)
            {
                SetHint("Merge需要2块板子！");
                return;
            }
            Utility.SaveDataGridView(dataGridView, curPlateInfo);

            MergeForm mergeForm = new MergeForm(plates);
            bool result = (bool)mergeForm.ShowDialog();
            if (!result)
                return;
            plates.Add(mergeForm.MergedPlate);

        }


        bool AlreadyExist(string name)
        {
            bool exist = false;
            foreach (var plate in plates)
            {
                if (plate.Name == name)
                    return true;
            }
            return false;
        }

        private void AddPlate()
        {
            SetHint("");
            
            QueryName queryNameForm = new QueryName(plates.Select(x => x.Name).ToList());
            var result = queryNameForm.ShowDialog();
            if (!(bool)result)
                return;
            string newPlateName = queryNameForm.PlateName;
            if (AlreadyExist(newPlateName))
            {
                SetHint(string.Format("板名为:{0}的微孔板已经存在！",newPlateName));
                return;
            }
          
            PlateInfo newPlateInfo = new PlateInfo(newPlateName, queryNameForm.AssayName);
            Trace.WriteLine(string.Format("Create new plate：{0}, assay: {1}", newPlateName, queryNameForm.AssayName));
            plates.Add(newPlateInfo);

        }

        private void LoadPlate()
        {
            SetHint("");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            PlateLayoutDefFile layoutDefFile = new PlateLayoutDefFile();
            var newPlateInfo = layoutDefFile.Read(openFileDialog.FileName);
            if(AlreadyExist(newPlateInfo.Name))
            {
                SetHint(string.Format("板名为:{0}的微孔板已经存在！", newPlateInfo.Name));
                return;
            }
            plates.Add(newPlateInfo);
        }

        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            MergePlates();
        }
   
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Save.");
            SetHint("");
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            if ((bool)rdbMust.IsChecked)
            {
                string errMsg = "";
                bool bSeq = CheckPlatesSequential(ref errMsg);
                if(!bSeq)
                {
                    SetHint(errMsg);
                    return;
                }
            }
            try
            {
                WriteAllPlates2File();
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex.Message);
                SetHint(ex.Message);
            }
        }

        private bool CheckPlatesSequential(ref string errMsg)
        {
            bool bSeq = true;
            foreach (var plateInfo in plates)
            {
               bSeq = CheckSinglePlateSequential(plateInfo, ref errMsg);
               if (!bSeq)
                   break;
            }
            return bSeq;
        }

        private bool CheckSinglePlateSequential(PlateInfo plateInfo, ref string errMsg)
        {
            //var cellPositions = plateInfo.BarcodeDefinitions.Keys.ToList();
            var cellPositions = plateInfo.BarcodeDefinitions.Where(x => x.Value != "").Select(x => x.Key).ToList();
            var wellIDs = cellPositions.Select(x => x.WellID);
            HashSet<int> allIDs = new HashSet<int>(wellIDs);
            int minWellID = wellIDs.Min();
            int maxWellID = wellIDs.Max();
            for(int id = minWellID; id < maxWellID; id++)
            {
                if(!allIDs.Contains(id))
                {
                    errMsg = string.Format("位于{0}处的样品缺失！", new CellPosition(id - 1).AlphaInteger);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region print
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            SetHint("");
            if (curPlateInfo == null)
            {
                SetHint("没有板子被选中！");
                return;
            }
            Utility.SaveDataGridView(dataGridView, curPlateInfo);

            string bmpPath = SaveBitmap();
            Process.Start(bmpPath);
        }

        private string SaveBitmap()
        {
            System.Windows.Media.Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
            Double dpiX = m.M11 * 96;
            Double dpiY = m.M22 * 96;
            //200*12
            //7*60
            int width = GetFitBMPWidth();
            int height = GetFitBMPHeight();
            var wb = new System.Windows.Media.Imaging.WriteableBitmap(width, height, dpiX, dpiY,
                               PixelFormats.Pbgra32, null);
            wb.Lock();
            var bmp = new System.Drawing.Bitmap(wb.PixelWidth, wb.PixelHeight,
                                                    wb.BackBufferStride,
                                                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                                                    wb.BackBuffer);

            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp); // Good old Graphics
            DrawEverything(g);
            //g.DrawLine( ... ); // etc...

            // ...and finally:
            g.Dispose();
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            string sFile = temp + "\\snapshot.png";
            bmp.Save(sFile);
            bmp.Dispose();

            //wb.AddDirtyRect( ... );
            wb.Unlock();
            return sFile;
        }
        static readonly int yUnit = Utility.CellYUnit;
        static readonly int border = 50;

        private void DrawEverything(System.Drawing.Graphics g)
        {
            int cnt = GetColumnCnt();
            int xUint = GetFitXUnit();
            int totalX = xUint * cnt;
            int totalY = (int)(yUnit * 8.5);
            int cols = cnt;
            int rows = 8;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            int fontSize = int.Parse(ConfigurationManager.AppSettings["PrintFontSize"]);
            System.Drawing.Font font1 = new System.Drawing.Font("SimHei", fontSize);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    CellPosition cellPos = new CellPosition(x, y);
                    if (curPlateInfo.BarcodeDefinitions[cellPos] == "")
                        continue;

                    float startX = (float)(x * xUint + border);
                    float startY = (float)((y + 0.5) * yUnit + border);

                    string content = curPlateInfo.BarcodeDefinitions[cellPos];
                    System.Drawing.PointF startPt = new System.Drawing.PointF(startX, (float)(startY + yUnit * 0.1));
                    g.DrawString(content,
                        font1, System.Drawing.Brushes.Black,
                        new System.Drawing.RectangleF(startPt, new System.Drawing.SizeF(xUint, yUnit)));

                    //String, Font, Brush, RectangleF, StringFormat)
                }
            }

            //竖线
            for (int x = 0; x < cols + 1; x++)
            {
                int xStart = x * xUint + border;
                if (x < cols)
                {
                    g.DrawString(string.Format("{0:D2}", x + 1), font1,
                        System.Drawing.Brushes.Black,
                        new System.Drawing.PointF((float)xStart + xUint * 0.2f, border + yUnit * 0.2f));
                }
                g.DrawLine(System.Drawing.Pens.Black,
                    new System.Drawing.Point(xStart, border),
                    new System.Drawing.Point(xStart, totalY + border));
            }

            //横线
            for (int y = 0; y < rows + 1 + 1; y++)
            {
                int yStart = (int)((y - 0.5) * yUnit + border);
                if (y == 0)
                    yStart = border;
                if (y >= 1 && y < rows + 1)
                {
                    g.DrawString(string.Format("{0}", (char)(y - 1 + 'A')), font1,
                     System.Drawing.Brushes.Black,
                     new System.Drawing.PointF(xUint * 0.2f, (float)yStart + yUnit * 0.1f));
                }
                g.DrawLine(System.Drawing.Pens.Black,
                    new System.Drawing.Point(0, yStart),
                    new System.Drawing.Point(totalX + border, yStart));
            }
            g.DrawLine(System.Drawing.Pens.Black,
                    new System.Drawing.Point(0, border),
                    new System.Drawing.Point(0, totalY + border));
            g.DrawLine(System.Drawing.Pens.Black,
                  new System.Drawing.Point(0, totalY + border),
                  new System.Drawing.Point(totalX + border, totalY + border));
        }

        private int GetFitBMPHeight()
        {
            int length = curPlateInfo.BarcodeDefinitions.Max(x => x.Value.Length);
            return (int)(border + 8.5 * yUnit + 10);
        }

        private int GetFitBMPWidth()
        {
            int cnt = GetColumnCnt();
            int margin = 150;
            return GetFitXUnit() * cnt + margin;
        }

        private int GetColumnCnt()
        {
            return curPlateInfo.IsWholePlate ? 12 : 6;
        }

        private int GetFitXUnit()
        {
            return 100;
        }

        #endregion

        #region help
        private void CommandHelp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            HelpForm help = new HelpForm();
            help.ShowDialog();
        }

        private void CommandHelp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        #endregion

        private void btnSetBarcode_Click(object sender, RoutedEventArgs e)
        {
            lstAssays.SelectedIndex = -1;
            SetHint("");
            if(dataGridView.CurrentCell == null)
            {
                SetHint("请先选中开始样品！");
                return;
            }

            DataGridViewCell currentCell = dataGridView.CurrentCell;
            int maxWellID = curPlateInfo.IsWholePlate ? 96 : 48;

            int totalBarcodeCnt = 0;
            string prefix = "";
            GetBarcodeSetting(ref totalBarcodeCnt,ref prefix);

            int curWellID = new CellPosition(currentCell.ColumnIndex, currentCell.RowIndex).WellID;
            maxWellID = Math.Min(maxWellID, curWellID + totalBarcodeCnt);
            for(int id = curWellID; id <= maxWellID; id++)
            {
                CellPosition cellPos = new CellPosition(id-1);
                dataGridView.Rows[cellPos.rowIndex].Cells[cellPos.colIndex].Value = prefix +(id - curWellID + 1).ToString();
            }
        }

        private void GetBarcodeSetting(ref int totalBarcodeCnt, ref string curBarcode)
        {
            curBarcode = txtStartBarcodeApproach1.Text; 
            totalBarcodeCnt = int.Parse(txtCount.Text);
           
            var selectedCell = dataGridView.CurrentCell;
            CellPosition curCell = new CellPosition(selectedCell.RowIndex, selectedCell.ColumnIndex);
            Trace.WriteLine(string.Format("Start barcode = {0}, count = {1}, position = {2}",
                            curBarcode,  totalBarcodeCnt,
                            CellPosition.GetDescription(curCell)));
        }
       
    }


 

    public static class ExtensionMethods
    {

        private static Action EmptyDelegate = delegate() { };



        public static void Refresh(this UIElement uiElement)
        {

            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

        }

    }
    [ValueConversion(typeof(string), typeof(SolidColorBrush))]
    public class String2ColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string s = (string)value;
            return s.Contains("YF_POP4_xl") ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.LightPink);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
