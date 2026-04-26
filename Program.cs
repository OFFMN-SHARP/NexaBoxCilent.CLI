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
        CommandParser(Command);
    }catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("An error occurred while processing the command: " + ex.Message);
        Console.ResetColor();
    }
    Console.WriteLine();
}

string? CommandParser(string Command)
{
    Dictionary<string, string> commands = new()
    {
        {"where", ""},
        {"whereis", ""},
        {"grep", ""},
        {"show", ""},
        {"fetch", ""},
        {"sed", "to"},
        {"create_to", "to"},
        {"from", "to"},
        {"download-to", "to"},
        {"syscall", ""},
        {"call", ""},
        {"whoami", ""},
        {"i?", ""},
        {"help", ""}
    };
    if(commands.TryGetValue(Command.Split(" ")[0], out string value))
    {
        string[] Cmd = Command.Split(" ");
        if (!string.IsNullOrEmpty(value) && Cmd.Length<3)throw new Exception($"The command '{Cmd[0]}' requires an additional argument: '{value}'.");
        if(Cmd.Length > 2) throw new Exception($"Too many arguments provided for the command '{Cmd[0]}'. Expected format: '{Cmd[0]} {value}'.");
        string Value1 = Cmd[1];
        string Value2 = Cmd.Length >=4 ? Cmd[3] : "";
    }
    else throw new Exception("Unknown command. Type 'help' for a list of available commands.");
}

void CancelExit(object sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Exiting...");
    Environment.Exit(0);
}

