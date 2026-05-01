using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexaBox.CLI.Admin
{
    public static class UserManager
    {
        public static async Task DeleteUser(string username)
        {
            var deleteData = new { username = username };
            string json = JsonSerializer.Serialize(deleteData);
            var request = new HttpRequestMessage(HttpMethod.Delete, "https://accounts.nexabox.de/api/admin/users")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            HttpResponseMessage response = await Program.Client.SendAsync(request);
            if (response.IsSuccessStatusCode)
                Console.WriteLine($"User '{username}' deleted successfully.");
            else
                Console.WriteLine($"Failed to delete user '{username}'.");
        }
        public static async Task ChangeUserPermissions(string username, string permission)
        {
            string[] group = permission.Split('@');
            string changePermissionsjson = JsonSerializer.Serialize(new { username = username, permissions = group });
            var changePermissionsContent = new StringContent(changePermissionsjson, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage changePermissionsResponse = await Program.Client.PutAsync("https://accounts.nexabox.de/api/admin/users/permissions", changePermissionsContent);
            if (changePermissionsResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("User permissions changed successfully.");
            }
            else Console.WriteLine("Failed to change user permissions.");
        }
        public static async Task NewUser(string username, string password)
        {
            string newUserjson = JsonSerializer.Serialize(new { username = username, initialPassword = password });
            var newUserContent = new StringContent(newUserjson, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage newUserResponse = await Program.Client.PostAsync("https://accounts.nexabox.de/api/admin/users", newUserContent);
            if (newUserResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("User created successfully.");
            }
            else
            {
                Console.WriteLine("Failed to create user.");
            }
        }
    }
}
