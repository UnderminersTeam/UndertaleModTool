using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    public class DebugUtil
    {
        public static void Assert(bool expr, string msg = "Unknown error.")
        {
            if (expr)
                return;

            throw new Exception($"Assertion failed! {msg}");
        }
    }
}
