using NexaBox.CLI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexaBox.CLI
{
    public static class EditW
    {
        public static async Task Edit(string FileName)
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
                    Console.WriteLine("3:Upload to cloud storage.");
                    Console.WriteLine("4:Cancel.");
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
                            if (string.IsNullOrEmpty(FileName))
                            {
                                Console.Write("Input the file name:");
                                FileName = Console.ReadLine() ?? "null";
                            }
                            File.WriteAllText(FileName, Text_Temp.ToString());
                            Console.WriteLine("Message: Text saved to file.");
                            break;
                        case 2:
                            Console.Write("Input the username:");
                            string To_User = Console.ReadLine() ?? "null";
                            await UserCommandFunctions.SendMessage(To_User, Text_Temp.ToString());
                            break;
                        case 3:
                            if (string.IsNullOrEmpty(FileName))
                            {
                                Console.Write("Input the file name:");
                                FileName = Console.ReadLine() ?? "null";
                            }
                            File.WriteAllText(FileName, Text_Temp.ToString());
                            await CouldFile.Upload.Uploader(FileName);
                            File.Delete(FileName);
                            break;
                        case 4:
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
    }
}
