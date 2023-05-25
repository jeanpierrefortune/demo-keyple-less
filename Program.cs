// Bootstrap
using System.IO;
using DemoKeypleLess.application;
using DemoKeypleLess.domain.api;
using DemoKeypleLess.domain.spi;
using DemoKeypleLess.infrastructure.pcscreader;
using DemoKeypleLess.infrastructure.server;
using Microsoft.Extensions.Configuration;
using Serilog;


// Create and configure the logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.File("logs/demo-keyple-less-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

Log.Information("Starting the application");

var builder = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: true, reloadOnChange: true);

// Load configuration data
IConfigurationRoot configuration = builder.Build();

string serverHost = configuration.GetSection("Server")["host"];
int serverPort = int.Parse(configuration.GetSection("Server")["port"]);
string serverEndpoint = configuration.GetSection("Server")["endpoint"];
string readerName = configuration.GetSection("Reader")["name"];

try
{
    Log.Information("Retrieve reader and server connectors...");

    // Get an instance of the ReaderServiceSpi implementation
    ReaderServiceSpi cardService = PcscReaderServiceSpiProvider.getInstance();

    // Get an instance of the ServerSpi implementation
    ServerSpi server = ServerSpiProvider.getInstance(serverHost, serverPort, serverEndpoint);

    // Create an instance of the MainServiceApi using the reader service and server
    MainServiceApi mainService = MainServiceApiProvider.getService(cardService, readerName, server);

    Log.Information("Create and start application...");

    // Create an instance of the application and start it
    Application app = new Application(mainService);
    app.Start();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The application failed to start");
}
finally
{
    Log.Information("Closing the application");
    Log.CloseAndFlush();
}
