using Microsoft.Win32;
using PoE_Trade_Bot.Models;
using PoE_Trade_Bot.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PoE_Trade_Bot.Models.Test;

namespace PoE_Trade_Bot
{
    public class BotEngine
    {
        private static string PoE_Path;
        private static string PoE_Logs_Dir;
        private static string PoE_Logs_File;

        private Task ReadLogs;
        private Task ExchangeRatesTask;

        private readonly int Top_Stash64 = 135;
        private readonly int Left_Stash64 = 25;

        private List<CustomerInfo> Customer;
        private List<CustomerInfo> CompletedTrades;

        private Tab _Tab;

        //current instance customer
        private Currencies Currencies;

        private bool IsAfk = false;

        public BotEngine()
        {
            Customer = new List<CustomerInfo>();
            CompletedTrades = new List<CustomerInfo>();

            var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\GrindingGearGames\Path of Exile", "InstallLocation", null);
            if (path != null)
            {
                PoE_Path = path.ToString();
                PoE_Logs_Dir = PoE_Path + @"logs\";
                PoE_Logs_File = PoE_Logs_Dir + @"\Client.txt";
            }

            if (!Win32.IsPoERun())
            {
                throw new Exception("Path of Exile is not running!");
            }
        }

        public void StartBot()
        {
            Currencies = new Currencies();
            _Tab = new Tab();

            ReadLogs = new Task(ReadLogsInBack);
            ReadLogs.Start();

            ExchangeRatesTask = new Task(CheckExchangeRates);
            ExchangeRatesTask.Start();

            StartTrader_PoEbota();

            Console.ReadKey();
        }

        private void CheckExchangeRates()
        {
            DateTime timer = DateTime.Now + new TimeSpan(0,30,0);

            while (true)
            {
                if (timer <= DateTime.Now)
                {
                    Currencies.Update();
                    timer = DateTime.Now + new TimeSpan(0, 30, 0);
                }

                Thread.Sleep(1000 * 60 * 5);
            }
        }

