String Text = ScriptTextInput("This is the prompt.", "This is the form title.", "This is a single text line input box test.", false);
ScriptMessage(Text);
Text = ScriptTextInput("This is the prompt.", "This is the form title.", "This is the default value string in the text box.\r\nNotice how you can have multiple lines?\r\nNotice how you can have multiple lines?", true);
ScriptMessage(Text);
