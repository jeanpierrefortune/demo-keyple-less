// Bootstrap

using Serilog.Events;
using Serilog;
using DemoKeypleLess.application;
using DemoKeypleLess.domain.spi;
using DemoKeypleLess.infrastructure.pcscreader;
using DemoKeypleLess.infrastructure.server;
using DemoKeypleLess.domain.api;

Log.Logger = new LoggerConfiguration ()
    .MinimumLevel.Debug ()
    .Enrich.FromLogContext ()
    .WriteTo.Console ( outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}" )
    .WriteTo.File ( "logs/demo-keyple-less-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7 )
    .CreateLogger ();

Log.Information ( "Starting the application" );

try
{
    Log.Information ( "Retrieve reader and server connectors..." );

    ReaderServiceSpi cardService = PcscReaderServiceSpiProvider.getInstance ();

    ServerSpi server = ServerSpiProvider.getInstance ( "http://localhost", 8080, "card/remote-plugin" );

    MainServiceApi mainService = MainServiceApiProvider.getService ( cardService, server );

    Log.Information ( "Create and start application..." );
    Application app = new Application ( mainService );
    app.Start ();
}
catch (Exception ex)
{
    Log.Fatal ( ex, "The application failed to start" );
}
finally
{
    Log.Information ( "Closing the application" );
    Log.CloseAndFlush ();
}