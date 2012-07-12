using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;  

namespace CMTVEngine
{
    static public class Utility
    {
        public static void Trace(string msg)
        {
//#if DEBUG
            DateTime d = DateTime.Now;
            Debug.WriteLine(String.Format("{0:00}:{1:00}:{2:000}--", d.Minute, d.Second, d.Millisecond) + msg);
//#endif
        }
    }
}
