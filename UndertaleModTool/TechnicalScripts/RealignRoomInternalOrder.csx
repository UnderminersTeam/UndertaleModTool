Data.GeneralInfo.RoomOrder.Clear();
for (int i = 0; i < Data.Rooms.Count; i++)
{
    Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = Data.Rooms[i] });
}
