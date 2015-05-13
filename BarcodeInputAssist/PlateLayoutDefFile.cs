using BarcodeInputAssist.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarcodeInputAssist
{
   
    class PlateLayoutDefFile
    {
        readonly string[] fileHeader = {
                                       "3500 Plate Layout File Version 1.0",
                                       "",
                                       "Plate Name	Application Type	Capillary Length (cm)	Polymer	Number of Wells	Owner Name	Barcode Number	Comments",
                           
                                   };
        public const string plateDef = "\tHID\t36\tPOP4\t96\ttjbh";
        const string sampleHeader = "Well	Sample Name	Assay	Results Group	File Name Convention	Sample Type	User Defined Field 1	User Defined Field 2	User Defined Field 3	User Defined Field 4	User Defined Field 5	Comments";
        string assayName = "";
        public PlateInfo Read(string sFile)
        {
            var lines = File.ReadLines(sFile);
            lines = lines.Skip(3);
            string generalInfo = lines.First();
            PlateInfo plateInfo;
            ParseGeneral(generalInfo, out plateInfo);
            
            lines = lines.Skip(1);
            bool setSampleDesc = false;
            foreach(string line in lines)
            {
                if (line == "")
                    continue;
                if (line.Contains("Well") && line.Contains("Sample Name"))
                    continue;
                string testEmpty = line.Replace("\t", "");
                if (testEmpty == "")
                    continue;

                if(!setSampleDesc)
                {
                    plateInfo.SampleDescription = GetSamleDesc(line);
                    setSampleDesc = true;
                }
                
                ParseSample(line, plateInfo);
            }
            if (plateInfo.FileFormat == null)
            {
                if( assayName != "")
                {
                    var formatHeaders = Utility.ReadConfig();
                    plateInfo.FileFormat = formatHeaders.Where(x => x.Assay == assayName).First();
                }
                else
                {
                    //let user select header
                    QueryType queryTypeForm = new QueryType();
                    queryTypeForm.ShowDialog();
                    if (queryTypeForm.SelectedFormat == null)
                        throw new Exception("未能设置文件类型！");
                    plateInfo.FileFormat = queryTypeForm.SelectedFormat;
                }
                
            }
            return plateInfo;
        }

        private void ParseSample(string line, PlateInfo plateInfo)
        {
            List<string> strs = line.Split('\t').ToList();
            if (strs.All(x => x == ""))
                return;
            string well = strs[0];
            string sampleName = strs[1];
            CellPosition cellPosition = new CellPosition(well);
            if(!plateInfo.IsWholePlate)//only 48 position
            {
                if (cellPosition.WellID > 48)
                    return;
            }
            string sAssay = strs[2];
            if(sAssay != "")
            {
                assayName = sAssay;
            }
            plateInfo.BarcodeDefinitions[cellPosition] = sampleName;
        }

        private string GetSamleDesc(string line)
        {
            List<string> strs = line.Split('\t').ToList();
            string sampleDesc = "";
            for(int i = 2; i< 5;i++)
            {
                sampleDesc += strs[i] + "\t";
            }
            return sampleDesc;
        }

        private void ParseGeneral(string generalInfo, out PlateInfo plateInfo)
        {
            List<string> strs = generalInfo.Split('\t').ToList();
            string plateName = strs[0];
            bool bMerged = false;
            if(strs.Count == 8)
            {
                bMerged = strs[7].Contains(strings.merged);
            }
            plateInfo = new PlateInfo(plateName,null,null, bMerged);
            plateInfo.PlateDescription = generalInfo;
        }


        public void Write(string sFile, PlateInfo plateInfo)
        {
            List<string> allLines = new List<string>();
            allLines.AddRange(fileHeader);
            allLines.Add(plateInfo.PlateDescription);
            allLines.Add("");
            allLines.Add(sampleHeader);
            plateInfo.BarcodeDefinitions = plateInfo.BarcodeDefinitions.OrderBy(x => Convert(x.Key)).ToDictionary(x=>x.Key,x=>x.Value);
            string defaultText = "\t\t\t\t\t"+plateInfo.DefaultComment;
            for (int i = 0; i < 96; i++ )
            {
                CellPosition cellPos = new CellPosition(i);
                if(plateInfo.BarcodeDefinitions.ContainsKey(cellPos))
                {
                    string barcode = plateInfo.BarcodeDefinitions[cellPos];
                    string well = cellPos.AlphaInteger + "\t";
                    string sampleType = GetSampleType(barcode);
                    string line = well;
                    if (barcode != "")
                        line += barcode + "\t" + plateInfo.SampleDescription + "\t" + sampleType;
                    else
                        line = GetDefault(cellPos,defaultText);
                    allLines.Add(line);
                }
                else
                {
                    allLines.Add(GetDefault(cellPos, defaultText));
                }
                
            }
            File.WriteAllLines(sFile, allLines);
        }

        private string GetDefault(CellPosition cellPos, string defaultText)
        {
            return cellPos.AlphaInteger + defaultText;
        }

        private int Convert(CellPosition cellPosition)
        {
            return cellPosition.rowIndex + cellPosition.colIndex * 100;
        }



        private string GetSampleType(string barcode)
        {
            string sampleType = "Sample";
            switch(barcode)
            {
                case "+":
                    sampleType = "Positive Control";
                    break;
                case "-":
                    sampleType = "Negative Control";
                    break;
                case "ladder":
                    sampleType = "Allelic Ladder";
                    break;
                default:
                    break;
            }
            return sampleType;
        }
    }

    public class FormattedHeader
    {

        public FormattedHeader(string s, bool hasDescription = false)
        {
            // TODO: Complete member initialization
            string[] strs = s.Split('\t').ToArray();
            int startIndex = hasDescription ? 1 : 0;
            if (hasDescription)
                Description = strs[0];
            Assay = strs[startIndex];
            ResultsGroup = strs[startIndex+1];
            FileNameConvention = strs[startIndex+1];
        }
        public string Description { get; set; }
        public string Assay { get; set; }
        public string ResultsGroup { get; set; }
        public string FileNameConvention { get; set; }
    }

}
