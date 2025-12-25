using Microsoft.JSInterop;
using System.Net.Http.Json;

public class SyncService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;
    private const string DbFilename = "fow_campaign.db"; 

    public SyncService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task SyncOnStartup()
    {
        try 
        {
            Console.WriteLine("Checking for remote database updates...");
            
            var dbBytes = await _http.GetByteArrayAsync("/api/GetDatabase");

            if (dbBytes != null && dbBytes.Length > 0)
            {
                var module = await _js.InvokeAsync<IJSObjectReference>("import", "./db-sync.js");
                await module.InvokeVoidAsync("writeDatabaseToOpfs", DbFilename, dbBytes);
                Console.WriteLine("Local database updated from Cloud.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sync failed, using offline data: {ex.Message}");
        }
    }

    public async Task UploadCurrentState()
    {
        var module = await _js.InvokeAsync<IJSObjectReference>("import", "./db-sync.js");
        var dbBytes = await module.InvokeAsync<byte[]>("readDatabaseFromOpfs", DbFilename);
        
        if (dbBytes != null)
        {
            var content = new ByteArrayContent(dbBytes);
            var response = await _http.PostAsync("/api/UploadDatabase", content);
            
            if (response.IsSuccessStatusCode)
                Console.WriteLine("Upload Successful!");
        }
    }
}