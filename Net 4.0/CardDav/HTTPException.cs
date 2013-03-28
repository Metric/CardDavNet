using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardDav
{
    public class HTTPException : Exception
    {
        public HTTPException() : base()
        {
           
        }

        public HTTPException(string message) : base(message)
        {
        }
    }
}
