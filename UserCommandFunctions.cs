using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexaBox.CLI
{
    public static class UserCommandFunctions
    {
        public static async Task FileSearch(string search)
        {
            HttpResponseMessage whereResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/files");
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
                Console.Out.Flush();
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
        public static async Task FileInfo(string fileId)
        {
            HttpResponseMessage whereResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/files");
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
        public static async Task UserInfo()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            HttpResponseMessage userInfoResponse = await Program.Client.GetAsync("https://accounts.nexabox.de/api/me");
            string userInfoContent = await userInfoResponse.Content.ReadAsStringAsync();
            JsonDocument userInfoDoc = JsonDocument.Parse(userInfoContent);
            JsonElement userInfoRoot = userInfoDoc.RootElement;
            Console.WriteLine("User Information:");
            Console.WriteLine(new string('=', 20));
            Console.WriteLine("Username: " + userInfoRoot.GetProperty("username").GetString());
            Console.WriteLine("Permissions: " + string.Join(", ", userInfoRoot.GetProperty("permissions").EnumerateArray().Select(p => p.GetString())));
            DateTime lastLogin = DateTimeOffset.FromUnixTimeMilliseconds(userInfoRoot.GetProperty("lastLogin").GetInt64()).DateTime;
            Console.WriteLine("Last Login: " + lastLogin.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine(new string('=', 20));
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.ResetColor();
        }
        public static async Task SendMessage(string username, string message)
        {
            var messageData = new
            {
                toUser = username,
                content = message
            };
            string messagejson = JsonSerializer.Serialize(messageData);
            var messageContent = new StringContent(messagejson, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage Send = await Program.Client.PostAsync("https://accounts.nexabox.de/api/messages", messageContent);
            if (Send.IsSuccessStatusCode) Console.WriteLine("Message sent successfully.");
            else Console.WriteLine("Failed to send message.");
        }
        public static async Task ChangeUserPassword()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Please enter your new password: ");
                string newPassword = Console.ReadLine() ?? "";
                if (!string.IsNullOrEmpty(newPassword))
                {

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Please confirm your new password: ");
                    string confirmPassword = Console.ReadLine() ?? "";
                    Console.ResetColor();
                    if (newPassword == confirmPassword)
                    {
                        var changePasswordData = new
                        {
                            newPassword = newPassword
                        };
                        string changePasswordjson = JsonSerializer.Serialize(changePasswordData);
                        var changePasswordContent = new StringContent(changePasswordjson, System.Text.Encoding.UTF8, "application/json");
                        HttpResponseMessage changePasswordResponse = await Program.Client.PostAsync("https://accounts.nexabox.de/api/change-password", changePasswordContent);
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
        public static async Task CallSystemCommand(string command)
        {
            string headle;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                headle = "cmd /c";
            }
            else headle = "/bin/bash -c";
            var proc = new Process();
            proc.StartInfo.FileName = headle;
            proc.StartInfo.Arguments = "\"" + command + "\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Console.WriteLine(output);
            if (!string.IsNullOrEmpty(error)) Console.WriteLine(error);
        }
        public static async Task Help()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Nexabox CLI - Command Reference");
            Console.WriteLine(new string('=', 40));
            Console.ResetColor();

            Console.WriteLine("  login <username> <password>    登录到 Nexabox");
            Console.WriteLine("  exit                            退出 CLI");
            Console.WriteLine();
            Console.WriteLine("  File Operations:");
            Console.WriteLine("  where <file/folder>             搜索文件 (别名: whereis, grep)");
            Console.WriteLine("  show <file>                     展示文件属性 (别名: fetch)");
            Console.WriteLine("  sed <file>                      创建并编辑文件 (别名: create)");
            Console.WriteLine("  from <file> to <path>           下载文件 (别名: download-to)");
            Console.WriteLine("  push <file>                     上传文件 (别名: pushto)");
            Console.WriteLine();
            Console.WriteLine("  Messaging:");
            Console.WriteLine("  msg <text> to <user>            发送消息 (别名: send, write, msgo)");
            Console.WriteLine();
            Console.WriteLine("  User & Admin:");
            Console.WriteLine("  whoami                          显示当前用户 (别名: i?)");
            Console.WriteLine("  passwd                          修改密码");
            Console.WriteLine("  mkuser <name> with <pass>       创建用户 (别名: invit)");
            Console.WriteLine("  chprmis <name> into <perms>     修改用户权限");
            Console.WriteLine("  rmuser <name>                   删除用户");
            Console.WriteLine();
            Console.WriteLine("  System:");
            Console.WriteLine("  syscall <cmd>                   执行本地命令");
            Console.WriteLine("  call <cmd>                      执行工作台指令");
            Console.WriteLine("  help                            显示此帮助");
        }
    }
}
