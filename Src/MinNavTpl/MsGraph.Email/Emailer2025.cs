using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
namespace MsGraph.Email;
public class Emailer2025
{
    const string appReg = "EmailAssistantAnyAndPersonal2_2024";
    readonly ILogger _logger;
    readonly IConfiguration _configuration;
    GraphServiceClient _graphClient;

    public Emailer2025(ILogger lgr)
    {
        _logger = lgr;
        _configuration = new ConfigurationBuilder().AddUserSecrets<Emailer2025>().Build();

        var clientId = _configuration[$"{appReg}:ClientId"] ?? throw new InvalidOperationException("¦·MicrosoftGraphClientId is missing in configuration");

        _graphClient = new MsGraphLibVer1.MyGraphDriveServiceClient(clientId).DriveClient;       //new GraphServiceClient(new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { TenantId = "consumers", ClientId = clientId, RedirectUri = new Uri("http://localhost") }), ["Mail.Send", "Mail.Send.Shared", "User.Read"]); // Important: Use "consumers" for personal accounts //tu: this one keeps being INTERACTIVE!!! ASKS FOR LOGIN EVERY TIME!!! even on OLD GOOD ONE: EmailAssistantAnyAndPersonal2.
    }

    public async Task<(bool success, string report)> Send(string emailAddress, string msgSubject, string msgBody, string[]? attachedFilenames = null, string? imageFullPathFilename = null)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var message = new Message
            {
                Subject = msgSubject,
                Body = new ItemBody { Content = msgBody, ContentType = BodyType.Html },
                ToRecipients = [new Recipient { EmailAddress = new EmailAddress { Address = emailAddress } }],
                Attachments = []
            };

            if (attachedFilenames?.Length > 0) // Add regular attachments if any
            {
                message.Attachments.AddRange(
                    attachedFilenames.Select(file =>
                        new FileAttachment
                        {
                            Name = Path.GetFileName(file),
                            ContentBytes = File.ReadAllBytes(file),
                            OdataType = "#microsoft.graph.fileAttachment"
                        }));
            }

            if (imageFullPathFilename != null && File.Exists(imageFullPathFilename)) // Add inline image if provided
            {
                message.Attachments.Add(new FileAttachment
                {
                    Name = Path.GetFileName(imageFullPathFilename),
                    ContentBytes = File.ReadAllBytes(imageFullPathFilename),
                    ContentType = "image/jpeg",
                    ContentId = "image1",
                    IsInline = true,
                    OdataType = "#microsoft.graph.fileAttachment"
                });
            }

            await _graphClient.Me.SendMail.PostAsync(new Microsoft.Graph.Me.SendMail.SendMailPostRequestBody { Message = message, SaveToSentItems = true });

            _logger.LogInformation($"sent to:  {emailAddress,-49} Subj: {msgSubject} \t (took {sw.Elapsed:m\\:ss\\.f})");

