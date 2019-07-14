#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerrariaApi.Server;
#endregion
namespace FakeProvider
{
    [ApiVersion(2, 1)]
    public class FakeProvider : TerrariaPlugin
    {
        #region Description
        
        public override string Name => "FakeProvider";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Author => "Anzhelika & ASgo";
        public override string Description => "TODO";
        public FakeProvider(Main game) : base(game) { }

        #endregion

        public override void Initialize()
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}