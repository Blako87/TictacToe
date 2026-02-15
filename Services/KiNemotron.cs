using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Summary description for Class1
/// </summary>
public class KiNemotron
{
    private readonly string aiServiceUrl = "http://192.168.178.50:1234/v1/chat/completions";

    public async Task<string?> GetAiResponseAsync(string boardsnapShot)
    {
        var requestData = new
        {
            model = "nemotron",
            messages = new[]
            {
                new { role = "system", content = "You are a profi for playing tic tac toe. The playfield looks like this (0=empty, 1=X, 2=O):\n[1, 0, 2]\n[0, 1, 0]\n[0, 0, 0]" },
                new { role = "user", content = $"What is the best move for O in the following board?responde just with 0,1,2 as index! no chars like (a,A,!,/,[],ß)!: {boardsnapShot}" }
            }
        };

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var jsonRequest = JsonSerializer.Serialize(requestData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(aiServiceUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
                return result ?? string.Empty;
            }

            return string.Empty;
        }
        catch (HttpRequestException)
        {
            return string.Empty;
        }
        catch (TaskCanceledException)
        {
            return string.Empty;
        }
    }
}
