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
        string checkDocPath;
        FormattedHeader fileformat;
        Dictionary<CellPosition,string> barcodeDefinitions = new Dictionary<CellPosition,string>();
        public PlateInfo(string plateName,FormattedHeader selectedFormat, Dictionary<CellPosition,string> predefinedBarcodes, bool wholePlate = false)
        {
            name = plateName;
            string merged = wholePlate ? "\t\tmerged" : "";
            fileformat = selectedFormat;
            PlateDescription = plateName + PlateLayoutDefFile.plateDef + merged;
            if(selectedFormat != null)
                SampleDescription = selectedFormat.Assay +"\t" + selectedFormat.ResultsGroup + "\t" + selectedFormat.FileNameConvention;
            this.wholePlate = wholePlate;
            int wellCount = wholePlate ? 96 : 48;
            if (predefinedBarcodes == null)
                predefinedBarcodes = new Dictionary<CellPosition, string>();
            PredefinedBarcodes = predefinedBarcodes;
            checkDocPath = "";
            InitDefinitions(wellCount);
        }

        public bool IsDuplicated(ref string errMsg)
        {
            Dictionary<CellPosition,string> compareDefs = new Dictionary<CellPosition,string>();
            foreach(var pair in barcodeDefinitions)
            {
                if(compareDefs.ContainsValue(pair.Value))
                {
                    errMsg = string.Format("板子：{0}中位于{1}与{2}处的条码重复！",name, pair.Key.AlphaInteger, compareDefs.First(x => x.Value == pair.Value).Key.AlphaInteger);
                    return true;
                }
                compareDefs.Add(pair.Key,pair.Value);
            }
            return false;
        }

        public string DefaultComment
        {
            get
            {
                return fileformat.Assay == "YF_POP4_xl" ? "NULL":"";
            }
        }
        public string PlateDescription { get; set; }
        public string SampleDescription { get; set; }
        public string CheckDocPath
        {
            get
            {
                return checkDocPath;
            }
            set
            {
                SetProperty(ref checkDocPath, value);
            }
        }
        
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




        public Dictionary<CellPosition,string> PredefinedBarcodes { get; set; }

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
