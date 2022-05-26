// WPF Text Input Dialog Box test script.

//    return ScriptInputDialog(titleText, labelText, defaultInputBoxText, cancelButtonText, submitButtonText, isMultiline, preventClose);
String Text = ScriptInputDialog("Title: Text Input Test 01", "Label: Multiline off, Prevent close off", "Default input here.", "Cancel Button", "Submit Button", false, false);
DisplayScriptMessage(Text);
Text = ScriptInputDialog("Title: Text Input Test 02", "Label: Multiline on, Prevent close off", "Default input here.\r\nDefault input here.\r\nDefault input here.", "Cancel Button", "Submit Button", true, false);
DisplayScriptMessage(Text);
Text = ScriptInputDialog("Title: Text Input Test 03", "Label: Multiline off, Prevent close on", "Default input here.", "Cancel Button", "Submit Button", false, true);
DisplayScriptMessage(Text);
Text = ScriptInputDialog("Title: Text Input Test 04", "Label: Multiline on, Prevent close on", "Default input here.\r\nDefault input here.\r\nDefault input here.", "Cancel Button", "Submit Button", true, true);
DisplayScriptMessage(Text);

void DisplayScriptMessage(string Text)
{
    if (Text != null)
    {
        ScriptMessage(Text);
        return;
    }
    ScriptMessage("Dialog was cancelled or closed.");
}