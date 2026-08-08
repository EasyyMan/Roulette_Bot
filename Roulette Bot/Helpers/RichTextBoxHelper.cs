//File: Roulette Bot\Helpers\RichTextBoxHelper.cs
//````````csharp
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using MVVM_Core.ViewModels;

namespace Roulette_Bot.Helpers;

public static class RichTextBoxHelper
{
    // ─────────────────────────────────────────────────────────────
    // 1. OLD plain-text binding (kept for backward compatibility)
    // ─────────────────────────────────────────────────────────────
    public static readonly DependencyProperty BoundTextProperty =
        DependencyProperty.RegisterAttached(
            "BoundText",
            typeof(string),
            typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata(string.Empty, OnBoundTextChanged));

    public static string GetBoundText(DependencyObject obj)
        => (string)obj.GetValue(BoundTextProperty);

    public static void SetBoundText(DependencyObject obj, string value)
        => obj.SetValue(BoundTextProperty, value);

    private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RichTextBox rtb)
        {
            rtb.Document.Blocks.Clear();
            if (e.NewValue is string newText && !string.IsNullOrEmpty(newText))
            {
                rtb.AppendText(newText);
                rtb.ScrollToEnd();
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 2. NEW formatted log binding (color + font size per line)
    // ─────────────────────────────────────────────────────────────
    public static readonly DependencyProperty LogEntriesProperty =
        DependencyProperty.RegisterAttached(
            "LogEntries",
            typeof(ObservableCollection<LogEntry>),
            typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata(null, OnLogEntriesChanged));

    public static ObservableCollection<LogEntry> GetLogEntries(DependencyObject obj)
        => (ObservableCollection<LogEntry>)obj.GetValue(LogEntriesProperty);

    public static void SetLogEntries(DependencyObject obj, ObservableCollection<LogEntry> value)
        => obj.SetValue(LogEntriesProperty, value);

    private static void OnLogEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RichTextBox rtb) return;

        rtb.Document.Blocks.Clear();

        if (e.NewValue is ObservableCollection<LogEntry> collection)
        {
            // Add any items that already exist
            foreach (var entry in collection)
                AppendLogEntry(rtb, entry);

            // Listen for new lines being added (very efficient for logs)
            collection.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (LogEntry newEntry in args.NewItems!)
                        AppendLogEntry(rtb, newEntry);
                }
                else if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    rtb.Document.Blocks.Clear();
                }
            };
        }
    }

    private static void AppendLogEntry(RichTextBox rtb, LogEntry entry)
    {
        if (!rtb.Dispatcher.CheckAccess())
        {
            rtb.Dispatcher.BeginInvoke(() => AppendLogEntry(rtb, entry));
            return;
        }

        var paragraph = new Paragraph();

        var messageRun = new Run(entry.Text)
        {
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(entry.Foreground)),
            FontSize = entry.FontSize,
            FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(entry.FontWeight)
        };
        paragraph.Inlines.Add(messageRun);
        paragraph.Inlines.Add(new LineBreak());

        if (!string.IsNullOrEmpty(entry.Timestamp))
        {
            var timeRun = new Run("  " + entry.Timestamp)
            {
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(entry.TimestampForeground)),
                FontSize = entry.TimestampFontSize,
                FontWeight = FontWeights.Normal
            };
            paragraph.Inlines.Add(timeRun);
        }

        rtb.Document.Blocks.Add(paragraph);
        rtb.ScrollToEnd();
    }
}