using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

using ZeroMQ;

namespace ZeroMQ.Test
{
    static partial class Program
    {
        public static void Has(IDictionary<string, string> dict, string[] args)
        {
            foreach (string arg in args)
                Console.WriteLine("{0}: {1}", arg, ZContext.Has(arg));
        }
    }
}
