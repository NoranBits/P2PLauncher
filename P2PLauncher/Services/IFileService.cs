using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PLauncher.Services
{
    internal interface IFileService
    {
        bool CheckPath(string path, bool endsWithFile);
    }
}
