using Microsoft.Extensions.Logging;
using MsGraph.Email;

// Define ANSI color escape sequences
const string RESET = "\u001b[0m";
const string GREEN = "\u001b[32m";
const string RED = "\u001b[31m";
const string CYAN = "\u001b[36m";
const string DARKYELLOW = "\u001b[33m";

Console.WriteLine("Hello...");
var emailAddress = "pigida@hotmail.com";

//var loggerConfiguration =
//    new LoggerConfiguration()
//        .MinimumLevel.Verbose()
//        .Enrich.FromLogContext(); // .Enrich.WithMachineName().Enrich.WithThreadId()                                       

//loggerConfiguration.WriteTo.File(outputTemplate: "{Timestamp:MM-dd HH:mm:ss.fff} {Level:w3} {Message}  {Exception}{NewLine}");

using var loggerFactory = LoggerFactory.Create(builder => { _ = builder.AddConsole(); });

ILogger logger = loggerFactory.CreateLogger<Program>();

var d = new Emailer2025(logger);

if (DateTime.Now == DateTime.Today) _ = await d.Send(emailAddress, $"Subject: Test", $"Body");

if (DateTime.Now == DateTime.Today)
{
    var (success, report) = await d.ListInboxItemsMatchingEmailAddress(emailAddress);
    Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}    {emailAddress}\n");
}

do
{
    try
    {
        Console.WriteLine($"{CYAN}Waiting for new email...{RESET}");

        var (success, report) = await d.StandByForNewEmailAndPlayWavFileWhenEmailArrives();
        Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{RED}Error: {ex.Message}{RESET}");
    }

    //Console.WriteLine($"{CYAN}Press any key to wait again or Escape to exit{RESET}");
} while (true); // Console.ReadKey().Key != ConsoleKey.Escape);

Console.WriteLine($"{GREEN}Done!{RESET}");