using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace PoE_Trade_Bot
{
    
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var Bot_Engine = new BotEngine();
            Bot_Engine.StartBot();

        }
    }
}
