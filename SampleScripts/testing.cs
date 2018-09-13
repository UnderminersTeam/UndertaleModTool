// Adds a text on the main menu! :D

if (Data == null)
	throw new Exception("Please load data.win first!");

var code = Data.Code.ByName("gml_Object_obj_intromenu_Draw_0");
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Push,
	Type1 = UndertaleInstruction.DataType.String,
	Value = new UndertaleResourceById<UndertaleString>("STRG") { Resource = Data.Strings.MakeString("This is a test#I can put whatever I want here now#hehe, this thing is working!") }
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.String,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushI,
	Type1 = UndertaleInstruction.DataType.Int16,
	Value = (short)50
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushI,
	Type1 = UndertaleInstruction.DataType.Int16,
	Value = (short)200
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("scr_drawtext_centered")),
	ArgumentsCount = 3
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Popz,
	Type1 = UndertaleInstruction.DataType.Variable
});
Selected = code;