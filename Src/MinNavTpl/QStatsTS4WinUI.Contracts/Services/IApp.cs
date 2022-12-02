using Microsoft.Extensions.Hosting;

namespace QStatsTS4WinUI.Contracts.Services;

public interface IApp
{
    IHost Host
    {
        get;
    }
}
