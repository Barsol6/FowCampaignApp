using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using AuthorizationLevel = Microsoft.Azure.WebJobs.Extensions.Http.AuthorizationLevel;

public static class CampaignFunctions
{
    
    private static string blobConnString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    private static string containerName = "campaign-data";
    private static string fileName = "master_campaign.db";

    [FunctionName("GetDatabase")]
    public static async Task<IActionResult> GetDatabase(
        [Microsoft.Azure.WebJobs.HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req)
    {
        var blobServiceClient = new BlobServiceClient(blobConnString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync()) return new NotFoundResult();
        
        var stream = await blobClient.OpenReadAsync();
        return new FileStreamResult(stream, "application/octet-stream");
    }

    [FunctionName("UploadDatabase")]
    public static async Task<IActionResult> UploadDatabase(
        [Microsoft.Azure.WebJobs.HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req)
    {
        var blobServiceClient = new BlobServiceClient(blobConnString);
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
        var blobClient = containerClient.GetBlobClient(fileName);
        
        await blobClient.UploadAsync(req.Body, overwrite: true);

        return new OkObjectResult("Database Updated");
    }
}