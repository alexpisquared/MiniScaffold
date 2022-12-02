namespace QStatsTS4WinUI.ViewModels;

public partial class DataGrid1ViewModel : ObservableRecipient, INavigationAware
{
    private readonly QstatsRlsContext Dbx;

    public ObservableCollection<Email> Source { get; } = new ObservableCollection<Email>();

    public DataGrid1ViewModel(QstatsRlsContext qstatsRlsContext)
    {
        Dbx = qstatsRlsContext;
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        var data = await Dbx.Emails.OrderByDescending(e => e.AddedAt).Take(100).ToListAsync();

        foreach (var item in data)
        {
            Source.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}