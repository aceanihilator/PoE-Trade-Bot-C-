using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Models.Test
{
    class Tab
    {
        Item[,] _Items;
        Hashtable _SimilarItems;


        public Tab()
        {
            _Items = new Item[12, 12];
            _SimilarItems = new Hashtable();
        }

        public void AddItem(Item item)
        {
            foreach (Cell c in item.Places)
            {
                _Items[c.Left, c.Top] = item;
            }

            if (!_SimilarItems.Contains(item.Name))
            {
                _SimilarItems.Add(item.Name, new ItemGroup());
            }

            if (_SimilarItems.Contains(item.Name))
            {
                (_SimilarItems[item.Name] as ItemGroup).AddItem(item);
            }

        }

        public List<Item> GetItems(int amount, string name, Price price)
        {
            List<Item> result = new List<Item>();

            var group = (_SimilarItems[name] as ItemGroup);

            if (group == null)
                return result;

            if (group.Price != null)

                if (group.TotalSize >= amount && group.Price.Cost / group.Price.ForNumberItems * amount <= price.Cost && price.CurrencyType.Name == group.Price.CurrencyType.Name)
                {
                    result = group.GetItems(amount);

                    if (result.Any())
                        foreach (Item i in result)
                        {
                            foreach (Cell c in i.Places)
                            {
                                _Items[c.Left, c.Top] = null;
                            }

                            group.RemoveItem(i);
                        }
                }

            return result;

        }

    }

    public class ItemGroup
    {
        private List<Item> Items;

        public Price Price { get; set; }

        public int TotalSize { get; private set; }

        public ItemGroup()
        {
            Items = new List<Item>();
        }

        public void AddItem(Item item)
        {
            Items.Add(item);

            TotalSize += item.SizeInStack;

            if (item.Price.IsSet)
            {
                Price = item.Price;
            }
        }

        public List<Item> GetItems(int amount)
        {
            List<Item> result = new List<Item>();


            if (Items.Any())
                foreach (Item i in Items)
                {
                    if (amount > 0)
                    {
                        result.Add(i);
                        amount -= i.SizeInStack;
                    }
                    else
                        break;
                }
            return result;
        }

        internal void RemoveItem(Item i)
        {
            Items.Remove(i);

            TotalSize -= i.SizeInStack;
        }
    }

}
