// Replaces the debug mode "Create system_information_962" option with "Go to room"

if (Data.Functions.ByName("get_integer") == null)
{
	Data.Functions.Add(new UndertaleFunction() {
		Name = Data.Strings.MakeString("get_integer"),
		UnknownChainEndingValue = 0 // TODO
	});
}

var code = Data.Code.ByName("gml_Object_obj_time_KeyPress_114");
code.Instructions.Clear();

code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushVar,
	Type1 = UndertaleInstruction.DataType.Variable,
	Value = new UndertaleInstruction.Reference<UndertaleVariable>(Data.Variables[23], UndertaleInstruction.VariableType.Normal)
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Push,
	Type1 = UndertaleInstruction.DataType.String,
	Value = new UndertaleResourceById<UndertaleString>("STRG") { Resource = Data.Strings.MakeString("Go to room") }
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.String,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("get_integer")),
	ArgumentsCount = 2
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("room_goto")),
	ArgumentsCount = 1
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Popz,
	Type1 = UndertaleInstruction.DataType.Int32
});