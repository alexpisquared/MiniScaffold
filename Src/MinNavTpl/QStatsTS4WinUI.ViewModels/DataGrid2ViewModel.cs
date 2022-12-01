using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using QStatsTS4WinUI.Contracts.ViewModels;
using QStatsTS4WinUI.Core.Contracts.Services;
using QStatsTS4WinUI.Core.Models;

namespace QStatsTS4WinUI.ViewModels;

public class DataGrid2ViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public DataGrid2ViewModel(ISampleDataService sampleDataService)
    {
        _sampleDataService = sampleDataService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        // TODO: Replace with real data.
        var data = await _sampleDataService.GetGridDataAsync();

        foreach (var item in data)
        {
            Source.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
