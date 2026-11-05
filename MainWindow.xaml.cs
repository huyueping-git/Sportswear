using System.Diagnostics;
using Sportswear.Data;
using Sportswear.Models;
using Sportswear.ViewModels;
using Sportswear.Views;

namespace Sportswear;

public partial class MainWindow : SportWindow
{
    private readonly ShoeDatabase _database;
    private readonly MainViewModel _viewModel;
    private bool _ready;
    private bool _sync;

    public MainWindow()
    {
        InitializeComponent();
        _database = new ShoeDatabase();
        _viewModel = new MainViewModel(_database);
        DataContext = _viewModel;
        Reload();
        _ready = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            AddShoe_Click(this, e);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.M)
        {
            AddRun_Click(this, e);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void Filter_Checked(object sender, RoutedEventArgs e)
    {
        if (!_ready || sender is not RadioButton { IsChecked: true, Tag: string tag })
            return;

        if (!_viewModel.SetFilter(tag))
            return;

        Reload();
    }

    private void RunShoeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _sync || RunShoeBox.SelectedItem is not ShoePick pick)
            return;

        var selectedRunId = _viewModel.SelectedRun?.Id;
        if (!_viewModel.SetRunShoeFilter(pick.Id))
            return;

        if (pick.Id != 0)
        {
            var shoe = _viewModel.AllShoes.FirstOrDefault(item => item.Id == pick.Id);
            if (shoe != null)
            {
                _viewModel.Select(shoe, keepRun: true);
                var card = _viewModel.Shoes.FirstOrDefault(item => item.Id == pick.Id);
                if (card != null && !ReferenceEquals(ShoeList.SelectedItem, card))
                {
                    _sync = true;
                    ShoeList.SelectedItem = card;
                    _sync = false;
                }
            }
        }

        var listed = selectedRunId is long id
            ? _viewModel.AllRuns.FirstOrDefault(run => run.Id == id)
            : null;
        if (listed != null)
        {
            FocusRun(listed.Id);
            return;
        }

