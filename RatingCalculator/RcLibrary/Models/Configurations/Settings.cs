using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models.Configurations
{
    public class Settings
    {
        public const string MainSectionName = "Settings";
        public string? RaiderIOAPI { get; set; }
        public int ExpansionId { get; set; }
    }
}
