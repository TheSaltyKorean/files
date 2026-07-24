using System.Threading;
using System.Windows;
using QuickFiles.Services;
using Velopack;

namespace QuickFiles;

public partial class App : Application
{
    private const string MutexName = "QuickFiles.SingleInstance";
    private const string ShowSignalName = "QuickFiles.ShowFlyout";

    private Mutex? _instanceMutex;
    private EventWaitHandle? _showSignal;
    private MainWindow? _flyout;
    private bool _startHidden;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var mutex = new Mutex(true, MutexName, out bool isFirstInstance);
        var signal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSignalName);

        if (!isFirstInstance)
        {
            // Another instance is already running (e.g. the user clicked the pinned
            // taskbar icon again): tell it to show the flyout and exit.
            signal.Set();
            return;
        }

        var app = new App
        {
            _instanceMutex = mutex,
            _showSignal = signal,
            _startHidden = args.Contains("--hidden"),
        };
        app.InitializeComponent();
        app.Run();
    }

    public bool IsFlyoutVisible => _flyout?.IsVisible == true;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _flyout = new MainWindow();
        if (!_startHidden)
            _flyout.ShowFlyout();

        StartShowSignalListener();
        UpdateService.Start(this);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void StartShowSignalListener()
    {
        var thread = new Thread(() =>
        {
            while (true)
            {
                _showSignal!.WaitOne();
                Dispatcher.BeginInvoke(() => _flyout?.ShowFlyout());
            }
        })
        {
            IsBackground = true,
            Name = "QuickFiles.ShowSignalListener",
        };
        thread.Start();
    }
}
