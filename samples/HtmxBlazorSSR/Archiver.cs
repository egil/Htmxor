namespace HtmxBlazorSSR;

public class Archiver
{
    private static volatile string _archiveStatus = "Waiting";
    private static volatile int _archiveProgress = 0;
    private static readonly Lazy<Archiver> _instance = new Lazy<Archiver>(() => new Archiver());
    private readonly Thread _thread;

    public static Archiver Instance => _instance.Value;

    public string Status => _archiveStatus;

    public double Progress => _archiveProgress / 10;

    public string ArchiveFile => "contacts.json";

    public Archiver()
    {
        _thread = new Thread(() => RunImpl()) { IsBackground = true };
    }

    public void Run()
    {
        if (_archiveStatus == "Waiting")
        {
            _archiveStatus = "Running";
            _archiveProgress = 0;
            _thread.Start();
        }
    }

    public void Reset()
    {
        _archiveStatus = "Waiting";
    }

    private void RunImpl()
    {
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(TimeSpan.FromSeconds(Random.Shared.NextDouble()));
            if (_archiveStatus != "Running")
            {
                return;
            }
            _archiveProgress = i + 1;
            Console.WriteLine("Here... " + _archiveProgress);
        }
        Thread.Sleep(TimeSpan.FromSeconds(1));
        if (_archiveStatus != "Running")
        {
            return;
        }
        _archiveStatus = "Complete";
    }
}
