using Microsoft.Extensions.Logging;
using MsGraph.Email;

// Define ANSI color escape sequences
const string RESET = "\u001b[0m";
const string GREEN = "\u001b[32m";
const string RED = "\u001b[31m";
const string CYAN = "\u001b[36m";

Console.WriteLine("Hello...");
var emailAddress = "pigida@hotmail.com";

using var loggerFactory = LoggerFactory.Create(builder => { _ = builder.AddConsole(); });

ILogger logger = loggerFactory.CreateLogger<Program>();

var d = new Emailer2025(logger);

if (DateTime.Now == DateTime.Today) _ = await d.Send(emailAddress, $"Subject: Test", $"Body");

var (success, report) = await d.ListInboxItemsMatchingEmailAddress(emailAddress);
Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}    {emailAddress}\n");

do
{
    Console.WriteLine($"{CYAN}Waiting for new email...{RESET}");

    (success, report) = await d.StandByForNewEmailAndPlayWavFileWhenEmailArrives();
    Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}");

    Console.WriteLine($"{CYAN}Press any key to wait again or Escape to exit{RESET}");
} while (Console.ReadKey().Key != ConsoleKey.Escape);

Console.WriteLine($"{GREEN}Done!{RESET}");