using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MsGraph.Email;

const string RESET = "\u001b[0m";
const string GREEN = "\u001b[32m";
const string RED = "\u001b[31m";
const string CYAN = "\u001b[36m";
const string DARKYELLOW = "\u001b[33m";

ILogger logger = LoggerFactory.Create(builder => { _ = builder.AddConsole(); }).CreateLogger<Program>();

var _configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var _emailBodyHtml = _configuration[$"EmailDetails:emailBodyHtml"] ?? throw new InvalidOperationException("¦·MicrosoftGraphClientId is missing in configuration");
var _emailAddress = _configuration[$"EmailDetails:emailAddress"] ?? throw new InvalidOperationException("¦·MicrosoftGraphClientId is missing in configuration");
var _emailSubject = _configuration[$"EmailDetails:emailSubject"] ?? throw new InvalidOperationException("¦·MicrosoftGraphClientId is missing in configuration");
var _emailService = new Emailer2025(logger);

while (true)
{
    Console.WriteLine($"\n{DARKYELLOW}MENU:{RESET}");
    Console.WriteLine("  'S' - Send a test email");
    Console.WriteLine("  'L' - List inbox items");
    Console.WriteLine("  'W' - Wait for a new email");
    Console.WriteLine("  'Esc' - Exit");
    Console.Write("Select an option: ");

    var keyInfo = Console.ReadKey(true);
    Console.WriteLine(keyInfo.KeyChar);

    switch (keyInfo.Key)
    {
        case ConsoleKey.S: await SendInitialTestEmail(_emailAddress, _emailSubject, _emailBodyHtml, _emailService); break;
        case ConsoleKey.L: await ListInboxItemsForEmail(RESET, GREEN, RED, _emailAddress, _emailService); break;
        case ConsoleKey.W: await WaitForNewEmailAndNotify(RESET, GREEN, RED, CYAN, _emailService); break;
        case ConsoleKey.Escape: Console.WriteLine($"{GREEN}Done!{RESET}"); return;
        default: Console.WriteLine($"{RED}Invalid option. Please try again.{RESET}"); break;
    }
}

static async Task SendInitialTestEmail(string emailAddress, string emailSubject, string emailBodyHtmlSample, Emailer2025 emailService)
{
    Console.WriteLine($"{CYAN}Sending test email to {GREEN} {emailAddress} {RESET} ...");
    var (success, report) = await emailService.Send(emailAddress, emailSubject, emailBodyHtmlSample);
    Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}    {emailAddress}\n");
}

static async Task ListInboxItemsForEmail(string RESET, string GREEN, string RED, string emailAddress, Emailer2025 d)
{
    Console.WriteLine($"{CYAN}Listing inbox items for {emailAddress}...{RESET}");
    var (success, report) = await d.ListInboxItemsMatchingEmailAddress(emailAddress);
    Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}    {emailAddress}\n");
}

