﻿using DemoKeypleLess.domain.api;
using DemoKeypleLess.domain.utils;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

namespace DemoKeypleLess.application
{

    /// <summary>
    /// The Application class coordinates the main operations of the application.
    /// </summary>
    public class Application
    {
        private readonly ILogger _logger;

        // Service API to interact with the main service
        private readonly MainServiceApi _mainService;

        /// <summary>
        /// Application constructor.
        /// </summary>
        /// <param name="mainService">Main service API</param>
        public Application(MainServiceApi mainService)
        {
            // Initialize logger for this context
            _logger = Log.ForContext<Application>();

            // Assign main service instance
            _mainService = mainService;
        }

        /// <summary>
        /// Start the main execution of the application.
        /// </summary>
        public void Start()
        {
            Misc.DisplayAndLog("Waiting for a card...", ConsoleColor.DarkBlue, LogEventLevel.Information, _logger);

            _mainService.WaitForCardInsertion();

            Misc.DisplayAndLog("Card found!", ConsoleColor.Green, LogEventLevel.Information, _logger);

            // Select the card and read its content
            var output = _mainService.SelectAndReadContracts();

            // Deserialize the output into a dynamic object
            OutputDto outputDto = JsonConvert.DeserializeObject<OutputDto>(output);

            if (outputDto.OutputData.StatusCode == 0)
            {
                Misc.DisplayAndLog("Reading successful", ConsoleColor.DarkGreen, LogEventLevel.Information, _logger);
            }
            else
            {
                Misc.DisplayAndLog("Reading failed, status code: " + outputDto.OutputData.StatusCode, ConsoleColor.DarkGreen, LogEventLevel.Information, _logger);
            }

            var contracts = outputDto.OutputData.Items;
            int i = 1;
            bool multitripContractPresent = false;
            foreach (string contract in contracts)
            {
                // Print each item to the console
                Misc.DisplayAndLog($"Contract #{i++}:\r\n" + contract, ConsoleColor.DarkGreen, LogEventLevel.Information, _logger);
                if (contract.Contains("MULTI_TRIP"))
                {
                    multitripContractPresent = true;
                }
            }

            if (!multitripContractPresent)
            {
                Misc.DisplayAndLog("No MULTI_TRIP contract found.", ConsoleColor.DarkRed, LogEventLevel.Information, _logger);
            }

            int nbUnits;
            do
            {
                Console.Write("Please enter a number of units between 1 and 20: ");
                string input = Console.ReadLine();

                if (!int.TryParse(input, out nbUnits) || nbUnits < 1 || nbUnits > 20)
                {
                    Console.WriteLine("Error: please enter a valid number of units between 1 and 20.");
                }
            } while (nbUnits < 1 || nbUnits > 20);

            Misc.DisplayAndLog($"The card will now be (re)loaded with {nbUnits} unit(s).", ConsoleColor.DarkCyan, LogEventLevel.Information, _logger);

            Misc.DisplayAndLog("Waiting for a card...", ConsoleColor.DarkBlue, LogEventLevel.Information, _logger);

            _mainService.WaitForCardInsertion();

            Misc.DisplayAndLog("Card found.", ConsoleColor.Green, LogEventLevel.Information, _logger);

            // Select the card and increase the contract counter by one unit
            output = _mainService.SelectAndIncreaseContractCounter(nbUnits);

            outputDto = JsonConvert.DeserializeObject<OutputDto>(output);

            if (outputDto.OutputData.StatusCode == 0)
            {
                Misc.DisplayAndLog("Reloading successful", ConsoleColor.DarkGreen, LogEventLevel.Information, _logger);
            }
            else
            {
                Misc.DisplayAndLog("Reloading failed, status code: " + outputDto.OutputData.StatusCode, ConsoleColor.DarkGreen, LogEventLevel.Information, _logger);
            }
        }
    }
}
