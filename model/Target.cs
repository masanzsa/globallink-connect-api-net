using TS = GlobalLink.Connect.TargetServiceRef;
using System;
using System.Collections.Generic;
using System.IO;

namespace GlobalLink.Connect.Model
{
    public class Target
    {
        public String clientIdentifier { get; set; }
        public String documentName { get; set; }
        public String documentTicket { get; set; }
        public String sourceLocale { get; set; }
        public String targetLocale { get; set; }
        public Dictionary<String, String> metadata { get; set; }
        public String submissionName { get; set; }
        public String ticket { get; set; }
        public WordCount wordCount { get; set; }
     

        public Target(TS.Target dtoTarget)
        {
            this.documentName = dtoTarget.document.documentInfo.name;
            this.targetLocale = dtoTarget.targetLanguage.locale;
            this.sourceLocale = dtoTarget.sourceLanguage.locale;
            //this.submissionName = dtoTarget.document.documentGroup.submission.submissionInfo.name;
            TS.TmStatistics tmstats = dtoTarget.tmStatistics;
            if (tmstats != null)
            {
                this.wordCount = new WordCount((int)tmstats.goldWordCount, (int)tmstats.oneHundredMatchWordCount, (int)tmstats.repetitionWordCount, (int)tmstats.noMatchWordCount,
                    (int)tmstats.totalWordCount);
            }
            this.clientIdentifier = dtoTarget.document.documentInfo.clientIdentifier;

            this.metadata = new Dictionary<String, String>();
            TS.Metadata[] metadatas = dtoTarget.targetInfo.metadata;
            if (metadatas != null)
            {
                foreach (TS.Metadata metadata in metadatas)
                {
                    this.metadata.Add(metadata.key, metadata.value);
                }
            }

            this.ticket = dtoTarget.ticket;
            this.documentTicket = dtoTarget.document.ticket;

        }

        /**
         * Get translated data
         * 
         * @return byte array containing the translated content
         */
        public MemoryStream  getData(GLExchange xchange)
        {
            return xchange.downloadCompletedTarget(this.ticket);
        }
        /**
       * Sends confirmation that the target resources was downloaded successfully
       * by the customer.
       * 
       * @param targetTicket
       *            Downloaded target`s ticket
       */
        public String sendDownloadConfirmation(GLExchange xchange)
        {
            return xchange.sendDownloadConfirmation(this.ticket);
        }
    }
}
