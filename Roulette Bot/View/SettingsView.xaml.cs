using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Roulette_Bot.View;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();        
    }

    private void Duration_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidDuration(newText);
    }

    private void Duration_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidDuration(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    // Helper methods
    private string GetNewText(TextBox textBox, string insertText)
    {
        int selectionStart = textBox.SelectionStart;
        return textBox.Text.Substring(0, selectionStart) +
               insertText +
               textBox.Text.Substring(selectionStart + textBox.SelectionLength);
    }

    private bool IsValidDuration(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;                    // allow temporarily empty while typing

        return int.TryParse(text, out int value) && value >= 0 && value <= 1440;
    }




    private void Dollar_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidDollar(newText);
    }

    private void Dollar_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidDollar(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    // New validation (only change from Duration is the minimum value)
    private bool IsValidDollar(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;                    // allow temporarily empty while typing

        return int.TryParse(text, out int value) && value >= 1;
    }


    //===========================================================================================================

    private void Btwn1_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidBtwn1(newText);
    }

    private void Btwn1_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidBtwn1(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    // Reuse the exact same validation as Duration (0–1440)
    private bool IsValidBtwn1(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;                    // allow temporarily empty while typing

        return int.TryParse(text, out int value) && value >= 0 && value <= 1440;
    }

    //===========================================================================================================

    private void Btwn2_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidBtwn2(newText);
    }

    private void Btwn2_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidBtwn2(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    // Validation (exactly the same 0–1440 rule)
    private bool IsValidBtwn2(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;                    // allow temporarily empty while typing

        return int.TryParse(text, out int value) && value >= 0 && value <= 1440;
    }

    //===========================================================================================================


    private void FixedEvery_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidFixedEvery(newText);
    }

    private void FixedEvery_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidFixedEvery(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }

    // Validation (exactly the same 0–1440 rule)
    private bool IsValidFixedEvery(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;                    // allow temporarily empty while typing

        return int.TryParse(text, out int value) && value >= 0 && value <= 1440;
    }

    //=============================================================================================


    private void ResetMarkAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var textBox = (TextBox)sender;
        string newText = GetNewText(textBox, e.Text);

        e.Handled = !IsValidDollar(newText);
    }

    private void ResetMarkAmount_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var pastedText = (string)e.DataObject.GetData(typeof(string));
            var textBox = (TextBox)sender;
            string newText = GetNewText(textBox, pastedText);

            if (!IsValidDollar(newText))
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }
}
