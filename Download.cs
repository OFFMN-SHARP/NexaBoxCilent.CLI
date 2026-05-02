using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexaBox.CLI.CouldFile
{
    public static class Download
    {
        public static async Task DownloadFile(string fileName, string localPath)
        {
            // 1. 获取文件列表，找到目标文件
            HttpResponseMessage listResp = await Program.Client.GetAsync("https://drive.nexabox.de/api/files");
            if (!listResp.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to retrieve file list.");
                return;
            }
            string listContent = await listResp.Content.ReadAsStringAsync();
            using JsonDocument listDoc = JsonDocument.Parse(listContent);

            JsonElement? targetFile = null;
            foreach (JsonElement file in listDoc.RootElement.EnumerateArray())
            {
                if (file.GetProperty("filename").GetString() == fileName)
                {
                    targetFile = file;
                    break;
                }
            }

            if (targetFile == null)
            {
                Console.WriteLine($"File '{fileName}' not found on drive.");
                return;
            }

            // 2. 获取切片 URL 列表
            var chunks = targetFile.Value.GetProperty("chunks").EnumerateArray()
                .Select(c => c.GetString())
                .ToList();

            long totalSize = targetFile.Value.GetProperty("size").GetInt64();
            Console.WriteLine($"Downloading '{fileName}' ({totalSize} bytes, {chunks.Count} chunks)...");

            // 3. 按顺序下载所有切片并写入本地文件
            if(Directory.Exists(localPath)) localPath = Path.Combine(localPath, fileName);
            using FileStream fs = File.Create(localPath);
            long downloaded = 0;

            for (int i = 0; i < chunks.Count; i++)
            {
                string chunkUrl = chunks[i];
                HttpResponseMessage chunkResp = await Program.Client.GetAsync(chunkUrl);
                if (!chunkResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to download chunk {i + 1}/{chunks.Count}.");
                    return;
                }

                using Stream chunkStream = await chunkResp.Content.ReadAsStreamAsync();
                await chunkStream.CopyToAsync(fs);
                downloaded += chunkStream.Length; // approximate

                // 进度提示
                Console.Write($"\rProgress: {i + 1}/{chunks.Count} chunks downloaded");
            }

            Console.WriteLine();
            Console.WriteLine($"Download complete. File saved to '{localPath}'.");
        }
        public static async Task BatchShareLinkParser(string Links,string fPath)
        {
            string[] AllLinks = Links.Split('@');
            if (AllLinks[0].StartsWith("*"))
            {
                List<string> DoneLinks =new List<string>();
                AllLinks[0]=AllLinks[0].Substring(1);
                AllLinks =AllLinks.Select(x => "="+x).ToArray();
            }
            foreach (string link in AllLinks)
            {//https://drive.nexabox.de/share.html?id=245e3332
                string ID = link.Split('=')[1];
                HttpResponseMessage Share =await Program.Client.GetAsync("https://drive.nexabox.de/api/public/share?id=" + ID);
                string ShareContent = await Share.Content.ReadAsStringAsync();
                JsonDocument ShareDoc = JsonDocument.Parse(ShareContent);
                JsonElement ShareRoot = ShareDoc.RootElement;
                //{"filename":"...","size":123,"needPassword":true}
                Console.WriteLine(new string('=', 20));
                string FileName =ShareRoot.GetProperty("filename").GetString();
                long FileSize = ShareRoot.GetProperty("size").GetInt64();
                bool NeedPassword = ShareRoot.GetProperty("needPassword").GetBoolean();
                Console.WriteLine("File Name: " + FileName);
                Console.WriteLine("File Size: " + FileSize + " bytes");
                Console.Write("Are you sure to download this file? (y/n): ");
                ConsoleKeyInfo key = Console.ReadKey();
                if(!key.KeyChar.ToString().ToLower().Equals("y"))continue;
                while (true)
                {//{"id":"a1b2c3d4", "password":"abcd"}
                    Console.WriteLine();
                    Console.Write("Please enter your password: ");
                    string Password = Console.ReadLine() ?? "";
                    var downloadData = new
                    {
                        id = ID,
                        password = Password
                    };
                    HttpResponseMessage FileMetaGet = await Program.Client.PostAsync("https://drive.nexabox.de/api/public/share?id=" + ID, new StringContent(JsonSerializer.Serialize(downloadData), System.Text.Encoding.UTF8, "application/json"));
                    if (FileMetaGet.IsSuccessStatusCode)
                    {
                        string FileMetaContent = await FileMetaGet.Content.ReadAsStringAsync();
                        JsonDocument FileMetaDoc = JsonDocument.Parse(FileMetaContent);
                        JsonElement FileMetaRoot = FileMetaDoc.RootElement;
                        if (FileMetaRoot.TryGetProperty("chunks", out JsonElement Chunks))
                        {
                            var chunks = Chunks.EnumerateArray().Select(c => c.GetString()).ToList();
                            var localFileName = Path.Combine(fPath, FileName);
                            Console.WriteLine($"Starting download of '{FileName}' ({FileSize} bytes) in {chunks.Count} chunk(s)...");
                            using FileStream fs = File.Create(localFileName);
                            for (int i = 0; i < chunks.Count; i++)
                            {
                                string chunkUrl = chunks[i];
                                HttpResponseMessage chunkResp = await Program.Client.GetAsync(chunkUrl);

                                using Stream chunkStream = await chunkResp.Content.ReadAsStreamAsync();
                                await chunkStream.CopyToAsync(fs);

                                Console.Write($"\rProgress: {i + 1}/{chunks.Count} chunks downloaded");
                            }
                            Console.WriteLine();
                            Console.WriteLine($"File saved to '{localFileName}'.");
                        }
                    }
                    else Console.WriteLine("File not found or password is incorrect");
                }
            }
        }
    }
}
