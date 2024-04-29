using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieSharp.Tools;
public static class Defer
{
    public class DeferContext : IDisposable
    {
        private readonly Action disposer;

        public DeferContext(Action disposer)
        {
            this.disposer = disposer;
        }

        public void Dispose()
        {
            this.disposer();
            GC.SuppressFinalize(this);
        }
    }

    public static DeferContext Run(Action disposer) {
        return new DeferContext(disposer);
    }
}
