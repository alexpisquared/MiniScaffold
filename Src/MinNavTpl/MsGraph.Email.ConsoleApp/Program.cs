using Emailing.NET6;
using Microsoft.Extensions.Logging;

Console.WriteLine("Hello...");
var emailAddress = "pigida@hotmail.com";

using var loggerFactory = LoggerFactory.Create(builder => { _ = builder.AddConsole(); });

ILogger logger = loggerFactory.CreateLogger<Program>();

var d = new Emailer2025(logger);

if (DateTime.Now == DateTime.Today) _ = await d.Send(emailAddress, $"Subject: Test", $"Body");

_ = await d.ListInboxItemsMatchingEmailAddress(emailAddress);

Console.WriteLine("Done!");
