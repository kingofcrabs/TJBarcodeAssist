using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BarcodeInputAssist
{
    class PlateInfo : BindableBase
    {
        string name;
        bool editable;
        Dictionary<CellPosition,string> barcodeDefinitions;
        public PlateInfo(string plateName, bool editable = true)
        {
            name = plateName;
            this.editable = editable; 
        }

        public bool Editable 
        {
            get
            {
                return editable;
            }
            set
            {
                SetProperty(ref editable, value);
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
        }

    }
}
