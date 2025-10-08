using System;
using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppCLDV;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly string _storageConnectionString;
    private BlobContainerClient _blobContainerClient;
    private TableClient _tableClient;
    
    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
        
        _storageConnectionString = Environment.GetEnvironmentVariable("connection");
        
        var serviceClient = new TableServiceClient(_storageConnectionString);
        _tableClient = serviceClient.GetTableClient("PeopleTable");
        
        _blobContainerClient = new BlobContainerClient(
            _storageConnectionString, "product-images");
        _blobContainerClient.CreateIfNotExists(
            Azure.Storage.Blobs.Models.PublicAccessType.Blob);
    }

    [Function(nameof(Function1))]
    public void Run([QueueTrigger("QueuesCldv", Connection = "")] QueueMessage message)
    {

        _logger.LogInformation("C# Queue trigger function processed: {messageText}", message.MessageText);
    }

    [Function(nameof(WriteToOrders))]

    public async Task WriteToOrders([QueueTrigger("stock-updates", Connection = "connection")] Azure.Storage.Queues.Models.QueueMessage message)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
        await _tableClient.CreateIfNotExistsAsync();
        var person = JsonSerializer.Deserialize<PersonEntity>(message.MessageText);
        if (person == null)
        {
            _logger.LogError("Failed to deserialize JSON message");
            return;
        }
        person.RowKey = Guid.NewGuid().ToString();
        person.PartitionKey = "People";
        _logger.LogInformation($"Saving entity with RowKey: {person.RowKey}");
        await _tableClient.AddEntityAsync(person);
        _logger.LogInformation("Successfully saved person to table.");
    }

    [Function("GetPeople")]
    public async Task<HttpResponseData> GetPeople(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "people")] HttpRequestData req)
    {
        _logger.LogInformation("C# Queue trigger function processed a request to get all people.");
        try
        {
            var people = await _tableClient.QueryAsync<PersonEntity>().ToListAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(people);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query table storage.");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync("An error occured while retrieving data from the table.");
            return response;
        }


    }
    [Function("AddProductImage")]
    public async Task<HttpResponseData> AddProductImage([HttpTrigger(AuthorizationLevel.Anonymous, "post"
        , Route = "product-with-image")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function " +
            "to add product with image received a request.");
        var newProduct = new PersonEntity();
        string? uploadedBlobUrl = null;
        var multipartReader = new MultipartReader(req.Headers.GetValues("Content-Type").First().Split(';')[1].Trim().Split('=')[1], req.Body);
        var section = await multipartReader.ReadNextSectionAsync();

        while (section != null)
        {
            var contentDisposition =
                section.Headers["Content-Disposition"].ToString();
            var name =
                contentDisposition.Split(';')[1].Trim().Split('=')[1].Trim('"');

            if (name == "Name" || name == "Email")
            {
                var value = await new
                    StreamReader(section.Body).ReadToEndAsync();
                if (name == "Name") newProduct.Name = value;
                if (name == "Email") newProduct.EmailAddress = value;

            }
            else if (name == "ProductImage")
            {
                var fileName = contentDisposition.Split(';')[2].Trim().Split('=')[1].Trim('"');
                var uniqueFileName = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}{Path.GetExtension(fileName)}";
                var blobClient = _blobContainerClient.GetBlobClient(uniqueFileName);
                await blobClient.UploadAsync(section.Body, true);
                uploadedBlobUrl = blobClient.Uri.ToString();
            }
            section = await multipartReader.ReadNextSectionAsync();
        }
        if (string.IsNullOrEmpty(newProduct.Name) ||
            string.IsNullOrEmpty(newProduct.EmailAddress) ||
            string.IsNullOrEmpty(uploadedBlobUrl))
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        newProduct.PartitionKey = "People";
        newProduct.RowKey = Guid.NewGuid().ToString();
        newProduct.ProductImageURL = uploadedBlobUrl;

        await _tableClient.AddEntityAsync(newProduct);
        _logger.LogInformation($"Successfully added {newProduct.Name}"
            + " and uploaded their picture");

        return req.CreateResponse(HttpStatusCode.Created);
    }

}