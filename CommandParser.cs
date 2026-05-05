using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexaBox.CLI
{
    public static class CommandParser
    {
        public static async Task MainLoop()
        {
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Nexabox CLI @[{Program.UserConfig.Username}|Build-{Program.Methods.Build}] <{Environment.CurrentDirectory}>");
                Console.Write("└──>");
                Console.ResetColor();
                string Command = Console.ReadLine() ?? "@Signal_UserInput=NULL";
                try
                {
                    await CommandParserAsync(Command);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An error occurred while processing the command: " + ex.Message);
                    Console.ResetColor();
                }
                Console.WriteLine();
            }
        }
        public static async Task<string?> CommandParserAsync(string Command)
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
                {"mkdir","from" },
                {"pushdir","" },

                //批量链接解析/单个链接创造
                {"plks","to" },
                { "mkls","with" },

    // 本地系统命令
        {"syscall", ""},

    // 工作台内部指令
        {"call", ""},

    // 身份显示
        {"whoami", ""},
        {"i?", ""},

    // 帮助与退出
        {"help", ""},
        {"exit", ""},
    
    //Cmd
        {"cd", ""},
        {"copy","to" },
        {"cp", "to"}
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
                            await UserCommandFunctions.FileSearch(Value1);
                            break;
                        case "whereis":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.FileSearch(Value1);
                            break;
                        case "grep":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.FileSearch(Value1);
                            break;
                        case "show":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.FileInfo(Value1);
                            break;
                        case "fetch":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.FileInfo(Value1);
                            break;
                        case "sed":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await EditW.Edit(Value1);
                            break;
                        case "create":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await EditW.Edit(Value1);
                            break;
                        case "from":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await CouldFile.Download.AutoDownload(Value1, Value2);
                            break;
                        case "download":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await CouldFile.Download.AutoDownload(Value1, Value2);
                            break;
                        case "syscall":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.CallSystemCommand(Value1);
                            break;
                        case "call":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            Console.WriteLine("Not supported yet.");
                            break;
                        case "whoami":
                            if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                            await UserCommandFunctions.UserInfo();
                            break;
                        case "i?":
                            if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                            await UserCommandFunctions.UserInfo();
                            break;
                        case "help":
                            if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                            await UserCommandFunctions.Help();
                            break;
                        case "exit":
                            if (Cmd.Length != 1) throw new Exception($"The command '{Cmd[0]}' does not require any arguments.");
                            await Program.Exit();
                            break;
                        case "send":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.SendMessage(Value2, Value1);
                            break;
                        case "write":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.SendMessage(Value2, Value1);
                            break;
                        case "msgo":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.SendMessage(Value2, Value1);
                            break;
                        case "msg":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.SendMessage(Value2, Value1);
                            break;
                        case "passwd":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.ChangeUserPassword();
                            break;
                        case "mkuser":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await Admin.UserManager.NewUser(Value1, Value2);
                            break;
                        case "invit":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await Admin.UserManager.NewUser(Value1, Value2);
                            break;
                        case "chprmis":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await Admin.UserManager.ChangeUserPermissions(Value1, Value2);
                            break;
                        case "rmuser":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await Admin.UserManager.DeleteUser(Value1);
                            break;
                        case "push":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await CouldFile.Upload.Uploader(Value1);
                            break;
                        case "pushto":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 arguments.");
                            await CouldFile.Upload.Uploader(Value1);
                            break;
                        case "cd":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await UserCommandFunctions.ChangeDir(Value1);
                            break;
                        case "copy":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.CopyFile(Value1, Value2);
                            break;
                        case "cp":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await UserCommandFunctions.CopyFile(Value1, Value2);
                            break;
                        case "plks":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await CouldFile.Download.BatchShareLinkParser(Value1,Value2);
                            break;
                        case "mkls":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await CouldFile.Upload.CreateShareLink(Value1, Value2);
                            break;
                        case "mkdir":
                            if (Cmd.Length != 4) throw new Exception($"The command '{Cmd[0]}' requires exactly 3 arguments.");
                            await CouldFile.Upload.CreateFolderAsync(Value1, Value2);
                            break;
                        case "pushdir":
                            if (Cmd.Length != 2) throw new Exception($"The command '{Cmd[0]}' requires exactly 1 argument.");
                            await CouldFile.Upload.PushDirectory(Value1);
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
    }
}
