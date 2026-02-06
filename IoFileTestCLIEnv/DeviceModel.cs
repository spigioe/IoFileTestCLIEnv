using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoFileTestCLIEnv
{
    public class DeviceModel
    {
        public string Name { get; set; }      // Pl: "Józsi Laptopja"
        public string IpAddress { get; set; } // Pl: "192.168.1.5"
        public DateTime LastSeen { get; set; } // Mikor láttuk utoljára (Heartbeathez jó lesz)
    }
}
