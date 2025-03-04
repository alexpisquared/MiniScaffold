using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
//using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Models;

namespace MsGraph.Email;

public class EmailNotificationHandler : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly GraphServiceClient _graphClient;
    private readonly Dispatcher _dispatcher;
    private CancellationTokenSource? _monitoringCts;
    private string _status = "Ready";
    private bool _isMonitoring;
    private ObservableCollection<EmailMessage> _recentEmails = new();
    private string? _currentSubscriptionId;
    private Timer? _subscriptionRenewalTimer;

    public EmailNotificationHandler(ILogger logger, IConfiguration configuration, GraphServiceClient graphClient)
    {
        _logger = logger;
        _configuration = configuration;
        _graphClient = graphClient;
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Event for new email notifications
    public event EventHandler<EmailNotificationEventArgs>? NewEmailReceived;

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        private set => SetProperty(ref _isMonitoring, value);
    }

    public ObservableCollection<EmailMessage> RecentEmails
    {
        get => _recentEmails;
        private set => SetProperty(ref _recentEmails, value);
    }

    public string? CurrentSubscriptionId
    {
        get => _currentSubscriptionId;
        private set => SetProperty(ref _currentSubscriptionId, value);
    }

    public async Task<(bool success, string report)> CreateSubscriptionAsync()
    {
        try
        {
            Status = "Starting polling-based monitoring...";
            
            // Since we're using a WPF application, we'll use polling instead of webhooks
            // Start monitoring with default parameters
            var result = await StartMonitoringAsync();
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting email monitoring");
            Status = $"Error: {ex.Message}";
            return (false, $"Error starting email monitoring: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> RenewSubscriptionAsync(string subscriptionId)
    {
        try
        {
            Status = "Renewing subscription...";

            var subscription = new Subscription
            {
                ExpirationDateTime = DateTime.UtcNow.AddMinutes(15)
            };

            _ = await _graphClient.Subscriptions[subscriptionId].PatchAsync(subscription);

            Status = "Subscription renewed successfully.";
            return (true, "Subscription renewed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription");
            Status = $"Error: {ex.Message}";
            return (false, $"Error renewing subscription: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> StartMonitoringAsync(string? emailFilter = null, int pollingIntervalSeconds = 10)
    {
        if (IsMonitoring)
            return (false, "Already monitoring for new emails.");

        try
        {
            _monitoringCts = new CancellationTokenSource();
            IsMonitoring = true;
            Status = $"Monitoring for new emails" + (emailFilter != null ? $" matching '{emailFilter}'" : "");

            await Task.Run(async () =>
            {
                var lastCheckTime = DateTimeOffset.Now;

                while (!_monitoringCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var messagesResponse = await _graphClient.Me.MailFolders["inbox"].Messages.GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Top = 25;
                            requestConfiguration.QueryParameters.Select = ["subject", "receivedDateTime", "from", "id", "bodyPreview"];
                            requestConfiguration.QueryParameters.Filter = $"receivedDateTime gt {lastCheckTime.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";
                        }, _monitoringCts.Token);

                        if (messagesResponse?.Value != null && messagesResponse.Value.Count > 0)
                        {
                            // Update check time
                            lastCheckTime = DateTimeOffset.Now;

                            // Filter messages if needed
                            var matchingMessages = string.IsNullOrEmpty(emailFilter)
                                ? messagesResponse.Value
                                : messagesResponse.Value.Where(msg =>
                                    (msg.From?.EmailAddress?.Address?.Contains(emailFilter, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (msg.Subject?.Contains(emailFilter, StringComparison.OrdinalIgnoreCase) == true)
                                ).ToList();

                            if (matchingMessages.Any())
                            {
                                _dispatcher.Invoke(() =>
                                {
                                    foreach (var msg in matchingMessages)
                                    {
                                        var emailMessage = new EmailMessage
                                        {
                                            Id = msg.Id ?? "",
                                            Subject = msg.Subject ?? "(No subject)",
                                            From = msg.From?.EmailAddress?.Address ?? "",
                                            FromDisplayName = msg.From?.EmailAddress?.Name ?? "",
                                            ReceivedTime = msg.ReceivedDateTime?.DateTime ?? DateTime.Now,
                                            Preview = msg.BodyPreview ?? ""
                                        };

                                        RecentEmails.Insert(0, emailMessage);

                                        // Raise event
                                        NewEmailReceived?.Invoke(this, new EmailNotificationEventArgs(emailMessage));
                                    }

                                    Status = $"Last check: {DateTime.Now} - Found {matchingMessages.Count()} new emails";
                                });
                            }
                        }

                        await Task.Delay(TimeSpan.FromSeconds(pollingIntervalSeconds), _monitoringCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while polling for emails");
                        _dispatcher.Invoke(() => Status = $"Error while polling: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(pollingIntervalSeconds * 2, 60)), _monitoringCts.Token);
                    }
                }
            }, _monitoringCts.Token);

            return (true, "Email monitoring started successfully.");
        }
        catch (Exception ex)
        {
            IsMonitoring = false;
            Status = $"Error: {ex.Message}";
            _logger.LogError(ex, "Error starting email monitoring");
            return (false, $"Error starting email monitoring: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> StopMonitoringAsync()
    {
        if (!IsMonitoring || _monitoringCts == null)
            return (true, "Not currently monitoring.");

        try
        {
            _monitoringCts.Cancel();
            IsMonitoring = false;
            Status = "Monitoring stopped.";
            return (true, "Email monitoring stopped successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping email monitoring");
            Status = $"Error: {ex.Message}";
            return (false, $"Error stopping email monitoring: {ex.Message}");
        }
    }

    public async Task<(bool success, string report)> HandleNotification(NotificationCollection notifications)
    {
        try
        {
            if (notifications?.Value == null || !notifications.Value.Any())
                return (false, "No notifications provided.");

            foreach (var notification in notifications.Value)
            {
                if (notification.ResourceData != null)
                {
                    try
                    {
                        // Process the new email
                        var messageId = notification.ResourceData.Id;
                        if (string.IsNullOrEmpty(messageId))
                            continue;

                        var message = await _graphClient.Me.Messages[messageId].GetAsync();
                        if (message == null)
                            continue;

                        var emailMessage = new EmailMessage
                        {
                            Id = message.Id ?? "",
                            Subject = message.Subject ?? "(No subject)",
                            From = message.From?.EmailAddress?.Address ?? "",
                            FromDisplayName = message.From?.EmailAddress?.Name ?? "",
                            ReceivedTime = message.ReceivedDateTime?.DateTime ?? DateTime.Now,
                            Preview = message.BodyPreview ?? ""
                        };

                        _dispatcher.Invoke(() =>
                        {
                            RecentEmails.Insert(0, emailMessage);
                            NewEmailReceived?.Invoke(this, new EmailNotificationEventArgs(emailMessage));
                        });

                        _logger.LogInformation($"New email received: {message.Subject} from {message.From?.EmailAddress?.Address}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing notification message ID: {notification.ResourceData.Id}");
                    }
                }
            }

            return (true, $"Successfully processed {notifications.Value.Count()} notifications.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling notifications");
            return (false, $"Error handling notifications: {ex.Message}");
        }
    }

    private void SetupSubscriptionRenewal(string? subscriptionId)
    {
        if (string.IsNullOrEmpty(subscriptionId))
            return;

        // Dispose any existing timer
        _subscriptionRenewalTimer?.Dispose();

        // Create renewal timer - renew every 10 minutes
        _subscriptionRenewalTimer = new Timer(async _ =>
        {
            await RenewSubscriptionAsync(subscriptionId);
        }, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;

        _dispatcher.InvokeAsync(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }, DispatcherPriority.DataBind);
    }

    public void Dispose()
    {
        _monitoringCts?.Cancel();
        _subscriptionRenewalTimer?.Dispose();
        _monitoringCts?.Dispose();
    }
}

public class NotificationCollection
{
    public IEnumerable<Notification> Value
    {
        get;
        internal set;
    }
}

public class Notification
{
    public ResourceData ResourceData
    {
        get;
        internal set;
    }
}

public class ResourceData : Dictionary<string, object>
{
    public string Id
    {
        get;
        internal set;
    }
}

