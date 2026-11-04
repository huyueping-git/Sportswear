using Sportswear.Data;
using Sportswear.Models;

namespace Sportswear.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ShoeDatabase _database;
    private List<Shoe> _all = [];
    private string _filter = "all";
    private Shoe? _selected;

    public MainViewModel(ShoeDatabase database)
    {
        _database = database;
        FooterText = "数据只保存在本机  ·  " + database.DatabasePath;
    }

    public ObservableCollection<Shoe> Shoes { get; } = [];
    public ObservableCollection<RunEntry> Runs { get; } = [];
    public ObservableCollection<RunEntry> AllRuns { get; } = [];

    public string FooterText { get; }
    public string TotalKmText { get; private set; } = "0";
    public string ActiveCountText { get; private set; } = "0";
    public string RunCountText { get; private set; } = "0";
    public int AttentionCount { get; private set; }
    public bool HasAttention { get; private set; }
    public string WeekKmText { get; private set; } = "0";
    public string WeekHint { get; private set; } = "这 7 天还没有新的里程";
    public string AllShoeCountText { get; private set; } = "共 0 双";
    public bool HasShoes { get; private set; }
    public bool ShowShoeList { get; private set; }
    public bool ShowFilterEmpty { get; private set; }
    public bool HasSelection => _selected != null;
    public bool HasRuns { get; private set; }
    public bool ShowRunEmpty { get; private set; }
    public bool HasAnyRuns { get; private set; }
    public bool HasSelectedRun => SelectedRun != null;
    public string RunHeaderText { get; private set; } = "里程记录";
    public string RunLogCountText { get; private set; } = "共 0 条";
    public bool ShowRunView { get; private set; }
    public bool ShowShoeView => !ShowRunView;
    public bool ShowAttentionBanner => HasAttention && ShowShoeView;
    public string ViewCountText => ShowRunView ? RunLogCountText : AllShoeCountText;

    public Shoe? Selected
    {
        get => _selected;
        private set
        {
            _selected = value;
            Raise();
            Raise(nameof(HasSelection));
        }
    }

    private RunEntry? _selectedRun;
    public RunEntry? SelectedRun
    {
        get => _selectedRun;
        set
        {
            if (ReferenceEquals(_selectedRun, value))
                return;

            _selectedRun = value;
            Raise();
            Raise(nameof(HasSelectedRun));
        }
    }

    public IReadOnlyList<Shoe> AllShoes => _all;

    public void SetRunView(bool showRuns)
    {
        if (ShowRunView == showRuns)
            return;

        ShowRunView = showRuns;
        Raise(nameof(ShowRunView));
        Raise(nameof(ShowShoeView));
        Raise(nameof(ShowAttentionBanner));
        Raise(nameof(ViewCountText));
    }

    public bool SetFilter(string filter)
    {
        if (_filter == filter)
            return false;

        _filter = filter;
        return true;
    }

    public void Reload()
    {
        _all = _database.GetShoes().ToList();
        var weekKm = _database.GetKmSince(DateTime.Today.AddDays(-6));

        TotalKmText = Km.Board(_all.Sum(shoe => shoe.TotalKm));
        ActiveCountText = _all.Count(shoe => !shoe.IsRetired).ToString(CultureInfo.InvariantCulture);
        RunCountText = _all.Sum(shoe => shoe.RunCount).ToString(CultureInfo.InvariantCulture);
        AttentionCount = _all.Count(shoe => !shoe.IsRetired && shoe.ProgressRatio >= 0.8);
        HasAttention = AttentionCount > 0;
        WeekKmText = Km.Board(weekKm);
        WeekHint = weekKm > 0 ? "最近 7 天，所有跑鞋加在一起" : "这 7 天还没有新的里程";
        AllShoeCountText = $"共 {_all.Count} 双";
        HasShoes = _all.Count > 0;

        var filtered = Filter(_all).ToList();
        Shoes.Clear();
        foreach (var shoe in filtered)
            Shoes.Add(shoe);

        ShowShoeList = Shoes.Count > 0;
        ShowFilterEmpty = HasShoes && Shoes.Count == 0;

        var runs = _database.GetAllRuns();
        AllRuns.Clear();
        foreach (var run in runs)
            AllRuns.Add(run);

        HasAnyRuns = AllRuns.Count > 0;
        RunLogCountText = $"共 {AllRuns.Count} 条";

        Raise(nameof(TotalKmText));
        Raise(nameof(ActiveCountText));
        Raise(nameof(RunCountText));
        Raise(nameof(AttentionCount));
        Raise(nameof(HasAttention));
        Raise(nameof(WeekKmText));
        Raise(nameof(WeekHint));
        Raise(nameof(AllShoeCountText));
        Raise(nameof(HasShoes));
        Raise(nameof(ShowShoeList));
        Raise(nameof(ShowFilterEmpty));
        Raise(nameof(HasAnyRuns));
        Raise(nameof(RunLogCountText));
        Raise(nameof(ViewCountText));
        Raise(nameof(ShowAttentionBanner));
    }

    public void Select(Shoe? shoe, bool keepRun = false)
    {
        Selected = shoe;
        if (!keepRun)
            SelectedRun = null;
        Runs.Clear();

        if (shoe != null)
        {
            foreach (var run in _database.GetRuns(shoe.Id))
                Runs.Add(run);
        }

        HasRuns = Runs.Count > 0;
        ShowRunEmpty = shoe != null && Runs.Count == 0;
        RunHeaderText = shoe == null ? "里程记录" : $"里程记录  ·  {Runs.Count}";

        Raise(nameof(HasRuns));
        Raise(nameof(ShowRunEmpty));
        Raise(nameof(RunHeaderText));
    }

    private IEnumerable<Shoe> Filter(IEnumerable<Shoe> shoes) => _filter switch
    {
        "active" => shoes.Where(shoe => !shoe.IsRetired),
        "attention" => shoes.Where(shoe => !shoe.IsRetired && shoe.ProgressRatio >= 0.8),
        _ => shoes
    };
}
