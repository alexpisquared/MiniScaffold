using System;

namespace MsGraph.Email;

public class EmailNotificationEventArgs : EventArgs
{
    public EmailMessage Email { get; }
    
    public EmailNotificationEventArgs(EmailMessage email)
    {
        Email = email;
    }
}
