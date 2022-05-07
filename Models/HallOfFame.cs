using System;

namespace Homiebot.Brain
{
    public class HallOfFameItem : IMemorableObject
    {
        private long longId;
        object IMemorableObject.Id {get => longId; set => longId = (long)value;}

        public HallOfFameItem()
        {
            
        }

        public Type idType => throw new NotImplementedException();
    }
}