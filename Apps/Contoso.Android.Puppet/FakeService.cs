﻿using System.IO;
using System.Threading.Tasks;

namespace Contoso.Android.Puppet
{
    static class FakeService
    {
        internal async static Task DoStuffInBackground()
        {
            await Task.Run(() => { throw new IOException("Server did not respond"); });
        }
    }
}
