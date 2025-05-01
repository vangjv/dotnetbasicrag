using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotnetbasicrag.Apps
{
    public interface IApp
    {
        string Name { get; }
        Task ExecuteAsync();
    }
}
