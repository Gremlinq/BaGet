using System;

namespace BaGet.Core
{
    [Flags]
    public enum ServerMode
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }
}
