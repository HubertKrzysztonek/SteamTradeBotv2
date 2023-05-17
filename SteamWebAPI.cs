using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamTradeBotv2
{
    public class SteamWebAPI
    {
        private string username;
        private string password;
        private string sharedSecret;
        private string sessionId;
        private HttpClient httpClient;

        public SteamWebAPI(string username, string password, string sharedSecret)
        {
            this.username = username;
            this.password = password;
            this.sharedSecret = sharedSecret;
            this.sessionId = Guid.NewGuid().ToString();
            this.httpClient = new HttpClient();
        }

        public async Task<bool> Login()
        {
            // Step 1: Get Steam login page
            HttpResponseMessage response = await httpClient.GetAsync("https://steamcommunity.com/login");
            response.EnsureSuccessStatusCode();
            string loginPageContent = await response.Content.ReadAsStringAsync();

            // Step 2: Extract login parameters
            string loginKey = SteamHelper.GetLoginKey(loginPageContent);
            string rsaTimestamp = SteamHelper.GetRsaTimestamp(loginPageContent);
            string rsaModulus = SteamHelper.GetRsaModulus(loginPageContent);
            string rsaExponent = SteamHelper.GetRsaExponent(loginPageContent);

            if (loginKey == null || rsaTimestamp == null || rsaModulus == null || rsaExponent == null)
            {
                return false;
            }

            // Step 3: Generate encrypted login details
            string encryptedPassword = SteamHelper.EncryptPassword(password, rsaModulus, rsaExponent);
            string encryptedTwoFactorCode = SteamHelper.GenerateTwoFactorCode(sharedSecret, rsaTimestamp);

            // Step 4: Send login request
            response = await httpClient.PostAsync("https://steamcommunity.com/login/dologin/",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", encryptedPassword },
                    { "twofactorcode", encryptedTwoFactorCode },
                    { "loginkey", loginKey },
                    { "rsatimestamp", rsaTimestamp },
                    { "remember_login", "false" },
                    { "captchagid", "-1" },
                    { "captcha_text", "" },
                    { "emailauth", "" },
                    { "emailsteamid", "" },
                    { "rsatimestamp", rsaTimestamp },
                    { "donotcache", DateTime.UtcNow.Ticks.ToString() }
                }));

            response.EnsureSuccessStatusCode();
            string loginResponse = await response.Content.ReadAsStringAsync();

            // Step 5: Check login response
            if (loginResponse.Contains("success\":true"))
            {
                // Step 6: Set session cookies
                IEnumerable<string> sessionCookies = response.Headers.GetValues("Set-Cookie");
                foreach (string cookie in sessionCookies)
                {
                    if (cookie.StartsWith("sessionid"))
                    {
                        sessionId = SteamHelper.ExtractSessionId(cookie);
                        break;
                    }
                }

                // Step 7: Verify login session
                response = await httpClient.GetAsync($"https://steamcommunity.com/?sessionid={sessionId}");
                response.EnsureSuccessStatusCode();
                string sessionVerificationResponse = await response.Content.ReadAsStringAsync();

                if (sessionVerificationResponse.Contains("var g_sessionID = \""))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<List<string>> GetInventoryItemIds()
        {
            List<string> itemIds = new List<string>();

            // Step 1: Get user inventory
            HttpResponseMessage response = await httpClient.GetAsync($"https://steamcommunity.com/inventory/{username}/753/6?l=english&count=5000");
            response.EnsureSuccessStatusCode();
            string inventoryResponse = await response.Content.ReadAsStringAsync();

            // Step 2: Extract item IDs
            itemIds = SteamHelper.ExtractInventoryItemIds(inventoryResponse);

            return itemIds;
        }

        public async Task AcceptTradeOffers()
        {
            // Step 1: Get trade offers
            HttpResponseMessage response = await httpClient.GetAsync("https://steamcommunity.com/tradeoffer/new/");
            response.EnsureSuccessStatusCode();
            string tradeOffersResponse = await response.Content.ReadAsStringAsync();

            // Step 2: Extract trade offer IDs
            List<string> tradeOfferIds = SteamHelper.ExtractTradeOfferIds(tradeOffersResponse);

            // Step 3: Accept trade offers where only receiving items
            foreach (string tradeOfferId in tradeOfferIds)
            {
                response = await httpClient.PostAsync($"https://steamcommunity.com/tradeoffer/{tradeOfferId}/accept",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "sessionid", sessionId },
                        { "serverid", "1" },
                        { "tradeofferid", tradeOfferId },
                        { "partner", "" },
                        { "captcha", "" },
                        { "trade_offer_create_params", "" }
                    }));

                response.EnsureSuccessStatusCode();
                string acceptTradeResponse = await response.Content.ReadAsStringAsync();

                if (acceptTradeResponse.Contains("success\":true"))
                {
                    Console.WriteLine($"Accepted trade offer {tradeOfferId}.");
                }
                else
                {
                    Console.WriteLine($"Failed to accept trade offer {tradeOfferId}.");
                }
            }
        }
    }
}
