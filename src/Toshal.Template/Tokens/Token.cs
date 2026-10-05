using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Toshal.Template.Tokens
{
    public abstract class Token : IToken
    {
        public int LineNumber { get; set; }

        public int StartingPosition { get; set; }

    }
}
