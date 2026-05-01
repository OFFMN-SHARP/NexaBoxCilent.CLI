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
    }
}
