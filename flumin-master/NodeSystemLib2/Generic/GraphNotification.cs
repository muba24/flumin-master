using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSystemLib2.Generic {

    public class GraphNotification {

        public enum NotificationType {
            Info, Warning, Error
        }

        public NotificationType Type { get; }
        public string Message { get; }
        public  Node Source { get; }

        public GraphNotification(NotificationType type, string msg) {
            Type = type;
            Message = msg;
            Source = null;
        }

        public GraphNotification(Node source, NotificationType type, string msg) {
            Type = type;
            Message = msg;
            Source = source;
        }

    }

}
