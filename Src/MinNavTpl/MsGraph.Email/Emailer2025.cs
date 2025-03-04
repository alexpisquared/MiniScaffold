using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
namespace Emailing.NET6;
public class Emailer2025
{
  const string appReg = "EmailAssistantAnyAndPersonal2_2024";
  readonly ILogger _lgr;
  readonly IConfiguration _configuration;
  readonly GraphServiceClient _graphClient;

  public Emailer2025(ILogger lgr)
  {
    _lgr = lgr;
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

      _lgr.LogInformation($"sent to:  {emailAddress,-49} Subj: {msgSubject} \t (took {sw.Elapsed:m\\:ss\\.f})");

      return (true, "Success sending email.");
    }
    catch (Exception ex)
    {
      _lgr.LogError(ex, emailAddress);
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
              (msg.From?.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true) ||
              // Check recipients
              (msg.ToRecipients?.Any(r => r.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true) == true) ||
              // Check CC recipients
              (msg.CcRecipients?.Any(r => r.EmailAddress?.Address?.Contains(emailAddress, StringComparison.OrdinalIgnoreCase) == true) == true)
          )
          .ToList();

      if (matchingMessages.Count() == 0)
        return (true, $"No messages found matching email address: {emailAddress}");

      // Build report string with matching messages
      var sb = new System.Text.StringBuilder();
      sb.AppendLine($"Found {matchingMessages.Count()} messages matching email address: {emailAddress}");
      sb.AppendLine();

      foreach (var msg in matchingMessages)
      {
        var receivedDate = DateTime.Parse(msg.ReceivedDateTime?.ToString() ?? DateTime.Now.ToString());
        sb.AppendLine($"Date: {receivedDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"From: {msg.From?.EmailAddress?.Name} <{msg.From?.EmailAddress?.Address}>");
        sb.AppendLine($"Subject: {msg.Subject}");
        sb.AppendLine($"Preview: {msg.BodyPreview?.Substring(0, Math.Min(msg.BodyPreview.Length, 100))}...");
        sb.AppendLine();
      }

      _lgr.LogInformation($"Found {matchingMessages.Count()} emails matching {emailAddress} (took {sw.Elapsed:m\\:ss\\.f})");

      return (true, sb.ToString());
    }
    catch (Exception ex)
    {
      _lgr.LogError(ex, emailAddress);
      return (false, $"Error searching inbox: {ex.Message}");
    }
  }
}