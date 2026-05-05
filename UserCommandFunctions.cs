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
        public static async Task CopyFile(string source, string destination)
        {
            string FullSource = Path.GetFullPath(source);
            string FullDestination = Path.GetFullPath(destination);
            File.Copy(FullSource, FullDestination);
        }
        public static async Task ChangeDir(string path)
        {
            string FullPath = Path.GetFullPath(path);
            Directory.SetCurrentDirectory(FullPath);
        }
        public static async Task FileSearch(string search)
        {
            bool RequestFailed = false;
            Console.WriteLine("Searching for: " + search);
            HttpResponseMessage searchResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/search?keyword=" + search);
            string searchContent = await searchResponse.Content.ReadAsStringAsync();
            JsonDocument searchDoc = JsonDocument.Parse(searchContent);
            if(!searchResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Search request failed.");
                Console.WriteLine("status code: " + searchResponse.StatusCode);
                Console.WriteLine("reason phrase: " + searchResponse.ReasonPhrase);
                Console.WriteLine("content: " + searchContent);
                Console.WriteLine("Start BFS search");
                RequestFailed=true;

            }
            if (searchContent.StartsWith("[") && searchContent.EndsWith("]"))
            {
                Console.WriteLine("Search results:");
                foreach (JsonElement file in searchDoc.RootElement.EnumerateArray())
                {
                    Console.WriteLine("----------------------------");
                    Console.WriteLine("File ID: " + file.GetProperty("id").GetString());
                    Console.WriteLine("File Name: " + file.GetProperty("filename").GetString());
                    Console.WriteLine("File Size: " + file.GetProperty("size").GetInt64() + " bytes");
                    if (file.TryGetProperty("chunks", out JsonElement chElem) && chElem.ValueKind == JsonValueKind.Array)
                        Console.WriteLine("File Chunks: " + string.Join(", ", chElem.EnumerateArray().Select(c => c.GetString())));
                    Console.WriteLine("File Path: " + file.GetProperty("path").GetString());
                }
            }
            Console.WriteLine("Want to search in all folders? [need very long time](y/n)");
            string BFSsearch = Console.ReadLine() ?? "n";
        BFS:
            if (BFSsearch.ToLower() == "y"||RequestFailed)
            {
                HttpResponseMessage whereResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/files");
                string whereContent = await whereResponse.Content.ReadAsStringAsync();
                JsonDocument WhereDoc = JsonDocument.Parse(whereContent);
                List<JsonElement> files = new List<JsonElement>();

                Console.WriteLine("Searching from drive files: ");
                foreach (JsonElement file in WhereDoc.RootElement.EnumerateArray())
                {
                    bool IsFolder = false;
                    string filepath = "/";
                    string fileId = file.GetProperty("id").GetString() ?? "UnknownID";
                    string filename = "";
                    string filesize = "";
                    if (file.TryGetProperty("isFolder", out JsonElement isFolderElem))
                    {
                        IsFolder = isFolderElem.GetBoolean();
                    }
                    else IsFolder = false;
                    if (file.TryGetProperty("filename", out JsonElement _filename))
                    {
                        if (file.TryGetProperty("path", out JsonElement path)) filepath = path.GetString() ?? "/"; else filepath = "/";
                        filename = _filename.GetString() ?? "UnknownFilename";
                        filesize = file.GetProperty("size").GetInt64() + " bytes";
                        string FullPath;
                        if (!filepath.EndsWith("/")) FullPath = filepath + "/" + filename; else FullPath = filepath + filename;
                        if (!FullPath.StartsWith("/")) FullPath = "/" + FullPath;
                        Console.WriteLine(FullPath + " (" + filesize + ")");
                        if (IsFolder)
                        {
                            HttpResponseMessage FolderWhereResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/files?path=" + FullPath);
                            string FolderWhereContent = await FolderWhereResponse.Content.ReadAsStringAsync();
                            JsonDocument FolderWhereDoc = JsonDocument.Parse(FolderWhereContent);
                            List<string> ChildFoldersFullPaths = new List<string>();
                            foreach (JsonElement ChilFolder in FolderWhereDoc.RootElement.EnumerateArray())
                            {
                                if (ChilFolder.TryGetProperty("isFolder", out JsonElement isFolderElem2))
                                {
                                    string FullPath2;
                                    string PathGen = ChilFolder.GetProperty("path").GetString() ?? "/";
                                    if (!PathGen.EndsWith("/")) FullPath2 = PathGen + "/" + ChilFolder.GetProperty("filename").GetString(); else FullPath2 = PathGen + ChilFolder.GetProperty("filename").GetString();
                                    if (!FullPath2.StartsWith("/")) FullPath2 = "/" + FullPath2;
                                    if (isFolderElem2.GetBoolean())
                                    {
                                        ChildFoldersFullPaths.Add(FullPath2);
                                    }
                                    else
                                    {
                                        if (!search.StartsWith("*"))
                                        {
                                            if (ChilFolder.GetProperty("filename").GetString() == search)
                                                files.Add(ChilFolder);
                                        }
                                        else
                                        {
                                            if (ChilFolder.GetProperty("filename").GetString().EndsWith(search.TrimStart('*')))
                                                files.Add(ChilFolder);
                                        }
                                    }
                                }
                                if (!search.StartsWith("*"))
                                {
                                    if (ChilFolder.GetProperty("filename").GetString() == search)
                                        files.Add(ChilFolder);
                                }
                                else
                                {
                                    if (ChilFolder.GetProperty("filename").GetString().EndsWith(search.TrimStart('*')))
                                        files.Add(ChilFolder);
                                }
                            }
                            while (ChildFoldersFullPaths.Count > 0)
                            {
                                List<string> TEMPFolders = new List<string>();
                                foreach (string ChildFolderFullPath in ChildFoldersFullPaths)
                                {
                                    HttpResponseMessage ChildFolderWhereResponse = await Program.Client.GetAsync("https://drive.nexabox.de/api/files?path=" + ChildFolderFullPath);
                                    string ChildFolderWhereContent = await ChildFolderWhereResponse.Content.ReadAsStringAsync();
                                    JsonDocument ChildFolderWhereDoc = JsonDocument.Parse(ChildFolderWhereContent);
                                    foreach (JsonElement ChilFolder in ChildFolderWhereDoc.RootElement.EnumerateArray())
                                    {
                                        if (ChilFolder.TryGetProperty("isFolder", out JsonElement isFolderElem2))
                                        {
                                            string FullPath2;
                                            string PathGen = ChilFolder.GetProperty("path").GetString() ?? "/";
                                            if (!PathGen.EndsWith("/")) FullPath2 = PathGen + "/" + ChilFolder.GetProperty("filename").GetString(); else FullPath2 = PathGen + ChilFolder.GetProperty("filename").GetString();
                                            if (!FullPath2.StartsWith("/")) FullPath2 = "/" + FullPath2;
                                            if (isFolderElem2.GetBoolean())
                                            {
                                                TEMPFolders.Add(FullPath2);
                                            }
                                        }
                                        if (!search.StartsWith("*"))
                                        {
                                            if (ChilFolder.GetProperty("filename").GetString() == search)
                                                files.Add(ChilFolder);
                                        }
                                        else
                                        {
                                            if (ChilFolder.GetProperty("filename").GetString().EndsWith(search.TrimStart('*')))
                                                files.Add(ChilFolder);
                                        }
                                    }
                                }
                                ChildFoldersFullPaths = TEMPFolders;
                            }
                        }
                        else
                        {
                            if (!search.StartsWith("*"))
                            {
                                if (filename == search)
                                    files.Add(file);
                            }
                            else
                            {
                                if (filename.EndsWith(search.TrimStart('*')))
                                    files.Add(file);
                            }
                        }
                    }
                    else continue;
                }

                Console.WriteLine("Search completed.");
                foreach (JsonElement file in files)
                {
                    bool skip = false;

                    if (!file.TryGetProperty("id", out JsonElement idElem))
                    {
                        Console.WriteLine("[Warning] Missing 'id' in a search result.");
                        skip = true;
                    }
                    if (!file.TryGetProperty("filename", out JsonElement fnElem))
                    {
                        Console.WriteLine("[Warning] Missing 'filename' in a search result.");
                        skip = true;
                    }
                    if (!file.TryGetProperty("size", out JsonElement szElem))
                    {
                        Console.WriteLine("[Warning] Missing 'size' in a search result.");
                        skip = true;
                    }
                    if (!file.TryGetProperty("path", out JsonElement pElem))
                    {
                        Console.WriteLine("[Warning] Missing 'path' in a search result.");
                        skip = true;
                    }

                    if (skip)
                    {
                        Console.WriteLine("[Debug] Raw JSON element: " + file.ToString());
                        continue; // 或者不加 continue，直接继续打印其他内容
                    }

                    Console.WriteLine("----------------------------");
                    Console.WriteLine("File ID: " + idElem.GetString());
                    Console.WriteLine("File Name: " + fnElem.GetString());
                    Console.WriteLine("File Size: " + szElem.GetInt64() + " bytes");

                    if (file.TryGetProperty("chunks", out JsonElement chElem) && chElem.ValueKind == JsonValueKind.Array)
                        Console.WriteLine("File Chunks: " + string.Join(", ", chElem.EnumerateArray().Select(c => c.GetString())));

                    Console.WriteLine("File Path: " + pElem.GetString());
                }
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
            Console.WriteLine("  copy <src> to <dst>             复制文件");
            Console.WriteLine("  cd <path>                       切换目录");
            Console.WriteLine("  cp <src> to <dst>               复制文件 (别名: copy)");
            Console.WriteLine("  plks <link@link2@link3> to <path> 批量拉取已分享文件");
            Console.WriteLine("  mkls <fileID> with <password>   创建文件分享链接");
        }
    }
}
