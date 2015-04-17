using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BarcodeInputAssist
{
    public class PlateInfo : BindableBase
    {
        string name;
        bool wholePlate;

        Dictionary<CellPosition,string> barcodeDefinitions = new Dictionary<CellPosition,string>();
        public PlateInfo(string plateName,FormattedHeader selectedFormat, bool wholePlate = false)
        {
            name = plateName;
            string merged = wholePlate ? "\t\tmerged" : "";
            PlateDescription = plateName + PlateLayoutDefFile.plateDef + merged;
            if(selectedFormat != null)
                SampleDescription = selectedFormat.Assay +"\t" + selectedFormat.ResultsGroup + "\t" + selectedFormat.FileNameConvention;
            this.wholePlate = wholePlate;
            int wellCount = wholePlate ? 96 : 48;
            InitDefinitions(wellCount);
        }

        public string PlateDescription { get; set; }
        public string SampleDescription { get; set; }
        
        
        private void InitDefinitions(int wellCount)
        {

            int colNum = (wellCount + 7) / 8;
         
            for (int c = 0; c < colNum; c++)
            {
                for (int r = 0; r < 8; r++)
                {
                    CellPosition cellPos = new CellPosition(c, r);
                    barcodeDefinitions[cellPos] = "";
                }
            }
        }

        public bool IsWholePlate 
        {
            get
            {
                return wholePlate;
            }
            set
            {
                SetProperty(ref wholePlate, value);
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                SetProperty(ref name, value);
            }
        }

        public Dictionary<CellPosition,string> BarcodeDefinitions 
        { 
            get
            {
                return barcodeDefinitions;
            }
            set
            {
                barcodeDefinitions = value;
            }
        }


        public void CopyFrom(PlateInfo srcPlate, bool copy2FirstHalf)
        {
            for(int i = 0; i<48;i++)
            {
                CellPosition srcCellPos = new CellPosition(i);
                CellPosition dstCellPos = copy2FirstHalf ? srcCellPos : new CellPosition(i + 48);

                if(srcPlate.BarcodeDefinitions.ContainsKey(srcCellPos))
                {
                    barcodeDefinitions[dstCellPos] = srcPlate.BarcodeDefinitions[srcCellPos];
                }
            }
        }
    }
}