        _sync = true;
        LogList.SelectedItem = null;
        _sync = false;
        _viewModel.SelectedRun = null;
    }

    private void View_Checked(object sender, RoutedEventArgs e)
    {
        if (!_ready || sender is not RadioButton { IsChecked: true, Tag: string tag })
            return;

        var showRuns = tag == "runs";
        if (_viewModel.ShowRunView == showRuns)
            return;

        _viewModel.SetRunView(showRuns);
        if (showRuns)
            FocusRun(_viewModel.SelectedRun?.Id ?? _viewModel.AllRuns.FirstOrDefault()?.Id);
    }

    private void ShoeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sync)
            return;

        _viewModel.Select(ShoeList.SelectedItem as Shoe);
    }

    private void ShoeList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ShoeList.SelectedItem is Shoe)
            EditShoe_Click(sender, e);
    }

    private void AddShoe_Click(object sender, RoutedEventArgs e)
    {
        var editor = new ShoeEditorWindow { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not Shoe shoe)
            return;

        var id = _database.AddShoe(shoe);
        Reload(id);
    }

    private void EditShoe_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Selected is not Shoe shoe)
            return;

        var editor = new ShoeEditorWindow(shoe) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not Shoe updated)
            return;

        _database.UpdateShoe(updated);
        Reload(updated.Id);
    }

    private void ToggleRetired_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Selected is not Shoe shoe)
            return;

        _database.SetRetired(shoe.Id, !shoe.IsRetired);
        Reload(shoe.Id);
    }

    private void DeleteShoe_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Selected is not Shoe shoe)
            return;

        var message = shoe.RunCount > 0
            ? $"删除「{shoe.Name}」及其 {shoe.RunCount} 条里程记录？此操作无法撤销。"
            : $"删除「{shoe.Name}」？此操作无法撤销。";

        if (!AlertWindow.Confirm(this, "删除跑鞋", message, "删除"))
            return;

        _database.DeleteShoe(shoe.Id);
        Reload();
    }

    private void AddRun_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.AllShoes.Count == 0)
        {
            AlertWindow.Info(this, "还没有跑鞋", "先添加一双跑鞋，再记录里程。");
            return;
        }

        var preferredShoeId = _viewModel.ShowRunView && _viewModel.RunShoeFilterId != 0
            ? _viewModel.RunShoeFilterId
            : _viewModel.Selected?.Id;
        var editor = new RunEditorWindow(_viewModel.AllShoes, preferredShoeId) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not RunEntry run)
            return;

        var id = _database.AddRun(run);
        _viewModel.SetRunView(true);
        if (RunViewRadio.IsChecked != true)
            RunViewRadio.IsChecked = true;
        Reload(run.ShoeId, id);
    }

    private void EditRun_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedRun is not RunEntry run)
            return;

        var editor = new RunEditorWindow(_viewModel.AllShoes, run.ShoeId, run) { Owner = this };
        if (editor.ShowDialog() != true || editor.Result is not RunEntry updated)
            return;

        _database.UpdateRun(updated);
        Reload(updated.ShoeId, updated.Id);
    }

    private void DeleteListedRun_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: RunEntry run })
            return;

        e.Handled = true;
        DeleteRun(run);
    }

    private void DeleteRun_Click(object sender, RoutedEventArgs e)
    {
        var run = _viewModel.SelectedRun;
        if (sender is ListBox { SelectedItem: RunEntry selected })
            run = selected;

        if (run == null)
            return;

        DeleteRun(run);
    }

    private void DeleteRun(RunEntry run)
    {
        if (!AlertWindow.Confirm(this, "删除记录", $"删除 {run.DateText} 的 {run.DistanceText} km 记录？跑鞋会留下，只减去这次里程。", "删除"))
            return;

        var shoeId = run.ShoeId;
        var nextRunId = _viewModel.AllRuns.FirstOrDefault(item => item.Id != run.Id && item.ShoeId == shoeId)?.Id
                        ?? _viewModel.AllRuns.FirstOrDefault(item => item.Id != run.Id)?.Id;
        _database.DeleteRun(run.Id);
        Reload(shoeId, nextRunId);
    }

    private void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_sync || LogList.SelectedItem is not RunEntry run)
            return;

        FocusRun(run.Id);
    }

    private void LogList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LogList.SelectedItem is RunEntry)
            EditRun_Click(sender, e);
    }

    private void RunList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedRun != null)
            EditRun_Click(sender, e);
    }

    private void RunList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeleteRun_Click(sender, e);
            e.Handled = true;
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
    {
        var directory = Path.GetDirectoryName(_database.DatabasePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private void Reload(long? selectId = null, long? selectRunId = null)
    {
        var keep = selectId ?? _viewModel.Selected?.Id;
        var keepRun = selectRunId ?? (_viewModel.ShowRunView ? _viewModel.SelectedRun?.Id : null);
        _sync = true;
        _viewModel.Reload(keepRun);
        var next = _viewModel.Shoes.FirstOrDefault(shoe => shoe.Id == keep)
                   ?? _viewModel.Shoes.FirstOrDefault();
        ShoeList.SelectedItem = next;
        RunShoeBox.SelectedItem = _viewModel.RunShoeOptions.FirstOrDefault(item => item.Id == _viewModel.RunShoeFilterId)
                                  ?? _viewModel.RunShoeOptions.FirstOrDefault();
        _sync = false;
        _viewModel.Select(next);
        if (next != null)
            ShoeList.ScrollIntoView(next);

        if (_viewModel.ShowRunView)
            FocusRun(keepRun);
        else if (selectRunId is long runId)
            _viewModel.SelectedRun = _viewModel.Runs.FirstOrDefault(run => run.Id == runId);
    }

    private void FocusRun(long? runId)
    {
        if (runId is not long id)
            return;

        var listed = _viewModel.AllRuns.FirstOrDefault(run => run.Id == id);
        if (listed == null)
            return;

        _sync = true;
        LogList.SelectedItem = listed;
        LogList.ScrollIntoView(listed);
        var shoe = _viewModel.AllShoes.FirstOrDefault(item => item.Id == listed.ShoeId);
        if (shoe != null && _viewModel.Selected?.Id != shoe.Id)
            _viewModel.Select(shoe, keepRun: true);

        var card = _viewModel.Shoes.FirstOrDefault(item => item.Id == listed.ShoeId);
        if (card != null && !ReferenceEquals(ShoeList.SelectedItem, card))
            ShoeList.SelectedItem = card;
        _sync = false;
        _viewModel.SelectedRun = _viewModel.Runs.FirstOrDefault(run => run.Id == id) ?? listed;
    }
}
