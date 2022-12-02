using Microsoft.Extensions.Hosting;

namespace QStatsTS4WinUI.Views;

public interface IApp
{
    IHost Host
    {
        get;
    }
}
