using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Models
{
    class Position
    {
        public int Left { get; set; } = -1;

        public int Top { get; set; } = -1;

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsVisible
        {
            get
            {
                if (Left >= 0 && Top >= 0)
                    return true;
                else
                    return false;
            }
        }
    }
}
