namespace UndertaleModLib.Models
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleFilterEffect : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public UndertaleString Value { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Value);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Value = reader.ReadUndertaleString();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
