using Emailing.NET6;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello...");
var emailAddress = "pigida@hotmail.com";

using var loggerFactory = LoggerFactory.Create(builder => { _ = builder.AddConsole(); });

ILogger logger = loggerFactory.CreateLogger<Program>();

var d = new Emailer2025(logger);

if (DateTime.Now == DateTime.Today) _ = await d.Send(emailAddress, $"Subject: Test", $"Body");

var (success, report) = await d.ListInboxItemsMatchingEmailAddress(emailAddress);
Console.WriteLine($"{(success ? "Success:" : "FAILED!")}   {report}    {emailAddress}");

do
{
    Console.WriteLine("Waiting for new email...");

    (success, report) = await d.StandByForNewEmailAndPlayWavFileWhenEmailArrives(@"C:\Windows\Media\Windows Notify Email.wav");
    Console.WriteLine($"{(success ? "Success:" : "FAILED!")}   {report}");

    Console.WriteLine("Press any key to wait again or Escape to exit");
} while (Console.ReadKey().Key != ConsoleKey.Escape);

Console.WriteLine("Done!");