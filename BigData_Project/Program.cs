using BigData_Project;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System.Configuration;
using System.Net;
using System.Text.RegularExpressions;


public class Program
{
    private static readonly string EndpointUri = ConfigurationManager.AppSettings["EndPointUri"] ?? "";

    private static readonly string PrimaryKey = ConfigurationManager.AppSettings["PrimaryKey"] ?? "";

    private CosmosClient cosmosClient;

    private Database database;

    private Container container;

    private string databaseId = "Database";
    private string containerId = "Container1";

    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Beginning operations...\n");
            Program p = new Program();
            await p.Start();

        }
        catch (CosmosException de)
        {
            Exception baseException = de.GetBaseException();
            Console.WriteLine("{0} error occurred: {1}", de.StatusCode, de);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: {0}", e);
        }
        finally
        {
            Console.WriteLine("End of operation");
            Console.ReadKey();
        }
    }

    public async Task Start()
    {
        this.cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "BigData_Project" });
        await this.CreateDatabaseAsync();
        await this.CreateContainerAsync();
        await this.ScaleContainerAsync();
        await this.AddItemsToContainerAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        this.database = await this.cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
        Console.WriteLine("Created Database: {0}\n", this.database.Id);
    }

    private async Task CreateContainerAsync()
    {
        this.container = await this.database.CreateContainerIfNotExistsAsync(containerId, "/id", 400);
        Console.WriteLine("Created Container: {0}\n", this.container.Id);
    }

    private async Task ScaleContainerAsync()
    {
        int? throughput = await this.container.ReadThroughputAsync();
        if (throughput.HasValue)
        {
            Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
            int newThroughput = throughput.Value + 100;

            await this.container.ReplaceThroughputAsync(newThroughput);
            Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
        }
    }

    private async Task AddItemsToContainerAsync()
    {
        var listOfPharmacies = ReadPharmaciesFromExcel("../../../../PharmaciesInUK.xlsx", "pharmacystoragetable-2023-02-08");
        foreach (var pharmacy in listOfPharmacies)
        {
            try
            {
                ItemResponse<Pharmacy> pharmacyResponse = await this.container.ReadItemAsync<Pharmacy>(pharmacy.Id, new PartitionKey(pharmacy.Id));
                Console.WriteLine("Item in database with id: {0} already exists\n", pharmacyResponse.Resource.Id);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                ItemResponse<Pharmacy> pharmacyResponse = await this.container.CreateItemAsync<Pharmacy>(pharmacy, new PartitionKey(pharmacy.Id));
                Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", pharmacyResponse.Resource.Id, pharmacyResponse.RequestCharge);
            }
        }
    }

    private static List<Pharmacy> ReadPharmaciesFromExcel(string filePath, string worksheetName)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(stream);

        var worksheet = package.Workbook.Worksheets[worksheetName];
        var rowCount = worksheet.Dimension.Rows;

        var pharmacies = new List<Pharmacy>();

        for (var row = 2; row <= rowCount; row++)
        {

            worksheet.Select($"{row}:{row}");
            var cells = worksheet.SelectedRange.Select(x => x.Text).ToArray();
            pharmacies.Add(MapPharmacy(cells));
        }

        return pharmacies;
    }

    private static Pharmacy MapPharmacy(string[] cells)
    {

        var addressLine1 = cells[3]?.Trim() ?? string.Empty;
        var addressLine2 = cells[5]?.Trim() ?? string.Empty;
        var addressLine3 = cells[7]?.Trim() ?? string.Empty;
        var county = cells[9]?.Trim() ?? string.Empty;
        var Id = cells[11]?.Trim() ?? string.Empty;
        var location = cells[19]?.Trim() ?? string.Empty;
        var postcode = cells[21]?.Trim() ?? string.Empty;
        var town = cells[23]?.Trim() ?? string.Empty;
        var tradingName = cells[25]?.Trim() ?? string.Empty;

        string pattern = @"(-?\d+\.\d+),(-?\d+\.\d+)";
        Match match = Regex.Match(location, pattern);
        string longitude = "";
        string latitude = "";

        if (match.Success && match.Groups.Count == 3)
        {
            longitude = match.Groups[1].Value;
            latitude = match.Groups[2].Value;
        }
        else
        {
            Console.WriteLine("Coordinates not found in the given string.");
        }

        return new Pharmacy(Id, tradingName, addressLine1, addressLine2, addressLine3, town, county, postcode, latitude, longitude);
    }
}
