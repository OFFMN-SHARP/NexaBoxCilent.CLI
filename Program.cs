using System.ComponentModel.Design;
using System.IO;
using System.Text;
using System.Text.Json;

namespace NexaBox.CLI
{
    public static class Program
    {
        public static HttpClient Client = new HttpClient();
        public static class Methods
        {
            public static class ApplicationName {
                public static string Name = "NexaBox.CLI";
                public static string CodeName = "Republish - Origin";
                public static string FullName = "NexaBox with Workstation Command Line Interface";
            }
            public static string Version = "1.0.0";
            public static string Build = "4065";
        }
        public static class UserConfig
        {
            public static string Username = "";
            public static string Password = "";
            public static string Token = "";
        }
        public static async Task Main(string[] args)
        {
            Console.CancelKeyPress += CancelExit;
            if(args.Length == 0)args = args.Append("null").ToArray();
            if (args.Length != 3 && !File.Exists("login.cfg") || args[0] != "login" && !File.Exists("login.cfg"))
            {
                Console.WriteLine("Usage: login <username> <password>");
                Console.WriteLine("Input your username:");
                UserConfig.Username = Console.ReadLine() ?? "";
                Console.WriteLine("Input your password:");
                UserConfig.Password = Console.ReadLine() ?? "";
            }
            else if (args.Length == 3 && args[0] == "login")
            {
                UserConfig.Username = args[1];
                UserConfig.Password = args[2];
            }
            else if (args.Length == 1 && File.Exists("login.cfg"))
            {
                string[] loginFileLines = File.ReadAllLines("login.cfg");
                UserConfig.Username = loginFileLines[0];
                UserConfig.Password = loginFileLines[1];
            }
            else
            {
                Console.WriteLine("No login credentials found. Please provide them using the 'login' command.");
                Console.WriteLine("Usage: login <username> <password>");
                Console.WriteLine("Input your username:");
                UserConfig.Username = Console.ReadLine() ?? "";
                Console.WriteLine("Input your password:");
                UserConfig.Password = Console.ReadLine() ?? "";
            }
            var LoginData = new
            {
                username = UserConfig.Username,
                password = UserConfig.Password
            };
            string LoginRequestJson = JsonSerializer.Serialize(LoginData);
            var LoginContent = new StringContent(LoginRequestJson, Encoding.UTF8, "application/json");
            var LoginResponse = await Client.PostAsync("https://accounts.nexabox.de/api/login", LoginContent);
            string LoginResponseJson = await LoginResponse.Content.ReadAsStringAsync();
            if(!JsonDocument.Parse(LoginResponseJson).RootElement.GetProperty("success").GetBoolean()){
                Console.WriteLine("Invalid login credentials.");
                await Exit();
            }
            if (!LoginResponseJson.StartsWith('{')&&!LoginResponseJson.EndsWith('}')){
                Console.WriteLine("Invalid login credentials.");
                await Exit();
            }else Console.WriteLine("Login successful!");
            var LoginRoot = JsonDocument.Parse(LoginResponseJson);
            Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + LoginRoot.RootElement.GetProperty("token").GetString());
            UserConfig.Token = LoginRoot.RootElement.GetProperty("token").GetString() ?? "";
            HttpResponseMessage UserInfoGet = await Client.GetAsync("https://accounts.nexabox.de/api/me");
            if (!UserInfoGet.IsSuccessStatusCode)
            {
                Console.WriteLine("Error: " + UserInfoGet.StatusCode);
                await Exit();
            }
            string UserInfoJson = await UserInfoGet.Content.ReadAsStringAsync();
            var UserInfoRoot = JsonDocument.Parse(UserInfoJson);
            var UserInfoData = UserInfoRoot.RootElement;
            string Permissions = UserInfoData.GetProperty("permissions").ToString();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Permissions Info: " + Permissions);
            Console.WriteLine(new string('=', 20));
            Console.WriteLine("Welcome back, Dr." + LoginRoot.RootElement.GetProperty("username").GetString());
            var LoginElementData = LoginRoot.RootElement;
            bool IsInitialLogin = LoginElementData.GetProperty("isInitial").GetBoolean();
            if (IsInitialLogin)
            {
                Console.WriteLine("It looks like this is your first time logging in. Please change your password.");
                while (true)
                {
                    Console.WriteLine("Input your new password:");
                    string NewPassword = Console.ReadLine() ?? "";
                    Console.WriteLine("Verify your new password:");
                    string NewPasswordVerify = Console.ReadLine() ?? "";
                    if (NewPassword != NewPasswordVerify)
                    {
                        Console.WriteLine("Passwords do not match. Please try again.");
                        continue;
                    }else break;
                }
                HttpResponseMessage ChangePassword = await Client.PostAsync("https://accounts.nexabox.de/api/change-password", new StringContent(JsonSerializer.Serialize(new { newPassword = UserConfig.Password }), System.Text.Encoding.UTF8, "application/json"));
                if (!ChangePassword.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error: " + ChangePassword.StatusCode);
                    await Exit();
                }
                Console.WriteLine("Password changed successfully! Please log in again.");
                await Exit();
            }
            Console.ResetColor();
            if (File.Exists("login.cfg")) File.Delete("login.cfg");
            using (StreamWriter sw = new StreamWriter("login.cfg"))
            {
                sw.WriteLine(UserConfig.Username);
                sw.WriteLine(UserConfig.Password);
            }
            await CommandParser.MainLoop();
        }
        public static async void CancelExit(object sender, ConsoleCancelEventArgs e) { e.Cancel = true; await Exit(); }
        public static async Task Exit()
        {
            async Task Exit()
            {
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            }
            await Exit();
        }
    }
}
