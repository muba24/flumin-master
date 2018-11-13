using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib {

    public class NodeSystemSettings {

        static private NodeSystemSettings _instance;

        static public NodeSystemSettings Instance => _instance ?? (_instance = new NodeSystemSettings { SystemHost = new DefaultSystemHost() });

        private NodeSystemSettings() { }

        // ----------------------------------

        public Forker Forker => CustomPool.Forker;

        public INodeSystemHost SystemHost { get; set; }

    }

}
