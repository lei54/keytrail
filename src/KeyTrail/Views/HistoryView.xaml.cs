using System.Windows;
using System.Windows.Controls;
using KeyTrail.Common;
using KeyTrail.ViewModels;

namespace KeyTrail.Views;

public partial class HistoryView
{
    private bool _initializedPeriod;
    private bool _initializedSlot;

    public HistoryView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (!_initializedPeriod && DataContext is HistoryViewModel)
            {
                DayRadio.IsChecked = true;
                _initializedPeriod = true;
                AllSlotRadio.IsChecked = true;
                _initializedSlot = true;
            }
        };
    }

    private async void Period_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not HistoryViewModel vm || sender is not RadioButton { Tag: string tag })
        {
            return;
        }

        StatsPeriod period = tag switch
        {
            "Week" => StatsPeriod.Week,
            "Month" => StatsPeriod.Month,
            _ => StatsPeriod.Day,
        };
        await vm.SetPeriodAsync(period);
    }

    private async void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryViewModel vm)
        {
            await vm.ShiftAsync(false);
        }
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is HistoryViewModel vm)
        {
            await vm.ShiftAsync(true);
        }
    }

    private async void Slot_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initializedSlot || DataContext is not HistoryViewModel vm ||
            sender is not RadioButton { Tag: string tag } ||
            !int.TryParse(tag, out int slot))
        {
            return;
        }

        await vm.SetSlotFilterAsync(slot);
    }
}
