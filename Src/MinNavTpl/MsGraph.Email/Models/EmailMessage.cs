using System;

namespace MsGraph.Email;

public class EmailMessage
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    public DateTime ReceivedTime { get; set; }
    public string Preview { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool HasAttachments { get; set; }
    
    public override string ToString() => 
        $"{ReceivedTime:yyyy-MM-dd HH:mm} - {FromDisplayName} <{From}>: {Subject}";
}
