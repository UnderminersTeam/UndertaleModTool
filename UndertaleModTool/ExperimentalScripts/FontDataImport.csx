// Made by mono21400

UndertaleFont font = Selected as UndertaleFont;
using (StreamReader reader = new StreamReader("glyphs_"+font.Name.Content + ".csv"))
{
	font.Glyphs.Clear();
	string line;
	while ((line = reader.ReadLine()) != null)
	{
		string[] s = line.Split(';');
		font.Glyphs.Add(new UndertaleFont.Glyph() 
		{
			Character = UInt16.Parse(s[0]),
			SourceX = UInt16.Parse(s[1]),
			SourceY = UInt16.Parse(s[2]),
			SourceWidth = UInt16.Parse(s[3]),
			SourceHeight = UInt16.Parse(s[4]),
			Shift = Int16.Parse(s[5]),
			Offset = Int16.Parse(s[6]),
		});
	}
}