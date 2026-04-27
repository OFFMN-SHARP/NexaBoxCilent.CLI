using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration.Ini;

const string build = "3058";

Console.CancelKeyPress += CancelExit;
using HttpClient client = new();
var _username = string.Empty;
var _password = string.Empty;
if (args.Length != 3 || args[0] != "login")
{
    Console.WriteLine("Usage: login <username> <password>");
    return;
}
else if (args.Length == 3 && args[0] == "login")
{
    _username = args[1];
    _password = args[2];
}
else if (args.Length == 1 && File.Exists("login.cfg"))
{
    string[] loginFileLines = File.ReadAllLines("login.cfg");
    _username = loginFileLines[0];
    _password = loginFileLines[1];
}
else
{
    Console.WriteLine("No login credentials found. Please provide them using the 'login' command.");
    return;
}
//{"username": "your_name", "password": "your_password"}
var loginData = new
{
    username = _username,
    password = _password
};
string loginjson = JsonSerializer.Serialize(loginData);
var loginContent = new StringContent(loginjson, System.Text.Encoding.UTF8, "application/json");
HttpResponseMessage response = await client.PostAsync("https://accounts.nexabox.de/api/login", loginContent);
string loginResponse = await response.Content.ReadAsStringAsync();
if(!loginResponse.StartsWith("{") || !loginResponse.EndsWith("}")) {
    Console.WriteLine("Login failed. Please check your username and password.");
    return;
}
using JsonDocument doc = JsonDocument.Parse(loginResponse);
JsonElement root = doc.RootElement;
bool success = root.GetProperty("success").GetBoolean();
if (!success) { 
    Console.WriteLine("Login failed. Please check your username and password.");
    return;
}else Console.WriteLine("Login successful!");
client.DefaultRequestHeaders.Add("Authorization", "Bearer " + root.GetProperty("token").GetString());
HttpResponseMessage userInfoResponse = await client.GetAsync("https://accounts.nexabox.de/api/me");
if (!userInfoResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Failed to retrieve user information.");
    return;
}
string userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
string Permissions = root.GetProperty("permissions").ToString();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("Permissions Info: " + Permissions);
Console.WriteLine(new string('=',20));
Console.WriteLine("Welcome back, Dr." + root.GetProperty("username").GetString());
Console.ResetColor();
while (true)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Nexabox CLI @[{root.GetProperty("username").GetString()}|Build-{build}] <{Environment.CurrentDirectory}>");
    Console.Write("└──>");
    Console.ResetColor();
    string Command = Console.ReadLine() ?? "@Signal_UserInput=NULL";
    try
    {
        await CommandParserAsync(Command);
    }catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("An error occurred while processing the command: " + ex.Message);
        Console.ResetColor();
    }
    Console.WriteLine();
}

async Task<string?> CommandParserAsync(string Command)
{
    Dictionary<string, string> commands = new()
    {
        {"@Signal_UserInput=NULL", "" },
        {"where", ""},
        {"whereis", ""},
        {"grep", ""},
        {"show", ""},
        {"fetch", ""},
        {"sed", "to"},
        {"create", "to"},
        {"from", "to"},
        {"download", "to"},
        {"syscall", ""},
        {"call", ""},
        {"whoami", ""},
        {"i?", ""},
        {"help", ""}
    };
    if (commands.TryGetValue(Command.Split(" ")[0], out string value))
    {
        string[] Cmd = Command.Split(" ");
        if (!string.IsNullOrEmpty(value) && Cmd.Length < 3) throw new Exception($"The command '{Cmd[0]}' requires an additional argument: '{value}'.");
        string Value1 = Cmd.Length > 1 ? Cmd[1] : String.Empty;
        string Value2 = Cmd.Length >= 4 ? Cmd[3] : "";
        if (!string.IsNullOrEmpty(value) && Value2 != value) throw new Exception($"Invalid argument for the command '{Cmd[0]}'. Expected argument: '{value}'.");
        try
        {
            switch (Cmd[0])
            {
                case "where":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await FileSearch(Value1);
                    break;
                case "whereis":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await FileSearch(Value1);
                    break;
                case "grep":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await FileSearch(Value1);
                    break;
                case "show":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    break;
                case "fetch":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    break;
                case "sed":
                    if(Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    break;
                case "create":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    break;
                case "from":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    break;
                case "download":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    break;
                case "syscall":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    break;
                case "call":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    break;
                case "whoami":
                    if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                    await UserInfo();
                    break;
                case "i?":
                    if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                    await UserInfo();
                    break;
                case "help":
                    if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                    break;
                case "exit":
                    if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                    await Exit();
                    break;
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
    else throw new Exception("Unknown command. Type 'help' for a list of available commands.");
}

async Task FileSearch(string search)
{
    HttpResponseMessage whereResponse = await client.GetAsync("https://drive.nexabox.de/api/files");
    string whereContent = await whereResponse.Content.ReadAsStringAsync();
    JsonDocument WhereDoc = JsonDocument.Parse(whereContent);
    List<JsonElement> files = new List<JsonElement>();
    Console.WriteLine("Searching from drive files: ");
    foreach (JsonElement file in WhereDoc.RootElement.EnumerateArray())
    {
        Console.WriteLine(file.GetProperty("filename").GetString());
        if (!string.IsNullOrEmpty(file.GetProperty("filename").GetString()))
        {
            if (!search.StartsWith('*'))
            {
                if (file.GetProperty("filename").GetString() == search)
                {
                    files.Add(file);
                }
            }
            else
            {
                if (file.GetProperty("filename").GetString().EndsWith(search.TrimStart('*')))
                {
                    files.Add(file);
                }
            }
        }
    }
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine("Search completed.");
    foreach (JsonElement file in files)
    {
        Console.WriteLine("----------------------------");
        Console.WriteLine("File ID: " + file.GetProperty("id").GetString());
        Console.WriteLine("File Name: " + file.GetProperty("filename").GetString());
        Console.WriteLine("File Size: " + file.GetProperty("size").GetInt64() + " bytes");
        Console.WriteLine("File Chunks:" + string.Join(", ", file.GetProperty("chunks").EnumerateArray().Select(c => c.GetString())));
    }
}


async Task UserInfo()
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    HttpResponseMessage userInfoResponse = await client.GetAsync("https://accounts.nexabox.de/api/me");
    //{"username":"offmn-user","password":"13141516msn","isInitialPassword":false,"permissions":["NexaboxDrive"],"lastLogin":1777299347623}
    string userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
    JsonDocument userInfoDoc = JsonDocument.Parse(userInfoContent);
    JsonElement userInfoRoot = userInfoDoc.RootElement;
    Console.WriteLine("User Information:");
    Console.WriteLine(new string('=', 20));
    Console.WriteLine("Username: " + userInfoRoot.GetProperty("username").GetString());
    Console.WriteLine("Permissions: " + string.Join(", ", userInfoRoot.GetProperty("permissions").EnumerateArray().Select(p => p.GetString())));
    //Console.WriteLine("Last Login: " + userInfoRoot.GetProperty("lastLogin").GetInt64());
    DateTime lastLogin = DateTimeOffset.FromUnixTimeMilliseconds(userInfoRoot.GetProperty("lastLogin").GetInt64()).DateTime;
    Console.WriteLine("Last Login: " + lastLogin.ToString("yyyy-MM-dd HH:mm:ss"));
    Console.WriteLine(new string('=', 20));
    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    Console.ResetColor();
}




async Task Exit()
{
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Exiting...");
    Environment.Exit(0);
}



void CancelExit(object sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Exiting...");
    Environment.Exit(0);
}

