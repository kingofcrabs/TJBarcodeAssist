using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace BarcodeInputAssist
{
    class Utility
    {

        
        static public void UpdateDataGridView(DataGridView dataGridView, PlateInfo plateInfo)
        {
            foreach (KeyValuePair<CellPosition, string> pair in plateInfo.BarcodeDefinitions)
            {
                CellPosition cellPos = pair.Key;
                string barcode = pair.Value;
                var cell = dataGridView.Rows[cellPos.rowIndex].Cells[cellPos.colIndex];
                cell.Value = barcode;
            }
        }

        static public void SaveDataGridView(DataGridView dataGridView, PlateInfo curPlateInfo)
        {
            if (curPlateInfo == null)
                return;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                foreach (DataGridViewCell cell in row.Cells)
                {
                    CellPosition cellPos = new CellPosition(cell.ColumnIndex, cell.RowIndex);
                    string sBarcode = cell.Value.ToString();
                    curPlateInfo.BarcodeDefinitions[cellPos] = sBarcode;
                }
            }

        }


        static public void InitDataGridView(DataGridView dataGridView, int sampleCount)
        {
            dataGridView.Columns.Clear();
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            List<string> strs = new List<string>();

            int editableColNum = (sampleCount + 7) / 8;
            const int totalColNum = 12;
            for (int j = 0; j < totalColNum; j++)
                strs.Add("");
            for (int i = 0; i < totalColNum; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.HeaderText = string.Format("{0}", 1 + i);
                dataGridView.Columns.Add(column);
                dataGridView.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
                if (i >= editableColNum)
                {
                    dataGridView.Columns[i].ReadOnly = true;
                    dataGridView.Columns[i].DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
                    dataGridView.Columns[i].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkGray;
                    dataGridView.Columns[i].DefaultCellStyle.SelectionBackColor = System.Drawing.Color.LightGray;
                    dataGridView.Columns[i].DefaultCellStyle.SelectionForeColor = System.Drawing.Color.DarkGray;

                }
            }

            for (int i = 0; i < 8; i++)
            {
                dataGridView.Rows.Add(strs.ToArray());
                dataGridView.Rows[i].HeaderCell.Value = string.Format("{0}", (char)(i + 'A'));
            }
        }

        internal static string GetSaveFolder()
        {
            string sWorkingFolder = ConfigurationManager.AppSettings["workingFolder"];

            string sSaveFolder = sWorkingFolder + "\\" + DateTime.Now.ToString("yyyyMMdd");
            if (!Directory.Exists(sSaveFolder))
                Directory.CreateDirectory(sSaveFolder);
            return sSaveFolder + "\\";
        }


        public static int CellYUnit
        {
            get
            {
                string yUnit = ConfigurationManager.AppSettings["PrintYUnit"];
                return int.Parse(yUnit);
            }
        }
    }
    public struct CellPosition
    {
        public int colIndex;
        public int rowIndex;
        public CellPosition(int ix, int iy)
        {
            colIndex = ix;
            rowIndex = iy;
        }

        public int WellID
        {
            get
            {
                return colIndex * 8 + rowIndex + 1;
            }
        }
        public CellPosition(int wellIndex)
        {
            colIndex = wellIndex / 8;
            rowIndex = wellIndex - 8 * colIndex;
        }

        public CellPosition(string well)
        {
            // TODO: Complete member initialization
            rowIndex = well.First() - 'A';
            colIndex = int.Parse(well.Substring(1)) - 1;
        }

        public string AlphaInteger
        {
            get
            {
                return string.Format("{0}{1:D2}", (char)('A' + rowIndex), colIndex + 1);
            }
        }

        static public string GetDescription(CellPosition cellPosition)
        {
            return string.Format("[条{0}行{1}]",  cellPosition.colIndex + 1, cellPosition.rowIndex + 1);
        }
    }
}
