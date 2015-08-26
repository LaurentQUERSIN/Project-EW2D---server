using Stormancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_EW2D___server
{
    public class app
    {
        public void Run(IAppBuilder builder)
        {
            builder.AddGameScene();
        }
    }
}
