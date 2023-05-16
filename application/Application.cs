using DemoKeypleLess.domain.api;
using Serilog;

namespace DemoKeypleLess.application {
    public class Application {
        private readonly ILogger _logger;
        private readonly MainServiceApi _mainService;

        public Application ( MainServiceApi mainService )
        {
            _logger = Log.ForContext<Application> ();
            _mainService = mainService;
        }

        public void Start ( )
        {
            Console.WriteLine($"Select and Read Contracts = {_mainService.SelectAndReadContracts ()}");
        }
    }
}
