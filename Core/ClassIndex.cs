using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core
{
    public class ClassIndex : Entity
    {
        public MappedList<ClassIndex, Entity> Entities { get; private set; }



        public override void initialize()
        {
            base.initialize();
            Entities = new MappedList<ClassIndex, Entity>(this, (a, b) => b._class.UnsafeAdd(a));
        }
    }
}
