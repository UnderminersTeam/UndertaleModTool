// Adds room ID and name display under the debug mode timer display

if (Data == null)
	throw new Exception("Please load data.win first!");

MessageBox.Show("Show room name and ID in debug mode\nby krzys_h", "ShowRoomName", MessageBoxButton.OK, MessageBoxImage.Information);

// TODO: enable only if in debug mode

var code = Data.Code.ByName("gml_Object_obj_time_Draw_64");

code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Push,
	Type1 = UndertaleInstruction.DataType.Int32,
	Value = (int)0xFFFF00
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("draw_set_color")),
	ArgumentsCount = 1
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Popz,
	Type1 = UndertaleInstruction.DataType.Variable
});

code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushVar,
	Type1 = UndertaleInstruction.DataType.Variable,
	//Value = new UndertaleInstruction.Reference<UndertaleVariable>(Data.Variables.ByName("room"))
	Value = new UndertaleInstruction.Reference<UndertaleVariable>(Data.Variables[23], UndertaleInstruction.VariableType.Normal)
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushI,
	Type1 = UndertaleInstruction.DataType.Int16,
	Value = (short)30
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushI,
	Type1 = UndertaleInstruction.DataType.Int16,
	Value = (short)10
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
	Type2 = UndertaleInstruction.DataType.Variable
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("draw_text")),
	ArgumentsCount = 3
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Popz,
	Type1 = UndertaleInstruction.DataType.Variable
});

code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushVar,
	Type1 = UndertaleInstruction.DataType.Variable,
	//Value = new UndertaleInstruction.Reference<UndertaleVariable>(Data.Variables.ByName("room"))
	Value = new UndertaleInstruction.Reference<UndertaleVariable>(Data.Variables[23], UndertaleInstruction.VariableType.Normal)
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("room_get_name")),
	ArgumentsCount = 1
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.PushI,
	Type1 = UndertaleInstruction.DataType.Int16,
	Value = (short)30
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Conv,
	Type1 = UndertaleInstruction.DataType.Int32,
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
	Kind = UndertaleInstruction.Opcode.Call,
	Type1 = UndertaleInstruction.DataType.Int32,
	Function = new UndertaleInstruction.Reference<UndertaleFunction>(Data.Functions.ByName("draw_text")),
	ArgumentsCount = 3
});
code.Instructions.Add(new UndertaleInstruction() {
	Kind = UndertaleInstruction.Opcode.Popz,
	Type1 = UndertaleInstruction.DataType.Variable
});

Selected = code;
MessageBox.Show("Patched!", "ShowRoomName", MessageBoxButton.OK, MessageBoxImage.Information);