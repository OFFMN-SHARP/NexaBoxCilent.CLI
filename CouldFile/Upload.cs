using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexaBox.CLI.CouldFile
{
    public static class Upload
    {
        public static async Task<List<byte[]>> SliceFile(string filePath, long sliceSize = 256 * 1024 * 1024) // 默认 256MB
        {
            var slices = new List<byte[]>();

            using FileStream fs = File.OpenRead(filePath);
            long fileSize = fs.Length;
            long remaining = fileSize;

            while (remaining > 0)
            {
                int currentSliceSize = (int)Math.Min(sliceSize, remaining);
                byte[] buffer = new byte[currentSliceSize];
                await fs.ReadAsync(buffer, 0, currentSliceSize);
                slices.Add(buffer);
                remaining -= currentSliceSize;
            }

            Console.WriteLine($"File sliced into {slices.Count} part(s).");
            return slices;
        }
        public static async Task CreateShareLink(string FileID,string? Password = null)
        {
            var Request = new
            {
                fileId = FileID,
                password = Password
            };
            var RequestContent = new StringContent(JsonSerializer.Serialize(Request), Encoding.UTF8, "application/json");
            var ShareLinkResp = await Program.Client.PostAsync("https://drive.nexabox.de/api/share", RequestContent);
            if (ShareLinkResp.IsSuccessStatusCode)
            {
                Console.WriteLine("Share link created.");
                string ShareStr =await ShareLinkResp.Content.ReadAsStringAsync();
                //{"success":true,"shareId":"b9ce5fd2","url":"/share.html?id=b9ce5fd2"}
                JsonDocument ShareDoc = JsonDocument.Parse(ShareStr);
                JsonElement ShareElement = ShareDoc.RootElement;
                bool Success = ShareElement.GetProperty("success").GetBoolean();
                if (Success)
                {////https://drive.nexabox.de/share.html?id=245e3332
                    Console.WriteLine("Share link: https://drive.nexabox.de"+ShareElement.GetProperty("url").GetString());
                    Console.WriteLine("Share ID: "+ShareElement.GetProperty("shareId").GetString());
                }
                else Console.WriteLine("Failed to get share link.");
            }
            else Console.WriteLine("Failed to create share link.");
        }
        public static async Task Uploader(string filePath)
        {
            string ParseredPath = String.Empty;
            string UNParseredPath ="/"+ filePath;
            string[] PathParts = UNParseredPath.Split('/').Skip(1).ToArray();
            if(PathParts.Length==1)ParseredPath="/";
            else ParseredPath = String.Join("/", PathParts.Take(PathParts.Length - 1));
            var fileslice = await SliceFile(filePath);
            List<string> chunks = new List<string>();
            for (int i = 0; i < fileslice.Count; i++)
            {
                HttpResponseMessage signResp = await Program.Client.GetAsync(
                    "https://drive.nexabox.de/api/signature");
                using JsonDocument signDoc = JsonDocument.Parse(
                    await signResp.Content.ReadAsStringAsync());
                JsonElement signRoot = signDoc.RootElement;

                string host = signRoot.GetProperty("host").GetString();
                string accessKeyId = signRoot.GetProperty("accessKeyId").GetString();
                string signature = signRoot.GetProperty("signature").GetString();
                string policy = signRoot.GetProperty("policy").GetString();

                using var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(accessKeyId), "OSSAccessKeyId");
                formData.Add(new StringContent(signature), "signature");
                formData.Add(new StringContent(policy), "policy");
                formData.Add(new StringContent(i.ToString()), "key");
                formData.Add(new ByteArrayContent(fileslice[i]), "file", "slice.png");
                formData.Headers.ContentType.MediaType = "image/png";
                HttpResponseMessage uploadResp = await Program.Client.PostAsync(host, formData);

                if (!uploadResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to upload slice {i}.");
                    return;
                }
                // 记录切片 URL（OSS 返回的地址）
                string chunkUrl = host + "/" + i;  // 具体以 API 返回为准
                chunks.Add(chunkUrl);
                Console.WriteLine($"Slice {i + 1}/{fileslice.Count} uploaded.");
            }
            // 把这两行从 for 循环里拿出来，放到 for 循环的下面
            var meta = new
            {
                filename = Path.GetFileName(filePath),
                size = new FileInfo(filePath).Length,
                chunks = chunks,  // 此时 chunks 已收集完所有切片 URL
                path = ParseredPath
            };
            var metaContent = new StringContent(
                JsonSerializer.Serialize(meta), Encoding.UTF8, "application/json");
            HttpResponseMessage metaResp = await Program.Client.PostAsync(
                "https://drive.nexabox.de/api/files", metaContent);
            if (metaResp.IsSuccessStatusCode)
                Console.WriteLine("Upload complete.");
            else
                Console.WriteLine("Metadata submission failed.");
        }
        public static async Task CreateFolderAsync(string folderName, string parentPath)
        {
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                name = folderName,
                path = parentPath
            }), Encoding.UTF8, "application/json");

            var response = await Program.Client.PostAsync($"https://drive.nexabox.de/api/folder", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
