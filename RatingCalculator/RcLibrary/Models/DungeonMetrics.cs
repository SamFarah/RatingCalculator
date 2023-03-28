using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcLibrary.Models
{
    public class DungeonMetrics
    {        
        public int Level { get; set; }
        public int Base { get; set; }
        public int Min { get { return Base - 10; } }
        public int Max { get { return Base + 5; } }
    }
}
