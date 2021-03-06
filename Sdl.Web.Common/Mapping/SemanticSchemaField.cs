﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents a Semantic Schema Field.
    /// </summary>
    /// <remarks>
    /// Deserialized from JSON in schemas.json.
    /// </remarks>
    public class SemanticSchemaField
    {
        /// <summary>
        /// XML field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// XML field path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Is field a metadata field?
        /// </summary>
        public bool IsMetadata
        {
            get
            {
                // metadata fields start their Path with /Metadata
                return Path.StartsWith("/Metadata");
            }
        }

        /// <summary>
        /// Is field an embedded field?
        /// </summary>
        public bool IsEmbedded
        {
            // TODO this could also be a linked field, does that matter?
            get 
            {
                // path of an embedded field contains more than two forward slashes, 
                // e.g. /Article/articleBody/subheading
                int count = 0;
                foreach (char c in Path)
                {
                    if (c == '/')
                    {
                        count++;
                    }                    
                }
                return count > 2;
            }
        }

        /// <summary>
        /// Is field multivalued?
        /// </summary>
        public bool IsMultiValue { get; set; }

        /// <summary>
        /// Field semantics.
        /// </summary>
        public List<FieldSemantics> Semantics { get; set; }

        /// <summary>
        /// Embedded fields.
        /// </summary>
        public List<SemanticSchemaField> Fields { get; set; }

        /// <summary>
        /// Check if current field contains given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns>True if this field contains given semantics, false otherwise</returns>
        [Obsolete("Deprecated in DXA 1.6. Use HasSemantics instead.")]
        public bool ContainsSemantics(FieldSemantics fieldSemantics)
        {
            return HasSemantics(fieldSemantics);
        }

        /// <summary>
        /// Check if current field has given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns><c>true</c> if this field has given semantics, <c>false</c> otherwise.</returns>
        public bool HasSemantics(FieldSemantics fieldSemantics)
        {
            // TODO: shouldn't we be matching on fieldSemantics.Entity? return Semantics.Any(s => s.Equals(fieldSemantics));
            return Semantics.Any(s => s.Property.Equals(fieldSemantics.Property) && s.Prefix.Equals(fieldSemantics.Prefix));
        }


        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <param name="includeSelf">If <c>true</c> the field itself will be returned if it matches the given semantics.</param>
        /// <returns>This field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldBySemantics(FieldSemantics fieldSemantics)
        {
            // Perform a breadth-first lookup: first see if any of the embedded fields themselves match.
            SemanticSchemaField matchingEmbeddedField = Fields.FirstOrDefault(ssf => ssf.HasSemantics(fieldSemantics));
            if (matchingEmbeddedField != null)
            {
                return matchingEmbeddedField;
            }

            // If none of the embedded fields match: let each embedded field do a breadth-first lookup of its embedded fields (recursive).
            return Fields.Select(ssf => ssf.FindFieldBySemantics(fieldSemantics)).FirstOrDefault(matchingField => matchingField != null);
        }

        /// <summary>
        /// Provides a string representation of the object.
        /// </summary>
        /// <returns>A string representation containing the field Name and Path</returns>
        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", GetType().Name, Name, Path);
        }
    }
}
