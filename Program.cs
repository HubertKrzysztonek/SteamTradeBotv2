using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SteamTradeBotv2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Steam credentials
            string username = "your_username";
            string password = "your_password";
            string sharedSecret = "your_shared_secret";

            // Log in to Steam
            SteamWebAPI steamWebAPI = new SteamWebAPI(username, password, sharedSecret);
            bool isLoggedIn = await steamWebAPI.Login();

            if (isLoggedIn)
            {
                Console.WriteLine("Successfully logged in to Steam.");

                // Read Steam inventory
                List<string> itemIds = await steamWebAPI.GetInventoryItemIds();
                Console.WriteLine($"Found {itemIds.Count} items in your inventory.");

                // Accept trade offers
                await steamWebAPI.AcceptTradeOffers();

                Console.WriteLine("Bot finished its tasks.");
            }
            else
            {
                Console.WriteLine("Failed to log in to Steam. Please check your credentials.");
            }
        }
    }
}