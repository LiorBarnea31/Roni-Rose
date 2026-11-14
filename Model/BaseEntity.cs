using System;
using System.Collections.Generic;
using System.Text;

namespace Model
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }

        protected BaseEntity() { }

        protected BaseEntity(int id)
        {
            Id = id;
        }
    }
}