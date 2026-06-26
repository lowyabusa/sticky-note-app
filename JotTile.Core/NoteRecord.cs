using System.Runtime.Serialization;

namespace JotTile.Core
{
    [DataContract]
    internal sealed class NoteRecord
    {
        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; } = string.Empty;

        [DataMember(Name = "text", Order = 1)]
        public string Text { get; set; } = string.Empty;

        [DataMember(Name = "x", Order = 2)]
        public int X { get; set; }

        [DataMember(Name = "y", Order = 3)]
        public int Y { get; set; }

        [DataMember(Name = "width", Order = 4)]
        public int Width { get; set; }

        [DataMember(Name = "height", Order = 5)]
        public int Height { get; set; }

        [DataMember(Name = "createdAt", Order = 6)]
        public string CreatedAt { get; set; } = string.Empty;

        [DataMember(Name = "updatedAt", Order = 7)]
        public string UpdatedAt { get; set; } = string.Empty;

        [DataMember(Name = "isFinalized", Order = 8)]
        public bool IsSaved { get; set; }

        internal NoteRecord Clone()
        {
            return new NoteRecord
            {
                Id = Id,
                Text = Text,
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                IsSaved = IsSaved
            };
        }
    }
}
