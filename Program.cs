using Microsoft.Extensions.Configuration.Ini;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

const string build = "3058";

Console.CancelKeyPress += CancelExit;
using HttpClient client = new();
var _username = string.Empty;
var _password = string.Empty;
if (args.Length != 3&&!File.Exists("login.cfg") || args[0] != "login"&&!File.Exists("login.cfg"))
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
bool isInitialPassword = root.GetProperty("isInitial").GetBoolean();
if (isInitialPassword)
{
    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Please enter a new password: ");
        string newPassword = Console.ReadLine() ?? "";
        if (string.IsNullOrEmpty(newPassword))
        {
            continue;
        }
        Console.ResetColor();
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Please confirm the new password: ");
            string confirmPassword = Console.ReadLine() ?? "";
            if (newPassword != confirmPassword)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Passwords do not match. Please try again.");
                Console.ResetColor();
                continue;
            }else break;
        }
        _password = newPassword;
        break;
    }
    HttpResponseMessage ChangePassword = await client.PostAsync("https://accounts.nexabox.de/api/change-password", new StringContent(JsonSerializer.Serialize(new { newPassword = _password }), System.Text.Encoding.UTF8, "application/json"));
}
Console.ResetColor();
if(File.Exists("login.cfg"))File.Delete("login.cfg");
using(StreamWriter sw = new StreamWriter("login.cfg"))
{
    sw.WriteLine(_username);
    sw.WriteLine(_password);
}
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
    Dictionary<string, string> commands = new() { 
    // 空输入信号
        {"@Signal_UserInput=NULL", ""},

    // 搜索文件
        {"where", ""},
        {"whereis", ""},
        {"grep", ""},

    // 展示属性
        {"show", ""},
        {"fetch", ""},

    // 创建并编辑传送
        {"sed", ""},
        {"create", ""},

    // 下载到本地
        {"from", "to"},
        {"download-to", "to"},

    // 消息发送
        {"msg", "to"},
        {"send", "to"},
        {"write", "to"},
        {"msgo", "to"},

    // 密码修改
        {"passwd", ""},

    // 管理员：创建用户
        {"mkuser", "with"},
        {"invit", "with"},

    // 管理员：修改权限
        {"chprmis", "into"},

    // 管理员：删除用户
        {"rmuser", ""},

    // 上传文件
        {"push", ""},
        {"pushto", ""},

    // 本地系统命令
        {"syscall", ""},

    // 工作台内部指令
        {"call", ""},

    // 身份显示
        {"whoami", ""},
        {"i?", ""},

    // 帮助与退出
        {"help", ""},
        {"exit", ""}
    };
    if (commands.TryGetValue(Command.Split(" ")[0], out string value))
    {
        string[] Cmd = Command.Split(" ");
        if (!string.IsNullOrEmpty(value) && Cmd.Length < 3) throw new Exception($"The command '{Cmd[0]}' requires an additional argument: '{value}'.");
        string Value1 = Cmd.Length > 1 ? Cmd[1] : String.Empty;
        string CheckValue1 = Cmd.Length >= 4 ? Cmd[2] : "";
        string Value2 = Cmd.Length >= 4 ? Cmd[3] : "";
        if (!string.IsNullOrEmpty(value) && CheckValue1 != value) throw new Exception($"Invalid argument for the command '{Cmd[0]}'. Expected argument: '{value}'.");
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
                    await FileInfo(Value1);
                    break;
                case "fetch":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await FileInfo(Value1);
                    break;
                case "sed":
                    if(Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await Edit(Value1);
                    break;
                case "create":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await Edit(Value1);
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
                case "send":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await SendMessage(Value2,Value1);
                    break;
                case "write":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await SendMessage(Value2, Value1);
                    break;
                case "msgo":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await SendMessage(Value2, Value1);
                    break;
                case "msg":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await SendMessage(Value2, Value1);
                    break;
                case "passwd":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await ChangeUserPassword();
                    break;
                case "mkuser":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await NewUser(Value1, Value2);
                    break;
                case "invit":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await NewUser(Value1, Value2);
                    break;
                case "chprmis":
                    if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                    await ChangeUserPermissions(Value1, Value2);
                    break;
                case "rmuser":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    await DeleteUser(Value1);
                    break;
                case "push":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                    break;
                case "pushto":
                    if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 arguments.");
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

async Task FileInfo(string fileId)
{
    HttpResponseMessage whereResponse = await client.GetAsync("https://drive.nexabox.de/api/files");
    string whereContent = await whereResponse.Content.ReadAsStringAsync();
    JsonDocument WhereDoc = JsonDocument.Parse(whereContent);
    List<JsonElement> files = new List<JsonElement>();
    Console.WriteLine("Searching from drive files: ");
    foreach (JsonElement file in WhereDoc.RootElement.EnumerateArray())
    {
        if (!string.IsNullOrEmpty(file.GetProperty("filename").GetString()))
        {
            if (file.GetProperty("id").GetString() == fileId)
            {
                files.Add(file);
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

async Task SendMessage(string username, string message)
{
    var messageData = new
    {
        toUser = username,
        content = message
    };
    string messagejson = JsonSerializer.Serialize(messageData);
    var messageContent = new StringContent(messagejson, System.Text.Encoding.UTF8, "application/json");
    HttpResponseMessage Send= await client.PostAsync("https://accounts.nexabox.de/api/messages",messageContent);
    if (Send.IsSuccessStatusCode)Console.WriteLine("Message sent successfully.");
    else Console.WriteLine("Failed to send message.");
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



async Task ChangeUserPassword()
{
    while (true)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Please enter your new password: ");
        string newPassword = Console.ReadLine() ?? "";
        if (!string.IsNullOrEmpty(newPassword))
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Please confirm your new password: ");
                string confirmPassword = Console.ReadLine() ?? "";
                Console.ResetColor();
                if (newPassword == confirmPassword)
                {
                    //{"newPassword": "my_new_password_123"}
                    var changePasswordData = new
                    {
                        newPassword = newPassword
                    };
                    string changePasswordjson = JsonSerializer.Serialize(changePasswordData);
                    var changePasswordContent = new StringContent(changePasswordjson, System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage changePasswordResponse = await client.PostAsync("https://accounts.nexabox.de/api/change-password", changePasswordContent);
                    if (changePasswordResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Password changed successfully.");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Failed to change password.");
                    }
                }
            }
        }
    }
}


async Task NewUser(string username, string password)
{//{"username": "new_guy", "initialPassword": "123456"}
    string newUserjson = JsonSerializer.Serialize(new { username = username, initialPassword = password });
    var newUserContent = new StringContent(newUserjson, System.Text.Encoding.UTF8, "application/json");
    HttpResponseMessage newUserResponse = await client.PostAsync("https://accounts.nexabox.de/api/admin/users", newUserContent);
    if (newUserResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("User created successfully.");
    }
    else
    {
        Console.WriteLine("Failed to create user.");
    }
}


async Task ChangeUserPermissions(string username, string permission)
{//{"username": "new_guy", "permissions": ["DataBoard", "Wiki"]}
    ///api/admin/users/permissions
    string[] group=permission.Split('@');
    string changePermissionsjson = JsonSerializer.Serialize(new { username = username, permissions = group });
    var changePermissionsContent = new StringContent(changePermissionsjson, System.Text.Encoding.UTF8, "application/json");
    HttpResponseMessage changePermissionsResponse = await client.PutAsync("https://accounts.nexabox.de/api/admin/users/permissions", changePermissionsContent);
    if (changePermissionsResponse.IsSuccessStatusCode)
    {
        Console.WriteLine("User permissions changed successfully.");
    }else Console.WriteLine("Failed to change user permissions.");
}


async Task DeleteUser(string username)
{
    var deleteData = new { username = username };
    string json = JsonSerializer.Serialize(deleteData);
    var request = new HttpRequestMessage(HttpMethod.Delete, "https://accounts.nexabox.de/api/admin/users")
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
    HttpResponseMessage response = await client.SendAsync(request);
    if (response.IsSuccessStatusCode)
        Console.WriteLine($"User '{username}' deleted successfully.");
    else
        Console.WriteLine($"Failed to delete user '{username}'.");
}




















































































async Task Edit(string FileName)
{
    Console.WriteLine("Message: Edit Mode (Write)");
    Console.WriteLine("Message: Type '/help' for help.");
    Console.WriteLine("Message: Type '/exit' to exit.");
    StringBuilder Text_Temp = new StringBuilder();
    int Up_LineCount = 0;//Up_LineCount is Update Line Count, which means the line number of the current input line.
    while (true)
    {
        Up_LineCount++;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"[{Up_LineCount}]>>");
        Console.ResetColor();
        string? Input = Console.ReadLine() ?? "@[Editor_NULLinput]_Replace_NULLinput_[SrcInfoOfEditor]";
        if (Input.ToLower() == "/help")
        {
            Console.WriteLine("Message: Edit Mode (Write) Help");
            Console.WriteLine("Message: Type '/help' for help.");
            Console.WriteLine("Message: Type '/exit' to exit.");
            Console.WriteLine("Message: Type '/clear' to clear the text.");
            Console.WriteLine("Tip: '[Up_LineCount]>>' means the current line number.");
        }
        else if (Input.ToLower() == "/exit")
        {
            Console.WriteLine("Message: Starting to save the text.");
            Console.WriteLine("Step1:Select the save mode.");
            Console.WriteLine("1:Save to file.");
            Console.WriteLine("2:Send  text to someone.");
            Console.WriteLine("3:Cancel.");
            Console.Write("Input the number:");
            int Save_Mode;
            try
            {
                Save_Mode = int.Parse(Console.ReadLine() ?? "2");
            }
            catch
            {
                Console.WriteLine("Message: Invalid input. Defaulting to output to console.");
                Save_Mode = 2;
            }
            switch (Save_Mode)
            {
                case 1:
                    Console.Write("Input the file name:");
                    string Save_File_Name = Console.ReadLine() ?? "null";
                    File.WriteAllText(Save_File_Name, Text_Temp.ToString());
                    Console.WriteLine("Message: Text saved to file.");
                    break;
                case 2:
                    Console.Write("Input the username:");
                    string To_User = Console.ReadLine() ?? "null";
                    await SendMessage(To_User, Text_Temp.ToString());
                    break;
                case 3:
                    Console.WriteLine("Message: Cancelled.");
                    return;
            }
            return;
        }
        else if (Input.ToLower() == "/clear")
        {
            Text_Temp.Clear();
            Console.WriteLine("Message: Text cleared.");
        }
        else if (Input.StartsWith("@[Editor_NULLinput]_Replace_NULLinput_[SrcInfoOfEditor]"))
        {
            Text_Temp.AppendLine(null);
        }
        else
        {
            Text_Temp.AppendLine(Input);
        }
    }
}





