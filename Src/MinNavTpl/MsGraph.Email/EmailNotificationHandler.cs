using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace MsGraph.Email;

public class EmailNotificationHandler(ILogger _logger, IConfiguration _configuration, GraphServiceClient _graphClient, string NotificationUrl = "https://yourapp.com/api/notifications") // Replace with your actual notification URL
{
    public async Task<(bool success, string report)> CreateSubscriptionAsync()
    {
        try
        {
            var subscription = new Subscription
            {
                ChangeType = "created",
                NotificationUrl = NotificationUrl,
                Resource = "me/mailFolders('inbox')/messages",
                ExpirationDateTime = DateTime.UtcNow.AddMinutes(15), // Subscriptions can be renewed
                ClientState = _configuration["secretClientValue"]
            };

            var createdSubscription = await _graphClient.Subscriptions.PostAsync(subscription);

            return (true, $"Subscription created. Id: {createdSubscription?.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription");
            return (false, $"Error creating subscription: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> RenewSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var subscription = new Subscription
            {
                ExpirationDateTime = DateTime.UtcNow.AddMinutes(15)
            };

            _ = await _graphClient.Subscriptions[subscriptionId].PatchAsync(subscription);

            return (true, "Subscription renewed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription");
            return (false, $"Error renewing subscription: {ex.Message}");
        }
    }

    //public async Task HandleNotification(NotificationCollection notifications)
    //{
    //    foreach (var notification in notifications.Value)
    //    {
    //        if (notification.ResourceData != null)
    //        {
    //            // Process the new email
    //            var messageId = notification.ResourceData.Id;
    //            var message = await _graphClient.Me.Messages[messageId].Request().GetAsync();
    //            // Handle the new email message
    //            _logger.LogInformation($"New email received: {message.Subject} from {message.From?.EmailAddress?.Address}");
    //        }
    //    }
    //}
}
