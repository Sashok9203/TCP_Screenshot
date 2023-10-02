using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared_Data
{
    [Serializable]
    public class ClientCommand
    {
        public Command Command { get; set; }
        public int Period { get; set; }
    }
}
