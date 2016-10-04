using System;
using System.Collections.Generic;

namespace GlobalLink.Connect.Model
{
    public class Submission
    {
        /**
         * [Optional] Custom attributes. Project.getCustomAttributes() gets you the list of configured attributes for the project. 
         */
        public Dictionary<String, String> customAttributes { get; set; }

        /**
         * [Optional] Due date by when the submission should be completed in Project Director.
         * This should always be greater than current date. If due date is not specified, Project Director 
         * will rely on the configured language model to calculate a due date. 
         */
        public DateTime dueDate { get; set; }

        /**
         * [Optional] Set instructions for the translators    
         */
        public String instructions { get; set; }

        /**
         * [Optional] Set priority. Defaults to Normal 
         */
        public Boolean isUrgent { get; set; }

        /**
         * [Optional] Key-value pair of metadata
         */
        public Dictionary<String, String> metadata { get; set; }

        /**
         * Name of the submission that will show up in Project Director
         */
        public String name { get; set; }

        /**
         * [Optional] Notes for the project managers 
         */
        public String pmNotes { get; set; }

        /**
         * Set the project for which this submission will be created
         */
        public Project project { get; set; }

        /**
        * Submission ticket
        */
        public List<String> tickets { get; set; }
        /**
         * [Optional]  Set the submitter to a user other than the logged in user 
         */
        public String submitter { get; set; }

        /**
         * [Optional]  Set the workflow to use 
         */
        public Workflow workflow { get; set; }

        public String owner { get; set; }


		public Submission()
        {
            customAttributes = new Dictionary<string, string>();
            tickets = new List<string>();
        }

        public Submission(SubmissionServiceRef.Submission submission)
        {
            customAttributes = new Dictionary<string, string>();
            tickets = new List<string>();
            if (submission != null)
            {
                this.tickets.Add(submission.ticket);
                this.name = submission.submissionInfo.name;
            }
        }


        public void setTickets(String[] tickets) {
		    this.tickets = new List<String>();
		    if( tickets != null && tickets.Length > 0 ) {
			    foreach( String ticket in tickets ) {
				    addTicket( ticket );
			    }
		    }
	    }

        public void addTicket(String ticket)
        {
            if (ticket == null)
            {
                throw new Exception("Invalid NULL ticket.");
            }
            else if (ticket.Length == 0)
            {
                throw new Exception("Invalid EMPTY ticket.");
            }
            if (!tickets.Contains(ticket))
            {
                this.tickets.Add(ticket);
            }
        }

    }
}
