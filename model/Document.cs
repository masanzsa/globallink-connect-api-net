using GlobalLink.Connect.DocumentServiceRef;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GlobalLink.Connect.Model
{
    public class Document : ReferenceDocument
    {
        /*
         * Source Language of the document in xx-XX format.
         */
        public String sourceLanguage { get; set; }

        /*
         * Target languages into which this document will be translated
         */
        public String[] targetLanguages { get; set; }

        /*
         * [Optional] Specify a unique identifier for this document (if one exists)
         * in the content management system
         */
        public String clientIdentifier { get; set; }

        /*
         * [Optional] Encoding of the content if the data is textual. Defaults to UTF-8
         */
        public String encoding { get; set; }
        
        /**
         * File Format profile that defines the parsing rules for the document
         */
        public String fileformat { get; set; }

        /**
         * [Optional] Translation instructions
         */
        public String instructions { get; set; }

        /*
         * [Optional] Additional metadata that you may want to attach to your document
         */
        public Dictionary<String, String> metadata { get; set; }

        public Dictionary<String, String> targetWorkflowNames { get; set; }


        public Document()
        {
            encoding = "UTF-8";
            metadata = new Dictionary<string,string>();
            targetWorkflowNames = new Dictionary<string,string>();
        }

        private String cleanup(String name) {
            String clean = String.Empty;
            try
            {
                String specialChars = "\\\\/:*?\"<>|";

                Regex cleanup = new Regex(@"[" + specialChars + "]");
                clean=cleanup.Replace(name, String.Empty);
            }
            catch {
                clean = "INVALID_NAME";
            }
            return clean;
        }

        /**
         * Internal method used by the UCF API
         * 
         * @return DocumentInfo object
         */
        public DocumentInfo getDocumentInfo(Submission submission)
        {
            DocumentInfo documentInfo = new DocumentInfo();

            documentInfo.name = cleanup(name);
            documentInfo.sourceLocale = sourceLanguage;
            documentInfo.projectTicket = submission.project.ticket;
            String[] tickets = submission.tickets.ToArray();
            if (tickets != null && tickets.Length > 0)
            {
                documentInfo.submissionTicket = tickets[tickets.Length-1];
            }

            if (metadata != null && metadata.Count > 0)
            {
                int i = 0;
                Metadata[] pdMetadatas = new Metadata[metadata.Count];
                foreach (String key in metadata.Keys)
                {
                    Metadata pdMetadata = new Metadata();
                    pdMetadata.key = key.Length > 255 ? key.Substring(0, 255) : key;
					string value = metadata[key];
                    pdMetadata.value = value.Length > 1024 ? value.Substring(0, 1024) : value;
                    pdMetadatas[i++] = pdMetadata;
                }
                documentInfo.metadata = pdMetadatas;
            }
            if (!String.IsNullOrEmpty(clientIdentifier))
            {
                documentInfo.clientIdentifier = clientIdentifier;
            }
            if (!String.IsNullOrEmpty(instructions))
            {
                documentInfo.instructions = instructions;
            }
            else if (!String.IsNullOrEmpty(submission.instructions)) {
                documentInfo.instructions = submission.instructions;
            }

            documentInfo.targetInfos = getTargetInfos(submission);

            return documentInfo;
        }

        /**
         * Internal method used by the UCF API
         * 
         * @return TargetInfo array
         */
        private TargetInfo[] getTargetInfos(Submission submission)
        {
            TargetInfo[] targetInfos = new TargetInfo[targetLanguages.Length];
            for (int j = 0; j < targetLanguages.Length; j++)
            {
                TargetInfo targetInfo = new TargetInfo();
                targetInfo.targetLocale = targetLanguages[j];
                targetInfo.encoding = encoding;
                if( targetWorkflowNames.Count > 0 ) {
				    foreach(String key in targetWorkflowNames.Keys ) {
					    if( key.Equals( targetLanguages[j] ) ) {
						    foreach( Workflow workflow in submission.project.workflows ) {
                                if (workflow.name.Equals(targetWorkflowNames[key]))
                                {
								    targetInfo.workflowDefinitionTicket = workflow.ticket;
								    break;
							    }
						    }
						    break;
					    }
				    }
			    }
                targetInfos[j] = targetInfo;
            }
            return targetInfos;
        }

        /**
         * Internal method used by the UCF API
         * 
         * @return ResourceInfo object
         * @throws IOException
         */
        public new ResourceInfo getResourceInfo()
        {
            ResourceInfo resourceInfo = new ResourceInfo();
            resourceInfo.name = cleanup(name); 
            resourceInfo.size = data.Length;
            resourceInfo.classifier = fileformat;
            resourceInfo.encoding = encoding;
            if (clientIdentifier != null)
            {
                resourceInfo.clientIdentifier = clientIdentifier;
            }
            return resourceInfo;
        }

    }
}
