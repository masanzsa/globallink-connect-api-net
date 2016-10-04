using GlobalLink.Connect.ProjectServiceRef;
using System;

namespace GlobalLink.Connect.Model
{
    public class LanguageDirection
    {
        public String sourceLanguage { get; set; }
        public String targetLanguage { get; set; }

        public LanguageDirection(ProjectLanguageDirection direction)
        {
            this.sourceLanguage = direction.sourceLanguage.locale;
            this.targetLanguage = direction.targetLanguage.locale;
        }

    }
}
