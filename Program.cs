using System.Text.Json;

namespace gitlabtest
{
    // Класс для десериализации файла параметров
    internal class Settings
    {
        public string username { get; set; }
        public string password { get; set; }
        public string uri { get; set; }
    }
    // Класс для десерелиализации ответа OAuth
    internal class OAuthResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public int created_at { get; set; }
    }
    // Класс для десерелиализации информации о пользователе, указаны только нужные значения
    internal class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool is_admin { get; set; }
    }
    // Класс для десерелиализации массива пользователей
    internal class Users
    {
        public List<User> user { get; set; }
    }
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            string fileName = $"{AppDomain.CurrentDomain.BaseDirectory}Settings.json";
            string settingsString = System.IO.File.ReadAllText(fileName);
            Settings settings = JsonSerializer.Deserialize<Settings>(settingsString);
            string token = "";
            FormUrlEncodedContent content;
            HttpResponseMessage response;
            string responseString;
            OAuthResponse oauthresponse;
            HttpClient client = new HttpClient();
            // Автозизовываемся по OAuth, получим токен
            var postparams = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "username", settings.username },
                { "password", settings.password }
            };
            content = new FormUrlEncodedContent(postparams);
            Console.WriteLine("Логинимся на сервере...");
            try
            {
                response = await client.PostAsync($"{settings.uri}/oauth/token", content);
                responseString = await response.Content.ReadAsStringAsync();
                oauthresponse = JsonSerializer.Deserialize<OAuthResponse>(responseString);
                token = oauthresponse.access_token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return;
            }
            finally
            {
                Console.WriteLine($"Токен: {token}");
            }

            postparams = new Dictionary<string, string>
            {
                { "access_token", oauthresponse.access_token }
            };
            content = new FormUrlEncodedContent(postparams);
            Console.WriteLine("Получаем список пользователей...");
            try
            {
                response = await client.GetAsync($"{settings.uri}/api/v4/users?access_token={token}&admins=false");
                responseString = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<User>>(responseString);
                foreach (var user in users)
                {
                    if (!user.is_admin)
                    {
                        Console.WriteLine($"Блокируем пользователя {user.name} ...");
                        try
                        {
                            await client.PostAsync($"{settings.uri}/api/v4/users/{user.id}/block", content);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                            return;
                        }
                    
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return;
            }
            // Отзываем токен
            Console.WriteLine("Отзыв токена...");
            try
            {
                await client.GetAsync($"{settings.uri}/oauth/revoke?token={oauthresponse.access_token}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return;
            }
        }
    }
}