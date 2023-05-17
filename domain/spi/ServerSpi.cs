using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DemoKeypleLess.domain.spi {
    public interface ServerSpi {
        string transmitRequest ( string jsonRequest );
    }
}
