﻿namespace Sdl.Web.Common
{
    /// <summary>
    /// Exception thrown by DXA Content Providers if an item is not found.
    /// </summary>
    public class DxaItemNotFoundException : DxaException
    {
        /// <summary>
        /// Gets the identifier of the item which is not found.
        /// </summary>
        public string ItemId
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DxaItemNotFoundException"/>.
        /// </summary>
        /// <param name="itemId">The identifier of the item which is not found.</param>
        public DxaItemNotFoundException(string itemId)
            : base(string.Format("Item '{0}' not found", itemId))
        {
            ItemId = itemId;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DxaItemNotFoundException"/>.
        /// </summary>
        /// <param name="itemId">The identifier of the item which is not found.</param>
        /// <param name="localizationId">The context Localization Identifier.</param>
        public DxaItemNotFoundException(string itemId, string localizationId)
            : base(string.Format("Item '{0}' not found for Localization '{1}'", itemId, localizationId))
        {
            ItemId = itemId;
        }

    }
}
