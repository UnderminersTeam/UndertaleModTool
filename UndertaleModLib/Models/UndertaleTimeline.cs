using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace UndertaleModLib.Models
{
    /// <summary>
    /// A timeline in a data file.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class UndertaleTimeline : UndertaleNamedResource
    {
        /// <summary>
        /// A specific moment in a timeline.
        /// </summary>
        [PropertyChanged.AddINotifyPropertyChangedInterface]
        public class UndertaleTimelineMoment : UndertaleObject
        {
            /// <summary>
            /// After how many steps this moment gets executed.
            /// </summary>
            public uint Step { get; set; }

            /// <summary>
            /// The actions that get executed at this moment.
            /// </summary>
            public UndertalePointerList<UndertaleGameObject.EventAction> Event { get; set; }

            /// <summary>
            /// Initializes a new empty instance of the <see cref="UndertaleTimelineMoment"/> class.
            /// </summary>
            public UndertaleTimelineMoment()
            {
                /*
                Step = 0;
                Event = new UndertalePointerList<UndertaleGameObject.EventAction>();
                */
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="UndertaleTimelineMoment"/> with the specified step time and event action list.
            /// </summary>
            /// <param name="step">After how many steps the moment shall be executed.</param>
            /// <param name="events">A list of events that shall be executed.</param>
            public UndertaleTimelineMoment(uint step, UndertalePointerList<UndertaleGameObject.EventAction> events)
            {
                Step = step;
                Event = events;
            }

            public void Serialize(UndertaleWriter writer)
            {
                // Since GM:S stores Steps first, and then Events, we can't serialize a single entry in a single function :(
            }

            public void Unserialize(UndertaleReader reader)
            {
                // Same goes for unserializing.
            }
        }

        /// <summary>
        /// The name of the timeline.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <summary>
        /// The moments this timeline has. Comparable to keyframes.
        /// </summary>
        public ObservableCollection<UndertaleTimelineMoment> Moments { get; set; } = new ObservableCollection<UndertaleTimelineMoment>();

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
                writer.Write(Moments[i].Step);

                // Unnecessary pointer to next array
                writer.WriteUndertaleObjectPointer(Moments[i].Event);
            }

            for (int i = 0; i < Moments.Count; i++)
            {
                // Write the actions for this moment
                writer.WriteUndertaleObject(Moments[i].Event);
            }
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();

            int momentCount = reader.ReadInt32();

            // Read the time points for each moment
            uint[] timePoints = new uint[momentCount];
            int[] unnecessaryPointers = new int[momentCount];
            for (int i = 0; i < momentCount; i++)
            {
                timePoints[i] = reader.ReadUInt32();
                unnecessaryPointers[i] = reader.ReadInt32();
            }

            // Read the actions for each moment
            for (int i = 0; i < momentCount; i++)
            {
                if (reader.Position != unnecessaryPointers[i])
                    throw new UndertaleSerializationException("Invalid action list pointer");

                // Read action list and assign time point (put into list)
                var timeEvent = reader.ReadUndertaleObject<UndertalePointerList<UndertaleGameObject.EventAction>>();
                Moments.Add(new UndertaleTimelineMoment(timePoints[i], timeEvent));
            }
        }
    }
}
