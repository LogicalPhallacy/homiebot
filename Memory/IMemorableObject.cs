using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
namespace homiebot.memory
{
    public interface IMemorableObject
    {
        object Id
        {
            get;
            internal set;
        }
        Type idType {get;}
        void SetId(object id)
        {
            if(id.GetType() == idType)
            {
                this.Id = id;
            }else
            {
                throw new InvalidCastException("Unexpected type for id");
            }
        }
    }
}