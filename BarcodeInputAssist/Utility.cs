using System;
using System.Collections.Generic;
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

        static public void InitDataGridView(DataGridView dataGridView, int sampleCount)
        {
            dataGridView.Columns.Clear();
            List<string> strs = new List<string>();

            int colNum = (sampleCount + 7) / 8;
            for (int j = 0; j < colNum; j++)
                strs.Add("");
            for (int i = 0; i < colNum; i++)
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.HeaderText = string.Format("{0}", 1 + i);
                dataGridView.Columns.Add(column);
                dataGridView.Columns[i].SortMode = DataGridViewColumnSortMode.Programmatic;
            }
            
            for (int i = 0; i < 8; i++)
            {
                dataGridView.Rows.Add(strs.ToArray());
                dataGridView.Rows[i].HeaderCell.Value = string.Format("{0}", (char)(i + 'A'));
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

        public CellPosition(int wellIndex)
        {
            colIndex = wellIndex / 16;
            rowIndex = wellIndex - 16 * colIndex;
        }

        static public string GetDescription(CellPosition cellPosition)
        {
            return string.Format("[条{0}行{1}]", 1l + cellPosition.colIndex, cellPosition.rowIndex + 1);
        }
    }
}
