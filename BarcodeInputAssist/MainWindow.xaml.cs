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
        static string filePath = "";
        HeavyWork busyForm;
        bool bMouseDown = false;
        AutoResetEvent evt = new AutoResetEvent(false);
        Dictionary<CellPosition, string> predefinedBarcodes;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            dataGridView.CurrentCellChanged += dataGridView_CurrentCellChanged;
            dataGridView.CellValidated += dataGridView_CellValidated;
            dataGridView.CellEndEdit += dataGridView_CellEndEdit;
            LoadAlisNames();
            LoadAssays();
            mainGrid.Background = new SolidColorBrush(Colors.LightGreen);
            mainGrid.Background.Opacity = 0.3;
            dataGridView.SelectionChanged += dataGridView_SelectionChanged;
            ShowLog(ConfigurationManager.AppSettings["showLog"]);
         }

     
        void dataGridView_CurrentCellChanged(object sender, EventArgs e)
        {
            if (lstAssays.SelectedIndex == -1)
                return;
            bool bAllowEdit = lstAssays.SelectedIndex == 0;
            dataGridView.BeginEdit(!bAllowEdit);
        }

        async void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            CellPosition cellPos = new CellPosition(dataGridView.CurrentCell.ColumnIndex, dataGridView.CurrentCell.RowIndex);
            if (!curPlateInfo.IsWholePlate)
            {
                if (cellPos.WellID > 48)
                    return;
            }
            if (dataGridView.CurrentCell.RowIndex == 7)
            {
                int colIndex = dataGridView.CurrentCell.ColumnIndex + 1;
                colIndex = Math.Min(dataGridView.Columns.Count - 1, colIndex);
                await ChangeCurrentCellDelay(dataGridView.CurrentCell.RowIndex,colIndex);
                //dataGridView.CurrentCell = dataGridView.Rows[0].Cells[colIndex];
            }
        }


        async Task ChangeCurrentCellDelay(int row, int colIndex)
        {
            await Task.Delay(100);
            dataGridView.CurrentCell = dataGridView.Rows[0].Cells[colIndex];
        } 

        private void LoadAssays()
        {
            string sConfigFolder = FolderHelper.GetConfigFolder();
            string assaysFile = sConfigFolder + "\\assays.txt";
            var assays = File.ReadAllLines(assaysFile).ToList();
            assays.Insert(0, "手工编辑");
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
            if (dataGridView.SelectedCells.Count == 0 || lstAssays.SelectedIndex <= 0)
                return;
            SetSelectedCell2CurrentAssay();
            //dataGridView.ClearSelection();
        }

        private void SetSelectedCell2CurrentAssay()
        {
            string assayName = (string)lstAssays.SelectedItem;
            int maxWellID = curPlateInfo.MaxWellID;
            foreach (DataGridViewCell cell in dataGridView.SelectedCells)
            {
                CellPosition cellPos = new CellPosition(cell.ColumnIndex, cell.RowIndex);
                if (cellPos.WellID > maxWellID)
                    continue;
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


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            bool bok = WriteAllPlates2File();
            if (!bok)
            {
                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && (!Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    e.Cancel = true;
                    SetHint("保存到文件失败，如果您确定要关闭，请按住Ctrl键点击关闭按钮。" + txtHint.Text);
                    return;    
                }
            }
            Trace.Listeners.Clear();
        }
        private void AddTracer()
        {
            _textBoxListener = new TextBoxTraceListener(txtLog);
            _textBoxListener.Name = "Textbox";
            _textBoxListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ThreadId;
            Trace.Listeners.Add(_textBoxListener);
        }

        private bool WriteAllPlates2File()
        {
            string errMsg = "";
            bool allConsist = true;
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
                bool consist = IsConsistentWithCheckDoc(plateInfo, ref errMsg);
                if(consist)
                {
                    layoutDefFile.Write(dstFile, plateInfo);
                }
                else
                {
                    allConsist = false;
                }
            }
            SetHint(errMsg);
            return allConsist;
        }

        private bool IsConsistentWithCheckDoc(PlateInfo plateInfo, ref string errMsg)
        {
            foreach(KeyValuePair<CellPosition,string> pair in plateInfo.BarcodeDefinitions)
            {
                if(plateInfo.PredefinedBarcodes.ContainsKey(pair.Key))
                {
                    if(plateInfo.PredefinedBarcodes[pair.Key] != pair.Value)
                    {
                        errMsg += string.Format("板子:{0}中有{1}处与预定义不一致，请检查！", plateInfo.Name, pair.Key.AlphaInteger);
                        return false;
                    }
                }
            }
            return true;
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
            CellPosition cellPos = new CellPosition(cell.ColumnIndex,cell.RowIndex);
            if (!curPlateInfo.IsWholePlate && cellPos.WellID > 48)
                return;

            string cellVal = cell.FormattedValue.ToString();
            SetHint("");
            string errMsg = "";
            bool bConsist = CheckConsist(cellPos, cell, ref errMsg);
            if (bConsist)
                return;
            Dictionary<string, string> map = new Dictionary<string, string>();
            map.Add("+", (string)cmbboxPositive.SelectedItem);
            map.Add("-", (string)cmbboxNegative.SelectedItem);
            if (map.ContainsKey(cellVal))
                cell.Value = map[cellVal];
        }

        private bool CheckConsist(CellPosition cellPos, DataGridViewCell cell, ref string errMsg)
        {
            string cellVal = cell.Value.ToString();
            bool bConsist = false;
            if (curPlateInfo.PredefinedBarcodes.Count > 0)
            {
                if (curPlateInfo.PredefinedBarcodes.ContainsKey(cellPos))
                {
                    string expectedBarcode = curPlateInfo.PredefinedBarcodes[cellPos];
                    bConsist = (expectedBarcode == cellVal);
                    if(!bConsist)
                    {
                        cell.Style.BackColor = System.Drawing.Color.Pink;
                        SetHint(string.Format("位于{0}处的条码错误，预期条码为:{1}", cellPos.AlphaInteger, curPlateInfo.PredefinedBarcodes[cellPos]));
                    }
                    else
                    {
                        cell.Style.BackColor = System.Drawing.Color.LightGreen;
                    }
                }
            }
            return bConsist;
        }


        private void SetHint(string s, bool bRed = true)
        {
            txtHint.Text = s;
            var color = bRed ? Colors.Red : Colors.Blue;
            txtHint.Foreground = new SolidColorBrush(color);
        }

   
        private void lstboxPlates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetHint("");
            
            lstAssays.SelectedIndex = -1;
            dataGridView.ClearSelection();
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            curPlateInfo = (PlateInfo)lstboxPlates.SelectedItem;
            txtCheckDoc.DataContext = curPlateInfo;
            Utility.InitDataGridView(dataGridView, curPlateInfo.BarcodeDefinitions.Count);
            Utility.UpdateDataGridView(dataGridView, curPlateInfo);

            int totalWell = curPlateInfo.MaxWellID;
            bool bok = false;
            string errMsg = "";
            foreach(var pair in curPlateInfo.PredefinedBarcodes)
            {
                if(pair.Key.WellID <= totalWell)
                {
                    var cell = dataGridView.Rows[pair.Key.rowIndex].Cells[pair.Key.colIndex];
                    bok = CheckConsist(pair.Key, cell, ref errMsg);
                }
            }

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
            SetHint("Merge完成。");
        }


        bool AlreadyExist(string name)
        {
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
          
            PlateInfo newPlateInfo = new PlateInfo(newPlateName, queryNameForm.SelectedFormat,queryNameForm.PredefinedBarcodes);
            Trace.WriteLine(string.Format("Create new plate：{0}, assay: {1}", newPlateName, queryNameForm.SelectedFormat));
            plates.Add(newPlateInfo);
            lstboxPlates.SelectedIndex = plates.Count - 1;
        }

        private void LoadPlate()
        {
            SetHint("");
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return;
                PlateLayoutDefFile layoutDefFile = new PlateLayoutDefFile();
                var newPlateInfo = layoutDefFile.Read(openFileDialog.FileName);
                if (AlreadyExist(newPlateInfo.Name))
                {
                    SetHint(string.Format("板名为:{0}的微孔板已经存在！", newPlateInfo.Name));
                    return;
                }
                plates.Add(newPlateInfo);
                lstboxPlates.SelectedIndex = plates.Count - 1;
            }
            catch(Exception ex)
            {
                SetHint(ex.Message);
            }
        }


   
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine("Save.");
            SetHint("");
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            string errMsg = "";
            if ((bool)rdbMust.IsChecked)
            {
                errMsg = "";
                bool bSeq = CheckPlatesSequential(ref errMsg);
                if(!bSeq)
                {
                    SetHint(errMsg);
                    return;
                }
            }
            try
            {
                bool bok = WriteAllPlates2File();
                if(bok)
                {
                    SetHint("保存成功。",false);
                }
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
            if (wellIDs.Count() == 0)
                return true;
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
            int firstLineHeight = 30;
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
                    float startY = (float)((y + 0.5) * yUnit + border + firstLineHeight);

                    string content = curPlateInfo.BarcodeDefinitions[cellPos];
                    System.Drawing.PointF startPt = new System.Drawing.PointF(startX, (float)(startY + yUnit * 0.1));
                    g.DrawString(content,
                        font1, System.Drawing.Brushes.Black,
                        new System.Drawing.RectangleF(startPt, new System.Drawing.SizeF(xUint, yUnit)));

                    if(x==0 && y ==0)
                    {
                        g.DrawString("板名：________________检验时间：______________检验人：______________复核人：______________", font1, System.Drawing.Brushes.Black,
                          new System.Drawing.RectangleF(new  System.Drawing.PointF(0,border), new System.Drawing.SizeF(1000, yUnit)));


                    }
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
                        new System.Drawing.PointF((float)xStart + xUint * 0.2f,firstLineHeight+ border + yUnit * 0.2f));
                }
                g.DrawLine(System.Drawing.Pens.Black,
                    new System.Drawing.Point(xStart, firstLineHeight + border),
                    new System.Drawing.Point(xStart, totalY + border + firstLineHeight));
            }

            //横线
            for (int y = 0; y < rows + 1 + 1; y++)
            {
                int yStart = (int)((y - 0.5) * yUnit + border);
                if (y == 0)
                    yStart = border;
                yStart += firstLineHeight;
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
                    new System.Drawing.Point(0, firstLineHeight + border),
                    new System.Drawing.Point(0, totalY + border + firstLineHeight));
            g.DrawLine(System.Drawing.Pens.Black,
                  new System.Drawing.Point(0, totalY + border + firstLineHeight),
                  new System.Drawing.Point(totalX + border, totalY + border + firstLineHeight));
        }

        private int GetFitBMPHeight()
        {
            int length = curPlateInfo.BarcodeDefinitions.Max(x => x.Value.Length);
            return (int)(border + 8.5 * yUnit + 40);
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

        #region set batch barcode
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
            int maxWellID = curPlateInfo.MaxWellID;

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
        #endregion
      
        #region set check doc



        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            SetHint("");
            if(curPlateInfo == null)
            {
                SetHint("未选中一块板子！");
                return;
            }
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".doc";
            dlg.Filter = "Word Files (*.doc)|*.doc";
            var result = (bool)dlg.ShowDialog();
            
            if (!result)
                return;

            busyForm = new HeavyWork();
            
            filePath = dlg.FileName;
            ThreadStart thStart = new ThreadStart(ReadPredefinedBarcodesInThread);
            Thread th = new Thread(thStart);
            th.Start();
            busyForm.ShowDialog();


            for (int i = 0; i < predefinedBarcodes.Count; i++)
            {
                var pair = predefinedBarcodes.ElementAt(i);
                if (pair.Value.Contains("阳"))
                    predefinedBarcodes[pair.Key] = "+";
                else if (pair.Value.Contains("阴"))
                    predefinedBarcodes[pair.Key] = "-";
            }

            if (predefinedBarcodes.Count > 0)
            {
                int positive = 0;
                int negative = 0;
                int ladder = 0;
                int sample = 0;
                ParseBarcodes(ref positive, ref negative, ref ladder, ref sample, predefinedBarcodes);
                SetHint(string.Format("定义文件有样品{0}个,ladder{1}个,阳性对照{2}个,阴性对照{3}个", sample, ladder, positive, negative), false);
                curPlateInfo.CheckDocPath = dlg.FileName;
                curPlateInfo.PredefinedBarcodes = predefinedBarcodes;
            }
        }
        private void ParseBarcodes(ref int positive, ref int negative, ref int ladder, ref int sample, Dictionary<CellPosition, string> predefinedBarcodes)
        {
            List<string> allBarcodes = predefinedBarcodes.Select(x => x.Value).ToList();

            foreach (var barcode in allBarcodes)
            {
                string tmp = barcode;
                tmp = tmp.Replace("\r", "");
                tmp = tmp.Replace("\a", "");
                if (tmp.Contains("阳性"))
                {
                    positive++;
                }
                else if (tmp.Contains("阴性"))
                {
                    negative++;
                }
                else if (tmp.Contains("ladder"))
                {
                    ladder++;
                }
                else if (tmp.Trim() != "")
                {
                    sample++;
                }
            }

        }


        public  void ReadPredefinedBarcodesInThread()
        {
            predefinedBarcodes = ReadPredefinedBarcodes(filePath);
            this.Dispatcher.Invoke(new Action(() =>
            { busyForm.Close(); }));
            
            
        }
     
        private  Dictionary<CellPosition, string> ReadPredefinedBarcodes(string sDocFilePath)
        {
            var predefinedBarcodes = new Dictionary<CellPosition, string>();
            //List<Microsoft.Office.Interop.Word.Range> TablesRanges = new List<Microsoft.Office.Interop.Word.Range>();
            //Microsoft.Office.Interop.Word._Application wordApp = new Microsoft.Office.Interop.Word.Application();
            //Microsoft.Office.Interop.Word._Document doc = wordApp.Documents.OpenNoRepairDialog(FileName: sDocFilePath, ConfirmConversions: false, ReadOnly: true, AddToRecentFiles: false, NoEncodingDialog: true);

            //for (int iCounter = 1; iCounter <= doc.Tables.Count; iCounter++)
            //{
            //    Microsoft.Office.Interop.Word.Range TRange = doc.Tables[iCounter].Range;
            //    TablesRanges.Add(TRange);
            //}

            //CellPosition curPosition = new CellPosition(0);
            //for (int par = 1; par <= doc.Paragraphs.Count; par++)
            //{
            //    Microsoft.Office.Interop.Word.Range r = doc.Paragraphs[par].Range;
            //    foreach (Microsoft.Office.Interop.Word.Range range in TablesRanges)
            //    {
            //        if (r.Start >= range.Start && r.Start <= range.End)
            //        {
            //            string text = r.Text.Trim();
            //            if (text.Length == 3 && char.IsLetter(text[0]) && char.IsDigit(text[1]) && char.IsDigit(text[2]))
            //            {
            //                curPosition = new CellPosition(text);
            //                continue;
            //            }
            //            string org = "";
            //            if (predefinedBarcodes.ContainsKey(curPosition))
            //                org = predefinedBarcodes[curPosition];
            //            predefinedBarcodes[curPosition] = org + text;
            //        }
            //    }
            //}
            //doc.Close(Type.Missing, Type.Missing, Type.Missing);
            //wordApp.Quit(Type.Missing);
            //for (int i = 0; i < predefinedBarcodes.Count; i++)
            //{
            //    var pair = predefinedBarcodes.ElementAt(i);
            //    string org = pair.Value;
            //    org = org.Replace("\r", "");
            //    org = org.Replace("\a", "");
            //    org = org.Replace("\n", "");
            //    predefinedBarcodes[pair.Key] = org;
            //}
            //predefinedBarcodes = predefinedBarcodes.Where(x => x.Value != "").ToDictionary(x => x.Key, x => x.Value);
            return predefinedBarcodes;
        }
        #endregion

        #region split & merge
        private void btnMerge_Click(object sender, RoutedEventArgs e)
        {
            MergePlates();
        }

        private void btnSplit_Click(object sender, RoutedEventArgs e)
        {
            SplitPlate();
        }

        private void SplitPlate()
        {
            SetHint("");
            if(lstboxPlates.SelectedItem == null)
            {
                SetHint("请先选中一块板子！");
                return;
            }
            Utility.SaveDataGridView(dataGridView, curPlateInfo);
            var srcPlate = (PlateInfo)lstboxPlates.SelectedItem;
            FormattedHeader formattedHeader = srcPlate.FileFormat;
            PlateInfo firstHalf = new PlateInfo(srcPlate.Name + "_1", formattedHeader, null, false);
            PlateInfo secondHalf = new PlateInfo(srcPlate.Name + "_2", formattedHeader, null, false);

            firstHalf.SplitFrom(srcPlate, true);
            secondHalf.SplitFrom(srcPlate, false);
            plates.Add(firstHalf);
            plates.Add(secondHalf);
            SetHint("切分完成",false);
        }
        #endregion
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
            Color color = Colors.LightBlue;
            if(s.Contains("YF_POP4_xl"))
                color =  Colors.LightGreen;
            else if(s.Contains("PP-Promega"))
                color = Colors.LightPink;
            else if(s.Contains("tjbh HID"))
                color = Colors.LightBlue;
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
