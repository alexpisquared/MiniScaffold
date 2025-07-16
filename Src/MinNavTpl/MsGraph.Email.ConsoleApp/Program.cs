using Microsoft.Extensions.Logging;
using MsGraph.Email;

const string RESET = "\u001b[0m";
const string GREEN = "\u001b[32m";
const string RED = "\u001b[31m";
const string CYAN = "\u001b[36m";
const string DARKYELLOW = "\u001b[33m";
const string emailAddress = "aPigida@cetaris.com";
const string emailBodyHtmlSample =
    """
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <style>
            body {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                background-color: #eef2f7;
                margin: 0;
                padding: 20px;
                color: #333;
            }
            .container {
                max-width: 700px;
                margin: auto;
                background: #ffffff;
                border-radius: 12px;
                box-shadow: 0 6px 18px rgba(0, 0, 0, 0.07);
                overflow: hidden;
            }
            .header {
                background: linear-gradient(135deg, #4a90e2 0%, #50e3c2 100%);
                color: white;
                padding: 30px 20px;
                text-align: center;
            }
            .header h1 {
                margin: 0;
                font-size: 28px;
                font-weight: 600;
            }
            .content {
                padding: 30px 40px;
            }
            .content h2 {
                color: #2c3e50;
                font-size: 22px;
            }
            .content p {
                line-height: 1.7;
                font-size: 16px;
                color: #555;
            }
            .settings-table {
                width: 100%;
                border-collapse: collapse;
                margin: 25px 0;
            }
            .settings-table th, .settings-table td {
                border: 1px solid #dfe6e9;
                padding: 15px;
                text-align: left;
            }
            .settings-table th {
                background-color: #f8f9fa;
                color: #4a90e2;
                font-size: 16px;
                font-weight: 600;
            }
            .settings-table td {
                font-size: 15px;
                color: #2d3436;
            }
            .settings-table tr:nth-child(even) {
                background-color: #fdfdfe;
            }
            .highlight {
                color: #d9534f;
                font-weight: bold;
                font-style: italic;
            }
            .footer {
                text-align: center;
                padding: 25px;
                font-size: 13px;
                color: #888;
                background-color: #f8f9fa;
                border-top: 1px solid #dfe6e9;
            }
            .footer a {
                color: #4a90e2;
                text-decoration: none;
            }
        </style>
    </head>
    <body>
        <div class="container">
            <div class="header">
                <h1>Here is your SIM settings details</h1>
            </div>
            <div class="content">
                <h2>Hello [Customer Name],</h2>
                <p>Thank you for choosing our service! As requested, here are the configuration details for your new SIM card. Please use the following settings to set up your device for data and MMS access.</p>
                
                <table class="settings-table">
                    <thead>
                        <tr>
                            <th>Setting</th>
                            <th>Value</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr>
                            <td>APN (Access Point Name)</td>
                            <td>global.internet.com</td>
                        </tr>
                        <tr>
                            <td>Username</td>
                            <td>[Your Username]</td>
                        </tr>
                        <tr>
                            <td>Password</td>
                            <td><span class="highlight">Please see secure message</span></td>
                        </tr>
                        <tr>
                            <td>MMSC</td>
                            <td>http://mms.service.com/mms/wapenc</td>
                        </tr>
                        <tr>
                            <td>MMS Proxy</td>
                            <td>10.100.1.1</td>
                        </tr>
                        <tr>
                            <td>MMS Port</td>
                            <td>8080</td>
                        </tr>
                         <tr>
                            <td>MCC (Mobile Country Code)</td>
                            <td>310</td>
                        </tr>
                         <tr>
                            <td>MNC (Mobile Network Code)</td>
                            <td>260</td>
                        </tr>
                    </tbody>
                </table>

                <p>If you experience any issues, our support team is ready to help. You can reach us 24/7 via our <a href="#">Support Portal</a> or by calling 1-800-555-GIGA.</p>
                <p>Best regards,<br><strong>The Connectivity Team</strong></p>
            </div>
            <div class="footer">
                &copy; 2024 GigaConnect Inc. All rights reserved.<br>
                <a href="#">Privacy Policy</a> | <a href="#">Terms of Service</a>
            </div>
        </div>
    </body>
    </html>
    """;

using var loggerFactory = LoggerFactory.Create(builder => { _ = builder.AddConsole(); });

ILogger logger = loggerFactory.CreateLogger<Program>();

var emailService = new Emailer2025(logger);

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
        case ConsoleKey.S: await SendInitialTestEmail(emailAddress, emailService); break;
        case ConsoleKey.L: await ListInboxItemsForEmail(RESET, GREEN, RED, emailAddress, emailService); break;
        case ConsoleKey.W: await WaitForNewEmailAndNotify(RESET, GREEN, RED, CYAN, emailService); break;
        case ConsoleKey.Escape: Console.WriteLine($"{GREEN}Done!{RESET}"); return;
        default: Console.WriteLine($"{RED}Invalid option. Please try again.{RESET}"); break;
    }
}

static async Task SendInitialTestEmail(string emailAddress, Emailer2025 emailService)
{
    Console.WriteLine($"{CYAN}Sending test email to {GREEN} {emailAddress} {RESET} ...");
    var (success, report) = await emailService.Send(emailAddress, $"Subject: Rich HTML Test", emailBodyHtmlSample);
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