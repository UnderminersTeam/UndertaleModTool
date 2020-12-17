// Text Input Dialog Box test script.

string input(bool isMultiline = false, bool preventClose = false, string title = "Input Dialog", string label = "Input:", string button = "Submit", string cancelButton = "Cancel", string def = "") {
	return ScriptInputDialog(isMultiline, preventClose, title, label, button, cancelButton, def);
}

void show(string msg) {
	ScriptMessage(msg is null ? "<dialog was cancelled>" : msg);
}

show(input());