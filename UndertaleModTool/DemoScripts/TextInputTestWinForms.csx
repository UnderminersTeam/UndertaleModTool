//SimpleTextInput(string titleText, string labelText, string defaultInputBoxText, bool isMultiline)
String Text = SimpleTextInput("This is the form title.", "This is the label prompt.", "This is a single text line input box test.", false);
ScriptMessage(Text);
Text = SimpleTextInput("This is the form title.", "This is the label prompt.", "This is the default value string in the text box.\r\nNotice how you can have multiple lines?\r\nNotice how you can have multiple lines?", true);
ScriptMessage(Text);
SimpleTextOutput("This is the form title.", "This is the label prompt.", "This is the text in a read-only text box.", false);
