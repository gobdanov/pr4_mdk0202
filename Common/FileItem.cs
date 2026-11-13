using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string FullName { get; set; }
        public bool IsFolder => Type == "Папка";
    }
}
