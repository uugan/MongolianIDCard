using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonSign
{
    interface ISmartCardInterface
    {
        byte[] SendApdu(ApduCommand command);
        bool IsCardPresent();
    }
}
