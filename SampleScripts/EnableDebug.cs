// Enables debug mode

var SCR_GAMESTART = Data.Scripts.ByName("SCR_GAMESTART").Code;
for(int i = 0; i < SCR_GAMESTART.Instructions.Count; i++)
	if (SCR_GAMESTART.Instructions[i].Kind == UndertaleInstruction.Opcode.Pop && SCR_GAMESTART.Instructions[i].Destination.Target.Name.Content == "debug")
		SCR_GAMESTART.Instructions[i-1].Value = (short)1;