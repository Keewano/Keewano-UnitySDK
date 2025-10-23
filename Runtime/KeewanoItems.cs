#pragma warning disable S1104

namespace Keewano
{
    /**
    @brief Represents a game item with a unique identifier and a quantity.

    @sa \ref DataFormatSpecs for name parameter requirements.
    */
    public struct Item
    {
        /// Unique name of the item.
        public string UniqItemName;

        /// Quantity of the item.
        public uint Count;

        ///@brief Initializes a new instance of the Item struct with a specified name and count.

        public Item(string uniqItemName, uint count)
        {
            UniqItemName = uniqItemName;
            Count = count;
        }

        /**
        @brief Initializes a new instance of the Item struct with a specified name,
        defaulting the count to 1.
        */
        public Item(string uniqItemName)
        {
            UniqItemName = uniqItemName;
            Count = 1;
        }
    };
}
