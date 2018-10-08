using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace UndertaleModLib.Models
{
    public class UndertaleTimeline : UndertaleNamedResource, INotifyPropertyChanged
    {
        private UndertaleString _Name;
        private List<Tuple<int, UndertalePointerList<UndertaleGameObject.EventAction>>> _Moments = new List<Tuple<int, UndertalePointerList<UndertaleGameObject.EventAction>>>();

        public UndertaleString Name { get => _Name; set { _Name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name")); } }
        public List<Tuple<int, UndertalePointerList<UndertaleGameObject.EventAction>>> Moments { get => _Moments; set { _Moments = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Moments")); } }

        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);

            writer.Write(Moments.Count);
            for (int i = 0; i < Moments.Count; i++)
            {
                // Write the time point
                writer.Write(Moments[i].Item1);

                // Unnecessary pointer to next array
                writer.WriteUndertaleObjectPointer(Moments[i].Item2);
            }

            for (int i = 0; i < Moments.Count; i++)
            {
                // Write the actions for this moment
                writer.WriteUndertaleObject(Moments[i].Item2);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();

            int momentCount = reader.ReadInt32();

            // Read the time points for each moment
            int[] timePoints = new int[momentCount];
            int[] unnecessaryPointers = new int[momentCount];
            for (int i = 0; i < momentCount; i++)
            {
                timePoints[i] = reader.ReadInt32();
                unnecessaryPointers[i] = reader.ReadInt32();
            }

            // Read the actions for each moment
            for (int i = 0; i < momentCount; i++)
            {
                if (reader.Position != unnecessaryPointers[i])
                    throw new UndertaleSerializationException("Invalid action list pointer");

                // Read action list and assign time point (put into list)
                Moments.Add(new Tuple<int, UndertalePointerList<UndertaleGameObject.EventAction>>(
                    timePoints[i], reader.ReadUndertaleObject<UndertalePointerList<UndertaleGameObject.EventAction>>()));
            }
        }
    }
}
