// Enables debug mode

if (Data == null)
	throw new Exception("Please load data.win first!");

MessageBox.Show("Debug mode enabler\nby krzys_h", "EnableDebug", MessageBoxButton.OK, MessageBoxImage.Information);

bool ok = false;
var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART")?.Code;
if (SCR_GAMESTART == null)
	throw new Exception("Script SCR_GAMESTART not found");
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++)
{
	if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].Destination.Target.Name.Content == "debug")
	{
		ok = true;
		MessageBoxResult result = MessageBox.Show("Enable or disable?", "EnableDebug", MessageBoxButton.YesNo, MessageBoxImage.Question);
		SCR_GAMESTART.Instructions[i-1].Value = (short)(result == MessageBoxResult.Yes ? 1 : 0);
		MessageBox.Show("Debug mode " + (result == MessageBoxResult.Yes ? "enabled" : "disabled"), "EnableDebug", MessageBoxButton.OK, MessageBoxImage.Information);
	}
}
if (!ok)
	throw new Exception("Patch point not found?");
Selected = SCR_GAMESTART;