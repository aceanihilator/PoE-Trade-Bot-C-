using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoE_Trade_Bot.Models
{
    public class CustomerInfo
    {
        public enum TradeStatuses
        {
            STARTED, ACCEPTED, CANCELED
        }

        public enum OrderTypes
        {
            SINGLE, MANY
        }

        public string Nickname { get; set; }

        public string Product { get; set; }

        public int NumberProducts { get; set; }

        public double Cost { get; set; }

        public Currency_ExRate Currency { get; set; }

        public string Stash_Tab { get; set; }

        public int Left { get; set; }

        public int Top { get; set; }

        public bool IsReady
        {
            get
            {
                if (OrderType == OrderTypes.SINGLE)
                {
                    if (Nickname != null && Product != null && Cost > 0 && Stash_Tab != null && Currency != null)
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    if (!string.IsNullOrEmpty(Nickname) && !String.IsNullOrEmpty(Product) && Cost > 0 && NumberProducts >= 0 && Currency != null)
                    {
                        return true;
                    }
                    else
                        return false;
                }
                
            }
        }

        public TradeStatuses TradeStatus { get; set; }

        public double Chaos_Price { get; set; }

        public string Item_PoE_Info;

        public bool IsInArea { get; set; }

        public OrderTypes OrderType { get; set; }

        public override string ToString()
        {
            if (OrderType == OrderTypes.SINGLE)
            {
                return $"\nNickname: <{Nickname}> \n" +
                $"Order Typer: <{OrderType}> \n" +
                $"Prodcut: <{Product}> \n" +
                $"Price: <{Cost}> \n" +
                $"Currency: <{Currency.Name}> \n" +
                $"Stash Tab: <{Stash_Tab}> \n" +
                $"Left: <{Left}> \n" +
                $"Top: <{Top}>\n";
            }
            else
            {
                return $"\nNickname: <{Nickname}> \n" +
                $"Order Typer: <{OrderType}> \n" +
                $"Prodcut: <{Product}> \n" +
                $"Number Products: <{NumberProducts}> \n" +
                $"Cost: <{Cost}> \n" +
                $"Currency: <{Currency.Name}> \n" +
                $"Left: <{Left}> \n" +
                $"Top: <{Top}>\n";
            }
        }
    }
}