            return (true, "Success sending email.");
        }
        catch (Exception ex)
        {
            Console.Beep(4500, 1000); Console.Beep(5000, 1000); Console.Beep(5500, 1000); _logger.LogError(ex, emailAddress);
            return (false, $"{ex.Message}\n{emailAddress}");
        }
    }

    public async Task<(bool success, string report)> ListInboxItemsMatchingEmailAddress(string emailAddress)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Get messages from the inbox folder
            var messagesResponse = await _graphClient.Me.MailFolders["inbox"].Messages.GetAsync(requestConfiguration =>
            {
                // Filter for messages that have the email address in from or recipients
                // Using $search would be more efficient but requires specific permissions
                requestConfiguration.QueryParameters.Top = 50; // Limit results
                requestConfiguration.QueryParameters.Select = ["subject", "receivedDateTime", "from", "toRecipients", "ccRecipients", "bodyPreview", "id"];
                //requestConfiguration.QueryParameters.OrderBy = ["receivedDateTime desc"];
            });

            if (messagesResponse?.Value == null)
                return (false, "No messages found or unable to access inbox.");

            // Filter the messages client-side for the matching email address
            var matchingMessages = messagesResponse.Value
                .Where(msg =>
                    // Check sender
                    msg.From?.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true ||
                    // Check recipients
                    msg.ToRecipients?.Any(r => r.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true) == true ||
                    // Check CC recipients
                    msg.CcRecipients?.Any(r => r.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true) == true
                )
                .ToList();

            if (matchingMessages.Count() == 0)
                return (true, $"No messages found matching email address: {emailAddress}");

            // Build report string with matching messages
            var sb = new System.Text.StringBuilder();
            _ = sb.AppendLine($"Found {matchingMessages.Count()} messages matching email address: {emailAddress}");
            _ = sb.AppendLine();

            foreach (var msg in matchingMessages)
            {
                var receivedDate = DateTime.Parse(msg.ReceivedDateTime?.ToString() ?? DateTime.Now.ToString());
                _ = sb.AppendLine($"  Date     {receivedDate:yyyy-MM-dd HH:mm}");
                _ = sb.AppendLine($"  From     {msg.From?.EmailAddress?.Name} <{msg.From?.EmailAddress?.Address}>");
                _ = sb.AppendLine($"  Subject  {msg.Subject}");
                _ = sb.AppendLine($"  Preview  {msg.BodyPreview?[..Math.Min(msg.BodyPreview.Length, 100)]}...");
                _ = sb.AppendLine();
            }

            _logger.LogInformation($"Found {matchingMessages.Count()} emails matching {emailAddress} (took {sw.Elapsed:m\\:ss\\.f})");

            return (true, sb.ToString());
        }
        catch (Exception ex)
        {
            Console.Beep(4500, 1000); Console.Beep(5000, 1000); Console.Beep(5500, 1000); _logger.LogError(ex, emailAddress);
            return (false, $"Error searching inbox: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> StandByForNewEmailAndPlayWavFileWhenEmailArrives(string? emailFilter = null, int pollingIntervalSeconds = 60, string? wavFilePath = @"C:\C\x\Gaming\TypeCatch\Assets\wav\Bad - Police.wav" /*"C:\Windows\Media\Windows Notify Email.wav"*/, CancellationToken? cancellationToken = null)
    {
        var sw = Stopwatch.StartNew();
        var soundPlayer1 = wavFilePath != null && File.Exists(wavFilePath)
            ? new System.Media.SoundPlayer(wavFilePath)
            : new System.Media.SoundPlayer(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media", "notify.wav"));

        var soundPlayer2 = new System.Media.SoundPlayer(@"C:\Windows\Media\Alarm01.wav");

        try
        {
            soundPlayer1.LoadAsync();
            soundPlayer2.LoadAsync();

            var localCancellationToken = cancellationToken ?? CancellationToken.None;
            var lastCheckTime = DateTimeOffset.Now;
            var emailsFound = 0;

            _logger.LogInformation($"Starting to monitor inbox for emails matching '{emailFilter}'. Checking every {pollingIntervalSeconds} seconds...");

            while (!localCancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get messages from the inbox folder that arrived after our last check
                    var messagesResponse = await _graphClient.Me.MailFolders["inbox"].Messages.GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = 25; // Limit results
                        requestConfiguration.QueryParameters.Select = ["subject", "receivedDateTime", "from", "id"];
                        requestConfiguration.QueryParameters.Filter = $"receivedDateTime gt {lastCheckTime.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";
                        //requestConfiguration.QueryParameters.OrderBy = ["receivedDateTime desc"];
                    });

                    if (messagesResponse?.Value != null && messagesResponse.Value.Count > 0)
                    {
                        // Update last check time to now
                        lastCheckTime = DateTimeOffset.Now;

                        // Filter for matching emails
                        var matchingMessages = messagesResponse.Value
                            .Where(msg =>
                                string.IsNullOrEmpty(emailFilter) ||
                                msg.From?.EmailAddress?.Address?.Contains(emailFilter, StringComparison.OrdinalIgnoreCase) == true ||
                                msg.Subject?.Contains(emailFilter, StringComparison.OrdinalIgnoreCase) == true
                            )
                            .ToList();

                        if (matchingMessages.Count > 0)
                        {
                            emailsFound += matchingMessages.Count;

                            foreach (var msg in matchingMessages)
                            {
                                var receivedDate = DateTime.Parse(msg.ReceivedDateTime?.ToString() ?? DateTime.Now.ToString());
                                _logger.LogInformation($"New email received at {receivedDate:yyyy-MM-dd HH:mm}: {msg.Subject} from {msg.From?.EmailAddress?.Address}");
                            }

                            await PlayNotificationSounds(soundPlayer1, soundPlayer2, localCancellationToken);
                        }
                    }

                    Console.WriteLine($"\u001b[38;2;250;180;250m{DateTime.Now:HH:mm:ss}\u001b[38;2;180;200;200m **\u001b[38;2;250;100;100m Wait \u001b[38;2;100;200;100m for \u001b[38;2;100;100;200m the next polling interval...\u001b[0m");
                    await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), localCancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation($"Normal cancellation, just exit the loop.");
                    break;
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError ex)
                {
                    _logger.LogWarning(ex, $"Error while polling for new emails: {ex.Error?.Message} ■ ■");
                    _graphClient = new MsGraphLibVer1.MyGraphDriveServiceClient(_configuration[$"{appReg}:ClientId"] ?? throw new InvalidOperationException("MicrosoftGraphClientId is missing in configuration")).DriveClient;
                }
                catch (Exception ex)
                {
                    Console.Beep(4500, 1000); Console.Beep(5000, 1000); Console.Beep(5500, 1000);
                    _logger.LogError(ex, $"Error while polling for new emails: {ex.GetType().Name} - {ex.Message} ■");
                    await Task.Delay(TimeSpan.FromSeconds(Math.Min(pollingIntervalSeconds * 2, 300)), localCancellationToken); // Backoff on error
                }
            } // while (!localCancellationToken.IsCancellationRequested)

            _logger.LogInformation($"Email monitoring stopped after {sw.Elapsed.TotalMinutes:F1} minutes. Found {emailsFound} matching emails.");
            return (true, $"Email monitoring completed. Found {emailsFound} matching emails during {sw.Elapsed.TotalMinutes:F1} minutes of monitoring.");
        }
        catch (TaskCanceledException)
        {
            return (true, $"Email monitoring was cancelled after {sw.Elapsed.TotalMinutes:F1} minutes.");
        }
        catch (Exception ex)
        {
            Console.Beep(4500, 1000); Console.Beep(5000, 1000); Console.Beep(5500, 1000); _logger.LogError(ex, ex.Message);
            return (false, $"Error monitoring emails: {ex.Message}");
        }
        finally
        {
            soundPlayer1.Dispose();
        }
    }

    static async Task PlayNotificationSounds(System.Media.SoundPlayer soundPlayer1, System.Media.SoundPlayer soundPlayer2, CancellationToken localCancellationToken)
    {
        for (var i = 0; i < 33; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                soundPlayer1.PlaySync();
                soundPlayer2.PlaySync();

                if (localCancellationToken.IsCancellationRequested)
                    return;
            }

            await Task.Delay(TimeSpan.FromSeconds(15), localCancellationToken);
        }
    }
}