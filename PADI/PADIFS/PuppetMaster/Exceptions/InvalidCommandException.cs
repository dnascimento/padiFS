using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster.Exceptions
{
    class CommandException : Exception
    {
        public String Meessages;
        public CommandException(String msg)
        {
            Meessages = msg;
        }
    }
}
