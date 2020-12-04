using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QotomReview.model
{
    public class BaseData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Size { get; set; }
        public string Speed { get; set; }
        public string Check { get; set; }

        public BaseData() { }

        public BaseData(string name, string type, string size)
        {
            Name = name;
            Type = type;
            Size = size;
        }

        public BaseData(string name, string type, string size, string speed) : this(name, type, size)
        {
            Speed = speed;
        }

        public override string ToString()
        {
            return $"{{{nameof(Name)}={Name}, {nameof(Type)}={Type}, {nameof(Size)}={Size}, {nameof(Speed)}={Speed}, {nameof(Check)}={Check}}}";
        }
    }
}
