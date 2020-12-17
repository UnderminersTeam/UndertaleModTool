// WPF Text Input Dialog Box test script.

string input(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose) {
	return ScriptInputDialog(titleText, labelText, defaultInputBoxText, cancelButtonText, submitButtonText, isMultiline, preventClose);
}

void show(string msg) {
	ScriptMessage(msg ?? "<dialog was cancelled or closed>");
}

// options.
string titleText = "A title",
       labelText = "A Label:",
       defaultInputBoxText = "default input here.",
       cancelButtonText = "A Cancel Button",
       submitButtonText = "A Submit Button";
bool   isMultiline = false,
       preventClose = false;

show(input(titleText, labelText, defaultInputBoxText, cancelButtonText, submitButtonText, isMultiline, preventClose));