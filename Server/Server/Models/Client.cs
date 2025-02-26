using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{   

    public class ClientClass
    {
        public string name_sucursal { get; set; }
        public string name_disk { get; set; }
        public string tipo { get; set; }

        public int used_storage_gb { get; set; }
        public int full_storage_gb { get; set; }

        public int available_storage_gb { get; set; }
    }
}
