using PS=GlobalLink.Connect.ProjectServiceRef;
using System;

namespace GlobalLink.Connect.Model
{
    public class Project
    {
        /**
        * Project short code
        */
        public String shortcode { get; set; }

        /**
        * Project name
        */
        public String name { get; set; }

        /**
         * Project ticket, an internal ID required for creating submissions
         */
        public String ticket { get; set; }

        /**
         * List of supported language directions
         */
        public LanguageDirection[] languageDirections { get; set; }

        /**
         * List of file format profiles configured for this project. A file format
         * profile defines the parsing rules to identify translatable content and
         * inline non-translatable content (placeholders) within the submitted
         * document. Project Director supports a wide range of formats such as XML,
         * DOC, PPT, XLS, .properties
         */
        public String[] fileFormats { get; set; }

        /**
         * List of available workflows configured for this project
         */
        public Workflow[] workflows { get; set; }

        /**
         * List of custom attributes configured for this project
         */
        public CustomAttribute[] customAttributes { get; set; }

        public String[] submitters { get; set; }


        public Project(PS.Project project)
        {
            this.shortcode = project.projectInfo.shortCode;
            this.name = project.projectInfo.name;
            this.ticket = project.ticket;

            int i = 0;
            this.languageDirections = new LanguageDirection[project.projectLanguageDirections.Length];
            foreach (PS.ProjectLanguageDirection direction in project.projectLanguageDirections)
            {
                this.languageDirections[i++] = new LanguageDirection(direction);
            }

            i = 0;
            this.fileFormats = new String[project.fileFormatProfiles.Length];
            foreach (PS.FileFormatProfile profile in project.fileFormatProfiles)
            {
                this.fileFormats[i++] = profile.profileName;
            }

            i = 0;
            this.workflows = new Workflow[project.workflowDefinitions.Length];
            foreach (PS.WorkflowDefinition definition in project.workflowDefinitions)
            {
                this.workflows[i++] = new Workflow(definition);
            }

            if (project.projectCustomFieldConfiguration != null)
            {
                try
                {
                    i = 0;
                    this.customAttributes = new CustomAttribute[project.projectCustomFieldConfiguration.Length];
                    foreach (PS.ProjectCustomFieldConfiguration customField in project.projectCustomFieldConfiguration)
                    {
                        this.customAttributes[i++] = new CustomAttribute(customField);
                    }
                }
                catch (Exception) { 
                // do nothing .. bug in PD always returns this as null. To be resolved
                }
            }
        }

    }
}