        private void ReadLogsInBack()
        {
            if (Win32.GetActiveWindowTitle() != "Path of Exile")
            {
                Console.WriteLine("Main window is not Path of Exile. ");

                Win32.PoE_MainWindow();
            }

            int last_index = -1;
            bool not_first = false;

            while (true)
            {
                var fs = new FileStream(PoE_Logs_File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var sr = new StreamReader(fs))
                {
                    int li = 0;
                    string ll = string.Empty;
                    while (!sr.EndOfStream)
                    {
                        li++;
                        ll = sr.ReadLine();

                        if (not_first && li > last_index)
                        {
                            if (ll.Contains("a24 [INFO Client"))
                            {
                                Console.WriteLine(ll);
                                GetInfo(ll);
                            }
                        }
                    }

                    sr.Dispose();
                    fs.Dispose();

                    if (li > last_index)
                    {
                        last_index = li;

                        if (!not_first)
                            not_first = true;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void GetInfo(string log24)
        {
            //GetFullInfoCustomer
            try
            {
                if (log24.Contains("Hi, I would like to buy your") && log24.Contains("@From"))
                {
                    var cus_inf = new CustomerInfo();

                    cus_inf.OrderType = CustomerInfo.OrderTypes.SINGLE;

                    int length;
                    int begin;
                    //Nickname

                    if (!log24.Contains("> "))
                    {
                        begin = log24.IndexOf("@From ") + 6;
                        length = log24.IndexOf(": ") - begin;
                        cus_inf.Nickname = log24.Substring(begin, length);
                    }
                    else
                    {
                        begin = log24.IndexOf("> ") + 2;
                        length = log24.IndexOf(": ") - begin;
                        cus_inf.Nickname = log24.Substring(begin, length);
                    }


                    //Product
                    begin = log24.IndexOf("your ") + 5;
                    length = log24.IndexOf(" listed") - begin;
                    cus_inf.Product = log24.Substring(begin, length);

                    //Currency
                    begin = log24.IndexOf(" in") - 1;
                    for (int i = 0; i < 50; i++)
                    {
                        if (log24[begin - i] == ' ')
                        {
                            begin = begin - i + 1;
                            break;
                        }
                    }
                    length = log24.IndexOf(" in") - begin;
                    cus_inf.Currency = Currencies.GetCurrencyByName(log24.Substring(begin, length));

                    //Price
                    begin = log24.IndexOf("for ") + 4;
                    cus_inf.Cost = GetNumber(begin, log24);

                    //Stash Tab
                    begin = log24.IndexOf("tab \"") + 5;
                    length = log24.IndexOf("\"; position") - begin;
                    cus_inf.Stash_Tab = log24.Substring(begin, length);

                    //left
                    begin = log24.IndexOf("left ") + 5;
                    cus_inf.Left = (int)GetNumber(begin, log24);

                    //top
                    begin = log24.IndexOf("top ") + 4;
                    cus_inf.Top = (int)GetNumber(begin, log24);

                    //to chaos chaosequivalent
                    cus_inf.Chaos_Price = cus_inf.Currency.ChaosEquivalent * cus_inf.Cost;

                    //trade accepted
                    cus_inf.TradeStatus = CustomerInfo.TradeStatuses.STARTED;

                    if (cus_inf.IsReady)
                    {
                        Customer.Add(cus_inf);

                        Console.WriteLine(cus_inf.ToString());
                    }
                }

                if (log24.Contains("I'd like to buy your") && log24.Contains("@From"))
                {
                    var cus = new CustomerInfo();

                    cus.OrderType = CustomerInfo.OrderTypes.MANY;

                    cus.Nickname = Regex.Replace(log24, @"([\w\s\W]+@From )|(: [\w\W\s]*)|(<[\w\W\s]+> )", "");

                    cus.Product = Regex.Replace(log24, @"([\w\W]+your +[\d,]* )|( for+[\w\s\W]*)|( Map [()\d\w]+)", "");

                    string test = Regex.Match(log24, @"your ([\d]+)").Groups[1].Value;

                    cus.NumberProducts = Convert.ToInt32(test);

                    cus.Cost = Convert.ToDouble(Regex.Replace(log24, @"([\s\w\W]+for my )|([\D])", "").Replace(".", ","));

                    cus.Currency = Currencies.GetCurrencyByName(Regex.Replace(log24, @"([\w\s\W]+my +[\d,.]* )|( in +[\w\W\s]*)", ""));

                    if (cus.IsReady)
                    {
                        Customer.Add(cus);

                        Console.WriteLine(cus.ToString());
                    }

                }
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }

            //check area
            if (Customer.Any())
            {
                if (log24.Contains(Customer.First().Nickname) && log24.Contains("has joined the area"))
                {
                    Console.WriteLine("He come for product");
                    Customer.First().IsInArea = true;
                }

                if (log24.Contains("Player not found in this area."))
                {
                    Customer.First().IsInArea = false;
                }

                if (log24.Contains(": Trade accepted."))
                {
                    Customer.First().TradeStatus = CustomerInfo.TradeStatuses.ACCEPTED;
                }

                if (log24.Contains(": Trade cancelled."))
                {
                    Customer.First().TradeStatus = CustomerInfo.TradeStatuses.CANCELED;
                }
            }
            else
            {
                if (log24.Contains("AFK mode is now ON. Autoreply"))
                {
                    IsAfk = true;
                }
                if (log24.Contains("AFK mode is now OFF"))
                {
                    IsAfk = false;
                }
            }

        }

        //Trade Functions

        private void StartTrader_PoEbota()
        {
            bool IsFirstTime = true;

            DateTime timer = DateTime.Now + new TimeSpan(0, new Random().Next(4, 6), 0);

            while (true)
            {
                if ((IsAfk && !Customer.Any()) || (!Customer.Any() && timer < DateTime.Now))
                {
                    if (Win32.GetActiveWindowTitle() != "Path of Exile")
                    {
                        Win32.PoE_MainWindow();
                    }

                    Win32.ChatCommand("&I am here");

                    timer = DateTime.Now + new TimeSpan(0, new Random().Next(4, 6), 0);

                    IsAfk = false;
                }

                if (IsFirstTime)
                {
                    if (Win32.GetActiveWindowTitle() != "Path of Exile")
                    {
                        Win32.PoE_MainWindow();

                        if (!OpenStash())
                        {
                            Console.WriteLine("");

                            IsFirstTime = false;

                            throw new Exception("Stash is not found in the area.");

                        }

                        ClearInventory("recycle_tab");

                        ScanTab();

                        IsFirstTime = false;
                    }
                }

                if (Customer.Any() && !IsFirstTime)
                {
                    if (Win32.GetActiveWindowTitle() != "Path of Exile")
                    {
                        Win32.PoE_MainWindow();
                    }

                    Console.WriteLine($"\nTrade start with {Customer.First().Nickname}");

                    #region Many items

                    if (Customer.First().OrderType == CustomerInfo.OrderTypes.MANY)
                    {
                        InviteCustomer();

                        if (!OpenStash())
                        {
                            KickFormParty();
                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        if (!TakeItems())
                        {
                            KickFormParty();
                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //check is area contain customer
                        if (!CheckArea())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //start trade
                        if (!TradeQuery())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }


                        if (!PutItems())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        if (!CheckCurrency())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }
                    }

                    #endregion

                    #region Single item

                    if (Customer.First().OrderType == CustomerInfo.OrderTypes.SINGLE)
                    {
                         InviteCustomer();

                        if (!OpenStash())
                        {
                            KickFormParty();
                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        if (!TakeProduct())
                        {
                            KickFormParty();
                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //check is area contain customer
                        if (!CheckArea())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //start trade
                        if (!TradeQuery())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //get product
                        if (!GetProduct())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }

                        //search and check currency
                        if (!CheckCurrency())
                        {
                            KickFormParty();

                            Customer.Remove(Customer.First());

                            Console.WriteLine("\nTrade end!");
                            continue;
                        }
                    }

                    #endregion



                    Win32.ChatCommand($"@{Customer.First().Nickname} ty gl");

                    KickFormParty();

                    CompletedTrades.Add(Customer.First());

                    Customer.Remove(Customer.First());

                    if (!OpenStash())
                    {
                        Console.WriteLine("Stash not found. I cant clean inventory after trade.");
                    }
                    else
                    {
                        ClearInventory();
                    }

                    Console.WriteLine("Trade comlete sccessfull");
                }

                Thread.Sleep(100);
            }
        }

        private void InviteCustomer()
        {
            if (Win32.GetActiveWindowTitle() != "Path of Exile")
            {
                Win32.PoE_MainWindow();
            }

            Console.WriteLine("Invite in party...");

            string command = "/invite " + Customer.First().Nickname;

            Win32.ChatCommand(command);
        }

        private bool OpenStash()
        {
            Bitmap screen_shot = null;
            Position found_pos = null;

            //find stash poition

            Console.WriteLine("Search stash in location...");

            for (int search_pos = 0; search_pos < 20; search_pos++)
            {
                screen_shot = ScreenCapture.CaptureScreen();
                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/stashtitle.png");

                if (found_pos.IsVisible)
                {
                    Win32.MoveTo(found_pos.Left + found_pos.Width / 2, found_pos.Top + found_pos.Height);

                    Thread.Sleep(100);

                    Win32.DoMouseClick();

                    Thread.Sleep(100);

                    Win32.MoveTo(screen_shot.Width / 2, screen_shot.Height / 2);

                    var timer = DateTime.Now + new TimeSpan(0, 0, 5);

                    while (true)
                    {
                        screen_shot = ScreenCapture.CaptureRectangle(140, 32, 195, 45);

                        var pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/open_stash.png");

                        if (pos.IsVisible)
                        {
                            screen_shot.Dispose();

                            return true;
                        }

                        if (timer < DateTime.Now)
                            break;

                        Thread.Sleep(500);
                    }
                }

                screen_shot.Dispose();

                Thread.Sleep(500);
            }

            Console.WriteLine("Stash is not found");

            return false;
        }

        private bool TakeProduct()
        {
            Bitmap screen_shot = null;
            Position found_pos = null;

            Console.WriteLine("Search trade tab...");

            for (int count_try = 0; count_try < 16; count_try++)
            {
                screen_shot = ScreenCapture.CaptureRectangle(10, 90, 450, 30);

                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/notactive_trade_tab.jpg");

                if (found_pos.IsVisible)
                    break;
                else
                {
                    found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/active_trade_tab.jpg");
                    if (found_pos.IsVisible)
                    {
                        break;
                    }
                }

                screen_shot.Dispose();

                Thread.Sleep(500);
            }

            screen_shot.Dispose();

            if (found_pos.IsVisible)
            {
                Win32.MoveTo(10 + found_pos.Left + found_pos.Width / 2, 90 + found_pos.Top + found_pos.Height / 2);

                Thread.Sleep(100);

                Win32.DoMouseClick();

                Thread.Sleep(300);

                Win32.MoveTo(Left_Stash64 + 38 * (Customer.First().Left - 1), Top_Stash64 + 38 * (Customer.First().Top - 1));

                Thread.Sleep(1000);

                string ctrlc = CtrlC_PoE();

                string product_clip = GetNameItem_PoE(ctrlc);

                if (product_clip == null || !Customer.First().Product.Contains(product_clip))
                {
                    Console.WriteLine("not found item");

                    Win32.ChatCommand($"@{Customer.First().Nickname} I sold it, sry");

                    Win32.SendKeyInPoE("{ESC}");

                    return false;
                }

                if (!IsValidPrice(ctrlc))
                {
                    Console.WriteLine("Fake price");

                    Win32.ChatCommand($"@{Customer.First().Nickname} It is not my price!");

                    Win32.SendKeyInPoE("{ESC}");

                    return false;
                }

                Win32.CtrlMouseClick();

                Thread.Sleep(100);

                Win32.MoveTo(750, 350);

                Win32.SendKeyInPoE("{ESC}");

                return true;

            }

            Console.WriteLine("Trade tab is not found");

            return false;
        }

        private bool CheckArea()
        {
            Console.WriteLine("Check area...");
            for (int i = 0; i < 60; i++)
            {
                if (Customer.First().IsInArea)
                {
                    return true;
                }
                Thread.Sleep(500);
            }
            Console.WriteLine("Player not here");
            return false;
        }

        private bool TradeQuery()
        {
            Position found_pos = null;

            Bitmap screen_shot = null;

            bool amIdoRequest = false;

            for (int try_count = 0; try_count < 3; try_count++)
            {
                Console.WriteLine("Try to accept or do trade...");

                for (int i = 0; i < 10; i++)
                {
                    if (!amIdoRequest)
                    {
                        screen_shot = ScreenCapture.CaptureRectangle(1030, 260, 330, 500);

                        found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/accespt.png");

                        if (found_pos.IsVisible)
                        {
                            Console.WriteLine("I will Accept trade request!");

                            Win32.MoveTo(1030 + found_pos.Left + found_pos.Width / 2, 260 + found_pos.Top + found_pos.Height / 2);

                            Thread.Sleep(100);

                            Win32.DoMouseClick();

                            Thread.Sleep(100);

                            Win32.MoveTo((1030 + screen_shot.Width) / 2, (260 + screen_shot.Height) / 2);

                            amIdoRequest = true;
                        }
                        else
                        {
                            Console.WriteLine("i write trade");
                            string trade_command = "/tradewith " + Customer.First().Nickname;

                            Win32.ChatCommand(trade_command);

                            screen_shot = ScreenCapture.CaptureRectangle(455, 285, 475, 210);

                            if (!Customer.First().IsInArea)
                                return false;

                            found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/trade_waiting.png");
                            if (found_pos.IsVisible)
                            {
                                Win32.MoveTo(455 + found_pos.Left + found_pos.Width / 2, 285 + found_pos.Top + found_pos.Height / 2);

                                screen_shot.Dispose();

                                amIdoRequest = true;
                            }
                            else
                            {
                                Console.WriteLine("Check trade window");
                                screen_shot = ScreenCapture.CaptureRectangle(330, 15, 235, 130);
                                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/trade_window_title.png");
                                if (found_pos.IsVisible)
                                {
                                    Console.WriteLine("I am in trade!");
                                    screen_shot.Dispose();
                                    return true;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Check trade window");
                        screen_shot = ScreenCapture.CaptureRectangle(330, 15, 235, 130);
                        found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/trade_window_title.png");
                        if (found_pos.IsVisible)
                        {
                            screen_shot.Dispose();
                            Console.WriteLine("I am in trade!");
                            return true;
                        }
                    }
                    Thread.Sleep(500);
                }

            }
            return false;
        }

        private bool GetProduct()
        {
            int x_inventory = 925;
            int y_inventory = 440;
            int offset = 37;

            Bitmap screen_shot;

            for (int j = 0; j < 12; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    Win32.MoveTo(x_inventory + offset * j, y_inventory + 175);

                    Thread.Sleep(100);

                    screen_shot = ScreenCapture.CaptureRectangle(x_inventory - 30 + offset * j, y_inventory - 30 + offset * i, 60, 60);

                    Position pos = OpenCV_Service.FindObject(screen_shot, "Assets/UI_Fragments/empty_cel.png", 0.4);

                    if (!pos.IsVisible)
                    {
                        Clipboard.Clear();

                        string ss = null;

                        Thread.Sleep(100);

                        Win32.MoveTo(x_inventory + offset * j, y_inventory + offset * i);

                        var time = DateTime.Now + new TimeSpan(0, 0, 5);

                        while (ss == null)
                        {
                            Win32.SendKeyInPoE("^c");
                            ss = Win32.GetText();

                            if (time < DateTime.Now)
                                ss = "empty_string";
                        }

                        if (ss == "empty_string")
                            continue;

                        if (Customer.First().Product.Contains(GetNameItem_PoE(ss)))
                        {
                            Console.WriteLine($"{ss} is found in inventory");

                            Win32.CtrlMouseClick();

                            screen_shot.Dispose();

                            return true;
                        }

                    }
                    screen_shot.Dispose();
                }
            }
            Win32.SendKeyInPoE("{ESC}");

            Win32.ChatCommand("@" + Customer.First().Nickname + " i sold it, sry");

            return false;
        }

        private bool CheckCurrency()
        {
            List<Position> found_positions = null;

            List<Currency_ExRate> main_currs = new List<Currency_ExRate>();

            //set main currencies

            main_currs.Add(Customer.First().Currency);

            if (Customer.First().Currency.Name != "chaos orb")
            {
                main_currs.Add(Currencies.GetCurrencyByName("chaos"));
            }

            if (Customer.First().Currency.Name != "divine orb")
            {
                main_currs.Add(Currencies.GetCurrencyByName("divine"));
            }

            if (Customer.First().Currency.Name != "exalted orb")
            {
                main_currs.Add(Currencies.GetCurrencyByName("exalted"));
            }

            if (Customer.First().Currency.Name != "orb of alchemy")
            {
                main_currs.Add(Currencies.GetCurrencyByName("alchemy"));
            }

            if (Customer.First().Currency.Name == "exalted orb")
            {
                Win32.ChatCommand($"@{Customer.First().Nickname} exalted orb = {Currencies.GetCurrencyByName("exalted").ChaosEquivalent}");

                main_currs.Add(Currencies.GetCurrencyByName("exalted"));
            }

            Bitmap screen_shot = null;

            int x_trade = 220;
            int y_trade = 140;

            for (int i = 0; i < 30; i++)
            {
                double price = 0;

                foreach (Currency_ExRate cur in main_currs)
                {
                    Win32.MoveTo(0, 0);

                    Thread.Sleep(100);

                    screen_shot = ScreenCapture.CaptureRectangle(x_trade, y_trade, 465, 200);

                    found_positions = OpenCV_Service.FindCurrencies(screen_shot, cur.ImageName, 0.6);

                    foreach (Position pos in found_positions)
                    {
                        Win32.MoveTo(x_trade + pos.Left + pos.Width / 2, y_trade + pos.Top + pos.Height / 2);

                        Thread.Sleep(100);

                        string ctrlc = CtrlC_PoE();

                        var curbyname = Currencies.GetCurrencyByName(GetNameItem_PoE(ctrlc));

                        if (curbyname == null)

                            price += GetSizeInStack(CtrlC_PoE()) * cur.ChaosEquivalent;

                        else

                            price += GetSizeInStack(CtrlC_PoE()) * curbyname.ChaosEquivalent;


                        screen_shot.Dispose();
                    }

                    if (price >= Customer.First().Chaos_Price && price != 0)
                        break;
                }

                Console.WriteLine("Bid price (in chaos) = " + price + " Necessary (in chaos) = "+ Customer.First().Chaos_Price);

                if (price >= Customer.First().Chaos_Price)
                {
                    Console.WriteLine("I want accept trade");

                    screen_shot = ScreenCapture.CaptureRectangle(200, 575, 130, 40);

                    Position pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/accept_tradewindow.png");

                    if (pos.IsVisible)
                    {
                        Win32.MoveTo(210 + pos.Left + pos.Width / 2, 580 + pos.Top + pos.Height / 2);

                        Thread.Sleep(100);

                        Win32.DoMouseClick();

                        screen_shot.Dispose();

                        var timer = DateTime.Now + new TimeSpan(0, 0, 5);

                        while (Customer.First().TradeStatus != CustomerInfo.TradeStatuses.ACCEPTED)
                        {
                            if (Customer.First().TradeStatus == CustomerInfo.TradeStatuses.CANCELED)
                                return false;

                            if (DateTime.Now > timer)
                                break;
                        }

                        if (Customer.First().TradeStatus == CustomerInfo.TradeStatuses.ACCEPTED)
                            return true;

                        else continue;

                    }
                }
                else
                {
                    screen_shot.Dispose();
                }

                Thread.Sleep(500);
            }

            Win32.SendKeyInPoE("{ESC}");

            return false;
        }

        private void KickFormParty()
        {
            Win32.ChatCommand("/kick " + Customer.First().Nickname);
        }

        //for many items

        private void ScanTab(string name_tab = "trade_tab")
        {
            Position found_pos = null;

            Console.WriteLine($"Search {name_tab} trade tab...");

            for (int count_try = 0; count_try < 16; count_try++)
            {
                var screen_shot = ScreenCapture.CaptureRectangle(10, 90, 450, 30);

                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/notactive_" + name_tab + ".jpg");

                if (found_pos.IsVisible)
                {
                    break;
                }
                else
                {
                    found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/active_" + name_tab + ".jpg");
                    if (found_pos.IsVisible)
                    {
                        screen_shot.Dispose();

                        break;
                    }
                }
                screen_shot.Dispose();

                Thread.Sleep(500);
            }            

            if (found_pos.IsVisible)
            {
                Win32.MoveTo(10 + found_pos.Left + found_pos.Width / 2, 90 + found_pos.Top + found_pos.Height / 2);

                Thread.Sleep(200);

                Win32.DoMouseClick();

                Thread.Sleep(250);

                List<Cell> skip = new List<Cell>();

                for (int i = 0; i < 12; i++)
                {
                    for (int j = 0; j < 12; j++)
                    {
                        if (skip.Find(cel => cel.Left == i && cel.Top == j) != null)
                        {
                            continue;
                        }

                        Win32.MoveTo(0, 0);

                        Thread.Sleep(100);

                        Win32.MoveTo(Left_Stash64 + 38 * i, Top_Stash64 + 38 * j);

                        #region OpenCv

                        var screen_shot = ScreenCapture.CaptureRectangle(Left_Stash64 - 30 + 38 * i, Top_Stash64 - 30 + 38 * j, 60, 60);

                        Position pos = OpenCV_Service.FindObject(screen_shot, "Assets/UI_Fragments/empty_cel.png", 0.5);

                        if (!pos.IsVisible)
                        {
                            #region Good code

                            string item_info = CtrlC_PoE();

                            if (item_info != "empty_string")
                            {
                                var item = new Item
                                {
                                    Price = GetPrice_PoE(item_info),
                                    Name = GetNameItem_PoE_Pro(item_info),
                                    StackSize = GetStackSize_PoE_Pro(item_info)
                                };

                                item.Places.Add(new Cell(i, j));

                                if (item.StackSize == 1)
                                {
                                    item.SizeInStack = 1;
                                }
                                else
                                {
                                    item.SizeInStack = (int)GetSizeInStack(item_info);
                                }

                                if (item.Name.Contains("Resonator"))
                                {
                                    if (item.Name.Contains("Potent"))
                                    {
                                        item.Places.Add(new Cell(i, j + 1));
                                        skip.Add(new Cell(i, j + 1));

                                    }

                                    if (item.Name.Contains("Prime") || item.Name.Contains("Powerful"))
                                    {
                                        item.Places.Add(new Cell(i, j + 1));
                                        skip.Add(new Cell(i, j + 1));
                                        item.Places.Add(new Cell(i + 1, j + 1));
                                        skip.Add(new Cell(i + 1, j + 1));
                                        item.Places.Add(new Cell(i + 1, j));
                                        skip.Add(new Cell(i + 1, j));
                                    }
                                }

                                _Tab.AddItem(item);

                                #endregion
                            }

                            screen_shot.Dispose();

                            #endregion
                        }
                    }
                }

                Win32.SendKeyInPoE("{ESC}");

                Console.WriteLine("Scan is end!");
            }
            else
            {
                throw new Exception($"{name_tab} not found.");
            }
        }

        private bool TakeItems(string name_tab = "trade_tab")
        {
            Position found_pos = null;

            Console.WriteLine($"Search {name_tab} trade tab...");

            for (int count_try = 0; count_try < 16; count_try++)
            {
                var screen_shot = ScreenCapture.CaptureRectangle(10, 90, 450, 30);

                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/notactive_" + name_tab + ".jpg");

                if (found_pos.IsVisible)
                    break;
                else
                {
                    found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/active_" + name_tab + ".jpg");
                    if (found_pos.IsVisible)
                    {
                        screen_shot.Dispose();

                        break;
                    }
                }
                screen_shot.Dispose();

                Thread.Sleep(500);
            }

            if (found_pos.IsVisible)
            {
                Win32.MoveTo(10 + found_pos.Left + found_pos.Width / 2, 90 + found_pos.Top + found_pos.Height / 2);

                Thread.Sleep(200);

                Win32.DoMouseClick();

                Thread.Sleep(250);

                var customer = Customer.First();

                var min_price = new Price
                {
                    Cost = customer.Cost,
                    CurrencyType = customer.Currency,
                    ForNumberItems = customer.NumberProducts
                };

                var items = _Tab.GetItems(customer.NumberProducts, customer.Product, min_price);

                if (items.Any())
                {
                    int TotalAmount = 0;

                    foreach (Item i in items)
                    {
                        TotalAmount += i.SizeInStack;

                        Win32.MoveTo(Left_Stash64 + 38 * i.Places.First().Left, Top_Stash64 + 38 * i.Places.First().Top);

                        Thread.Sleep(100);

                        string item_info = CtrlC_PoE();

                        if (!item_info.Contains(i.Name))
                        {
                            Console.WriteLine("Information incorrect.");

                            return false;
                        }

                        if (TotalAmount > customer.NumberProducts)
                        {
                            TotalAmount -= i.SizeInStack;

                            int necessary = customer.NumberProducts - TotalAmount;

                            i.SizeInStack -= necessary;

                            _Tab.AddItem(i);

                            TotalAmount += necessary;

                            Win32.ShiftClick();

                            Thread.Sleep(100);

                            Win32.SendNumber_PoE(necessary);

                            Win32.SendKeyInPoE("{ENTER}");

                            PutInInventory();

                        }
                        else
                        {
                            Win32.CtrlMouseClick();
                        }


                        if (TotalAmount == customer.NumberProducts)
                        {
                            Win32.SendKeyInPoE("{ESC}");

                            return true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Items not found!");

                    Win32.ChatCommand($"@{customer.Nickname} maybe I sold it");
                }

            }

            Console.WriteLine("Tab not found");

            return false;
        }

        private void PutInInventory()
        {
            var screen_shot = ScreenCapture.CaptureRectangle(900, 420, 460, 200);

            var empty_poss = OpenCV_Service.FindObjects(screen_shot, "Assets/UI_Fragments/empty_cel.png", 0.5);

            if (empty_poss.Any())
            {
                foreach (Position pos in empty_poss)
                {
                    Win32.MoveTo(900 + pos.Left, 420 + pos.Top);

                    var info = CtrlC_PoE();

                    Thread.Sleep(100);

                    if (info == "empty_string")
                    {
                        Win32.DoMouseClick();

                        Thread.Sleep(150);

                        screen_shot.Dispose();

                        return;
                    }
                }
            }

            else
                Console.WriteLine("Inventory is full");
        }

        private bool PutItems()
        {
            int x_inventory = 925;
            int y_inventory = 440;
            int offset = 37;

            var customer = Customer.First();

            int TotalAmount = 0;

            List<Cell> skip = new List<Cell>();

            for (int j = 0; j < 12; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (skip.Find(cel => cel.Left == i && cel.Top == j) != null)
                    {
                        continue;
                    }

                    Win32.MoveTo(x_inventory + offset * j, y_inventory + 175);

                    Thread.Sleep(100);

                    var screen_shot = ScreenCapture.CaptureRectangle(x_inventory - 30 + offset * j, y_inventory - 30 + offset * i, 60, 60);

                    var pos = OpenCV_Service.FindObject(screen_shot, "Assets/UI_Fragments/empty_cel.png", 0.5);

                    if (!pos.IsVisible)
                    {
                        Win32.MoveTo(x_inventory + offset * j, y_inventory + offset * i);

                        var item_info = CtrlC_PoE();

                        string name = GetNameItem_PoE_Pro(item_info);

                        if (name != customer.Product)
                        {
                            continue;
                        }

                        int SizeInStack = GetStackSize_PoE_Pro(item_info);

                        TotalAmount += SizeInStack;

                        if (name.Contains("Resonator"))
                        {
                            if (name.Contains("Potent"))
                            {
                                skip.Add(new Cell(i, j + 1));

                            }

                            if (name.Contains("Prime") || name.Contains("Powerful"))
                            {
                                skip.Add(new Cell(i, j + 1));
                                skip.Add(new Cell(i + 1, j + 1));
                                skip.Add(new Cell(i + 1, j));
                            }
                        }

                        Win32.CtrlMouseClick();

                        Thread.Sleep(250);

                        if (TotalAmount >= customer.NumberProducts)
                        {
                            screen_shot.Dispose();

                            Console.WriteLine($"I put {TotalAmount} items in trade window");

                            return true;
                        }
                    }

                    screen_shot.Dispose();
                }
            }
            Win32.SendKeyInPoE("{ESC}");

            Win32.ChatCommand("@" + Customer.First().Nickname + " i sold it, sry");

            return false;
        }

        private void ClearInventory(string recycle_tab = "recycle_tab")
        {
            Position found_pos = null;

            Console.WriteLine($"Search {recycle_tab}...");

            Thread.Sleep(500);

            for (int count_try = 0; count_try < 16; count_try++)
            {
                var screen_shot = ScreenCapture.CaptureRectangle(10, 90, 450, 30);

                found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/notactive_" + recycle_tab + ".png");

                Thread.Sleep(1000);

                if (found_pos.IsVisible)
                {
                    break;
                }
                else
                {
                    found_pos = OpenCV_Service.FindObject(screen_shot, @"Assets/UI_Fragments/active_" + recycle_tab + ".png");
                    if (found_pos.IsVisible)
                    {
                        screen_shot.Dispose();

                        break;
                    }
                }
                screen_shot.Dispose();

                Thread.Sleep(500);
            }



            if (found_pos.IsVisible)
            {
                Win32.MoveTo(10 + found_pos.Left + found_pos.Width / 2, 90 + found_pos.Top + found_pos.Height / 2);

                Thread.Sleep(200);

                Win32.DoMouseClick();

                Thread.Sleep(250);


                int x_inventory = 925;
                int y_inventory = 440;
                int offset = 37;

                for (int j = 0; j < 12; j++)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Win32.MoveTo(x_inventory + offset * j, y_inventory + 175);

                        Thread.Sleep(100);

                        var screen_shot = ScreenCapture.CaptureRectangle(x_inventory - 30 + offset * j, y_inventory - 30 + offset * i, 60, 60);

                        Position pos = OpenCV_Service.FindObject(screen_shot, "Assets/UI_Fragments/empty_cel.png", 0.5);

                        if (!pos.IsVisible)
                        {
                            Win32.MoveTo(x_inventory + offset * j, y_inventory + offset * i);

                            Thread.Sleep(100);

                            string item_info = CtrlC_PoE();

                            if (item_info != "empty_string")
                            {
                                Win32.CtrlMouseClick();
                            }
                        }
                    }
                }

            }

            else
            {
                throw new Exception($"{recycle_tab} not found!");
            }
        }

        //util
        private double GetNumber(int begin, string target)
        {
            double result = 0;
            string buf = string.Empty;

            for (int i = begin; i < begin + 5; i++)
            {
                if (target[i] != ' ' && target[i] != ')')
                {
                    if (target[i] != '.')
                        buf += target[i];
                    else buf += ',';
                }
                else
                {
                    begin = i + 1;
                    break;
                }
            }

            return result = Convert.ToDouble(buf);
        }

        private int GetStackSize_PoE_Pro(string item_info)
        {
            if (!item_info.Contains("Stack Size:"))
                return 1;

            int res = Convert.ToInt32(Regex.Match(item_info, @"Stack Size: [0-9.]+/([0-9.]+)").Groups[1].Value);

            return res;
        }

        private string GetNameItem_PoE_Pro(string item_info)
        {
            if (item_info.Contains("Rarity: Currency"))
            {
                string str = Regex.Match(item_info, @"Rarity: Currency\s([\w ']+)").Groups[1].Value;
                return str;
            }

            if (item_info.Contains("Map Tier:"))
            {
                if (item_info.Contains("Rarity: Rare"))
                {
                    var match = Regex.Match(item_info, @"Rarity: Rare\s([\w ']*)\s([\w ']*)");

                    if (!string.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        return match.Groups[2].Value.Replace(" Map", "");
                    }
                    else
                        return match.Groups[1].Value.Replace(" Map", "");
                }

                if (item_info.Contains("Rarity: Normal"))
                {
                    return Regex.Match(item_info, @"Rarity: Normal\s([\w ']*)").Groups[1].Value.Replace(" Map", "");
                }

                if (item_info.Contains("Rarity: Unique"))
                {
                    var match = Regex.Match(item_info, @"Rarity: Unique\s([\w ']*)\s([\w ']*)");

                    if (!string.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        return $"{match.Groups[1].Value} {match.Groups[2].Value}".Replace(" Map", "");
                    }
                    else
                        return "Undefined item";
                }

            }

            if (item_info.Contains("Rarity: Divination Card"))
            {
                return Regex.Match(item_info, @"Rarity: Divination Card\s([\w ']*)").Groups[1].Value;
            }

            //I think that it for predicate fragments
            if (!item_info.Contains("Requirements:"))
            {
                if (item_info.Contains("Rarity: Normal"))
                {
                    return Regex.Match(item_info, @"Rarity: Normal\s([\w ']*)").Groups[1].Value;
                }
            }

            return "Not For Sell";

        }

        private string GetNameItem_PoE(string clip)
        {
            if (!String.IsNullOrEmpty(clip) && clip != "empty_string")
            {
                var lines = clip.Split('\n');

                if (lines.Count() == 1)
                    return null;

                if (!lines[2].Contains("---"))
                {
                    return lines[1].Replace("\r", "") + " " + lines[2].Replace("\r", "");
                }
                else
                    return lines[1].Replace("\r", "");

            }
            return null;
        }

        private bool IsValidPrice(string ctrlC_PoE)
        {
            bool isvalidprice = false;
            bool isvalidcurrency = false;

            if (!String.IsNullOrEmpty(ctrlC_PoE) && ctrlC_PoE != "empty_string")
            {
                var lines = ctrlC_PoE.Split('\n');

                foreach (string str in lines)
                {
                    if (str.Contains("Note: ~price"))
                    {
                        var result = Regex.Replace(str, "[^0-9.]", "");

                        double price = Convert.ToDouble(result);

                        if (price <= Customer.First().Cost)
                            isvalidprice = true;

                        int length = str.Length - 1;
                        int begin = 0;

                        for (int i = length; i > 0; i--)
                        {
                            if (str[i] == ' ')
                            {
                                begin = i + 1;
                                break;
                            }
                        }

                        result = str.Substring(begin, str.Length - begin).Replace("\r", "");

                        if (Currencies.GetCurrencyByName(result).Name == Customer.First().Currency.Name)
                        {
                            isvalidcurrency = true;
                        }


                        if (isvalidcurrency && isvalidprice)
                            return true;
                    }
                }
            }
            return false;
        }

        private double GetSizeInStack(string ctrlC_PoE)
        {
            if (!String.IsNullOrEmpty(ctrlC_PoE) && ctrlC_PoE != "empty_string")
            {
                int begin = ctrlC_PoE.IndexOf("Stack Size: ") + 12;
                int length = ctrlC_PoE.IndexOf("/") - begin;

                return Convert.ToDouble(ctrlC_PoE.Substring(begin, length));
            }
            return 0;
        }

        private string CtrlC_PoE()
        {
            Clipboard.Clear();

            string ss = null;

            Thread.Sleep(100);

            var time = DateTime.Now + new TimeSpan(0, 0, 1);

            while (ss == null)
            {
                Win32.SendKeyInPoE("^c");
                ss = Win32.GetText();

                if (time < DateTime.Now)
                    ss = "empty_string";
            }

            return ss.Replace("\r", "");
        }

        public Price GetPrice_PoE(string item_info)
        {
            Price price = new Price();

            if (!item_info.Contains("Note: ~price"))
                return new Price();

            if (Regex.IsMatch(item_info, "~price [0-9.]+/[0-9.]+"))
            {
                price.Cost = Convert.ToDouble(Regex.Replace(item_info, @"([\w\s\W\n]+Note: ~price )|(/+[\w\s\W]*)|([^0-9.])", ""));

                price.ForNumberItems = Convert.ToInt32(Regex.Replace(item_info, @"([\w\s\W]+/)|([^0-9.])", ""));

                price.CurrencyType = Currencies.GetCurrencyByName(Regex.Replace(item_info, @"[\w\s\W]+\d+\s|\n", ""));
            }
            if (Regex.IsMatch(item_info, @"~price +[0-9.]+\s\D*"))
            {
                price.Cost = Convert.ToDouble(Regex.Replace(item_info, @"[\w\W]*~price |[^0-9.]*", "").Replace('.', ','));

                price.ForNumberItems = GetStackSize_PoE_Pro(item_info);

                price.CurrencyType = Currencies.GetCurrencyByName(Regex.Replace(item_info, @"[\w\s\W]+\d+\s|\n", ""));
            }

            if (!price.IsSet)
                return new Price();
            else
                return price;
        }
    }
}