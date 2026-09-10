using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRMS.models
{
    class PayslipItem
    {

        public PayslipItem(string v, object value)
        {
            this.Name = v;
            this.Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }
    }
}
