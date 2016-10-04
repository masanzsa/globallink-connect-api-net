using GlobalLink.Connect.ProjectServiceRef;
using System;

namespace GlobalLink.Connect.Model
{
    public class Workflow
    {
        public String name { get; set; }
        public String ticket { get; set; }

        public Workflow(WorkflowDefinition definition)
        {
            this.name = definition.name;
            this.ticket = definition.ticket;
        }
    }
}
