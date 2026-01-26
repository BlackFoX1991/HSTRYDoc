using System;
using System.Collections.Generic;

namespace HSTRYDoc
{
    public sealed class MaskField
    {
        public string Name { get; init; } = string.Empty;          // e.g. "PATIENT_ID"
        public string? RefMaskId { get; init; }                    // e.g. "PERSON" (optional)

        public bool IsReference => !string.IsNullOrWhiteSpace(RefMaskId);

        public override string ToString()
            => IsReference ? $"{Name}:{RefMaskId}" : Name;
    }

    public sealed class MaskDefinition
    {
        public string ModelBlockTitle { get; init; } = string.Empty;   // e.g. "benutzer.model"
        public string MaskId { get; init; } = string.Empty;            // e.g. "PATIENTS"
        public string DisplayName { get; init; } = string.Empty;       // e.g. "Patientenliste..."
        public List<MaskField> Fields { get; init; } = new();

        public string DataBlockSuffix => $".data:{MaskId}";

        public bool HasIdField
        {
            get
            {
                foreach (var f in Fields)
                {
                    if (string.Equals(f.Name, "ID", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(MaskId))
                return DisplayName;

            return $"{DisplayName}  ({MaskId})";
        }
    }

    public sealed class MaskRecord
    {
        public int SourceBlockIndex { get; init; }
        public Dictionary<string, string> Data { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class MaskDataDocument
    {
        public List<MaskRecord> Records { get; } = new();
    }
}
