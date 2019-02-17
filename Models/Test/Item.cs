using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Models.Test
{
    public class Item
    {
        public List<Cell> Places { get; set; }

        public string Name { get; set; }

        public Price Price { get; set; }

        public int SizeInStack { get; set; } = 0;

        public int StackSize { get; set; } = 0;

        public Item()
        {
            Places = new List<Cell>();
        }
    }

    public class Cell
    {
        public Cell(int left, int top)
        {
            Left = left;
            Top = top;
        }

        public int Left { get; set; }

        public int Top { get; set; }
    }

    public class Price
    {
        public double Cost { get; set; } = -1;

        public int ForNumberItems { get; set; }

        public Currency_ExRate CurrencyType { get; set; } = null;

        public bool IsSet
        {
            get
            {
                if (Cost != -1 && CurrencyType != null)
                    return true;
                else
                    return false;
            }
        }

    }
}
