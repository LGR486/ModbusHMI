using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.DataBaseService
{
    public interface IDataBaseService : IDisposable
    {
        event Action<string> Log;
        void ClearData();
    }
}