static async Task WaitForNewEmailAndNotify(string RESET, string GREEN, string RED, string CYAN, Emailer2025 d)
{
    try
    {
        Console.WriteLine($"{CYAN}Waiting for new email... (Press Ctrl+C to cancel and return to menu){RESET}");
        var (success, report) = await d.StandByForNewEmailAndPlayWavFileWhenEmailArrives();
        Console.WriteLine($"{(success ? $"{GREEN}Success:{RESET}" : $"{RED}FAILED!{RESET}")}   {report}");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"\n{DARKYELLOW}Wait cancelled. Returning to menu.{RESET}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{RED}Error: {ex.Message}{RESET}");
    }
}
/*
{
  "EmailDetails": {
    "emailAddress": "aPigida@cetaris.com",
    "emailSubject": "▀▄▀▄▀▄▀▄ Rich HTML Test ▀▄▀▄▀▄▀▄",
    "emailBodyHtml": "<!DOCTYPE html>\n<html lang='en'>\n<head>\n    <meta charset='UTF-8'>\n    <meta name='viewport' content='width=device-width, initial-scale=1.0'>\n    <style>\n        body {\n            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;\n            background-color: #eef2f7;\n            margin: 0;\n            padding: 20px;\n            color: #333;\n        }\n        .container {\n            max-width: 700px;\n            margin: auto;\n            background: #ffffff;\n            border-radius: 12px;\n            box-shadow: 0 6px 18px rgba(0, 0, 0, 0.07);\n            overflow: hidden;\n        }\n        .header {\n            background: linear-gradient(135deg, #4a90e2 0%, #50e3c2 100%);\n            color: white;\n            padding: 30px 20px;\n            text-align: center;\n        }\n        .header h1 {\n            margin: 0;\n            font-size: 28px;\n            font-weight: 600;\n        }\n        .content {\n            padding: 30px 40px;\n        }\n        .content h2 {\n            color: #2c3e50;\n            font-size: 22px;\n        }\n        .content p {\n            line-height: 1.7;\n            font-size: 16px;\n            color: #555;\n        }\n        .settings-table {\n            width: 100%;\n            border-collapse: collapse;\n            margin: 25px 0;\n        }\n        .settings-table th, .settings-table td {\n            border: 1px solid #dfe6e9;\n            padding: 15px;\n            text-align: left;\n        }\n        .settings-table th {\n            background-color: #f8f9fa;\n            color: #4a90e2;\n            font-size: 16px;\n            font-weight: 600;\n        }\n        .settings-table td {\n            font-size: 15px;\n            color: #2d3436;\n        }\n        .settings-table tr:nth-child(even) {\n            background-color: #fdfdfe;\n        }\n        .highlight {\n            color: #d9534f;\n            font-weight: bold;\n            font-style: italic;\n        }\n        .footer {\n            text-align: center;\n            padding: 25px;\n            font-size: 13px;\n            color: #888;\n            background-color: #f8f9fa;\n            border-top: 1px solid #dfe6e9;\n        }\n        .footer a {\n            color: #4a90e2;\n            text-decoration: none;\n        }\n    </style>\n</head>\n<body>\n    <div class='container'>\n        <div class='header'>\n            <h1>Here is your SIM settings details</h1>\n        </div>\n        <div class='content'>\n            <h2>Hello [Customer Name],</h2>\n            <p>Thank you for choosing our service! As requested, here are the configuration details for your new SIM card. Please use the following settings to set up your device for data and MMS access.</p>\n            \n            <table class='settings-table'>\n                <thead>\n                    <tr>\n                        <th>Setting</th>\n                        <th>Value</th>\n                    </tr>\n                </thead>\n                <tbody>\n                    <tr>\n                        <td>APN (Access Point Name)</td>\n                        <td>global.internet.com</td>\n                    </tr>\n                    <tr>\n                        <td>Username</td>\n                        <td>[Your Username]</td>\n                    </tr>\n                    <tr>\n                        <td>Password</td>\n                        <td><span class='highlight'>Please see secure message</span></td>\n                    </tr>\n                    <tr>\n                        <td>MMSC</td>\n                        <td>http://mms.service.com/mms/wapenc</td>\n                    </tr>\n                    <tr>\n                        <td>MMS Proxy</td>\n                        <td>10.100.1.1</td>\n                    </tr>\n                    <tr>\n                        <td>MMS Port</td>\n                        <td>8080</td>\n                    </tr>\n                     <tr>\n                        <td>MCC (Mobile Country Code)</td>\n                        <td>310</td>\n                    </tr>\n                     <tr>\n                        <td>MNC (Mobile Network Code)</td>\n                        <td>260</td>\n                    </tr>\n                </tbody>\n            </table>\n\n            <p>If you experience any issues, our support team is ready to help. You can reach us 24/7 via our <a href='#'>Support Portal</a> or by calling 1-800-555-GIGA.</p>\n            <p>Best regards,<br><strong>The Connectivity Team</strong></p>\n        </div>\n        <div class='footer'>\n            &copy; 2024 GigaConnect Inc. All rights reserved.<br>\n            <a href='#'>Privacy Policy</a> | <a href='#'>Terms of Service</a>\n        </div>\n    </div>\n</body>\n</html>"
  },
  "WhereAmI": "MsGraph.Email.ConsoleApp.csProj"
}
*/