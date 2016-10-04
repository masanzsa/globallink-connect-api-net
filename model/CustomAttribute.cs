using GlobalLink.Connect.ProjectServiceRef;
using System;

namespace GlobalLink.Connect.Model
{
    public class CustomAttribute
    {
        public bool mandatory { get; set; }
        public String name { get; set; }
        public String type { get; set; }
        public String values { get; set; }

        public CustomAttribute(ProjectCustomFieldConfiguration customField)
        {
            this.name = customField.name;
            this.type = customField.type;
            this.values = customField.values;
            this.mandatory = (bool)customField.mandatory;
        }

        public CustomAttribute(bool mandatory, String name, String type, String values)
        {
            this.mandatory = mandatory;
            this.name = name;
            this.type = type;
            this.values = values;
        }
    }
}
