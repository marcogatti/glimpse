using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Glimpse.ViewModels
{
    public class LabelViewModel
    {
        public String systemName { get; set; }
        private String _showName;
        public String showName
        {
            get
            {
                return this._showName;
            }
            set
            {
                if (value.StartsWith("[Gmail]/"))
                {
                    this._showName = value.Substring(8); // 8 = longitud de "[Gmail]/"
                }
                else
                {
                    this._showName = value;
                }
            }
        }

        public LabelViewModel(String showableName, String systemName)
        {
            this.showName = showableName;
            this.systemName = systemName;
        }
    }
}