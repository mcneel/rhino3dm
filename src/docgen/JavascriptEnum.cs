using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace docgen
{
    class JavascriptEnum : BindingClass
    {
        public JavascriptEnum(string name) : base(name)
        {
            Name = name;
        }

        public string Name { get; }
        public List<string> Elements { get; } = new List<string>();
    }
}
