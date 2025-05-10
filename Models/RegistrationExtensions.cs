using System;
using System.Text;

namespace Health_Care_MIS.Models
{
    public partial class Registration
    {
        public string FullName
        {
            get
            {
                string firstName = Firstname != null ? Encoding.UTF8.GetString(Firstname).Trim() : "";
                return $"{firstName} {Lastname}".Trim();
            }
        }
    }
}
