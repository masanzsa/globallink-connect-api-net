using GlobalLink.Connect.Config;
using GlobalLink.Connect.Proxy;
using System;
using System.ServiceModel;
using DS = GlobalLink.Connect.DocumentServiceRef;
using PS = GlobalLink.Connect.ProjectServiceRef;
using SS = GlobalLink.Connect.SubmissionServiceRef;
using TS = GlobalLink.Connect.TargetServiceRef;
using US = GlobalLink.Connect.UserProfileServiceRef;
using GL = GlobalLink.Connect.Model;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using GlobalLink.Connect.SubmissionServiceRef;
using GlobalLink.Connect.UserProfileServiceRef;
using System.Net;
using GlobalLink.Connect.Model;

namespace GlobalLink.Connect
{
    public class GLExchange
    {
        private const string PDVERSION = "4.13.0";

        private ProjectDirectorConfig pdConfig;
        private GL.Submission submission;
        private BasicHttpBinding binding;
        private UsernameToken USERNAMETOKEN;
        private EndpointAddress PROJECT_ENDPOINT_ADDRESS;
        private EndpointAddress TARGET_ENDPOINT_ADDRESS;
        private EndpointAddress SUBMISSION_ENDPOINT_ADDRESS;
        private EndpointAddress DOCUMENT_ENDPOINT_ADDRESS;
        private EndpointAddress USER_ENDPOINT_ADDRESS;

        /**
         * Initialize a connection to Project Director
         * 
         * @param connectionConfig
         *            The Project Director server configuration to connect to.
         * @see ProjectDirectorConfig
         */
        public GLExchange(ProjectDirectorConfig connectionConfig)
        {
            setConnectionConfig(connectionConfig);
        }

        private String _cleanup(String name)
        {
            String clean = String.Empty;
            try
            {
                String specialChars = "\\\\/:*?\"<>|";

                Regex cleanup = new Regex(@"[" + specialChars + "]");
                clean = cleanup.Replace(name, String.Empty);
            }
            catch
            {
                clean = "INVALID_NAME";
            }
            return clean;
        }

        private SS.Date _convertDate(DateTime dateTime)
        {
            SS.Date date = new SS.Date();
            DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            date.date = (long)(dateTime - Jan1st1970).TotalMilliseconds;
            return date;
        }

        private GL.Target[] _convertTargetsToInternal(TS.Target[] targets)
        {
            if (targets == null)
            {
                return new GL.Target[0];
            }
            GL.Target[] result = new GL.Target[targets.Length];
            int i = 0;
            foreach (TS.Target target in targets)
            {
                result[i++] = new GL.Target(target);
            }
            return result;
        }

        private GL.Project[] _convertProjectsToInternal(PS.Project[] projects)
        {
            GL.Project[] result = new GL.Project[projects.Length];
            int i = 0;
            try
            {
                foreach (PS.Project project in projects)
                {
                    GL.Project proj = new GL.Project(project);
                    proj.submitters = _getSubmitters(proj.shortcode);
                    result[i++] = proj;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return result;
        }

        private GL.Submission[] _convertSubmissionsToInternal( SS.Submission[] list ) {
		    List<GL.Submission> result = new List<GL.Submission>();
		    foreach( SS.Submission sub in list ) {
			    result.Add( new GL.Submission( sub ) );
		    }
		    return result.ToArray();
	    }

        
	    private Dictionary<String,SubmissionInfo> _createSubmissionInfos() {
		    int count = -1;
		    List<string> tickets = submission.tickets;

		    // Use counter only if multiple submissions are to be started
		    if( tickets.Count > 1 ) {
			    count = 1;
		    }

		    Dictionary<String,SubmissionInfo> submissionInfos = new Dictionary<String,SubmissionInfo>( tickets.Count );

		    foreach( String submissionTicket in tickets ) {
			    submissionInfos.Add( submissionTicket, _createSubmissionInfo( count ) );
			    if( count != -1 ) {
				    count = count + 1;
			    }
		    }

		    return submissionInfos;
	    }

        private SS.SubmissionInfo _createSubmissionInfo(int count)
        {
            SubmissionInfo submissionInfo = new SubmissionInfo();

            //Custom Attributes
            if (submission.customAttributes != null && submission.customAttributes.Count > 0)
            {
                SubmissionCustomFields[] customFields = new SubmissionCustomFields[submission.customAttributes.Count];
                int i = 0;
                foreach (String key in submission.customAttributes.Keys)
                {
                    SubmissionCustomFields custom = new SubmissionCustomFields();
                    custom.fieldName = key;
                    custom.fieldValue = submission.customAttributes[key];
                    customFields[i] = custom;
                    i++;
                }
                submissionInfo.submissionCustomFields = customFields;
            }


            if (submission.dueDate != null && submission.dueDate.Year != 1)
            {
                submissionInfo.dateRequested = _convertDate(submission.dueDate);
            }

		    if( count == -1 ) {
			    submissionInfo.name = _cleanup(submission.name);
		    } else {
                submissionInfo.name = _cleanup(submission.name) + "(" + count + ")";
		    }

            //TODO submission.instructions - waiting for PD ticket to be fixed
            if (submission.isUrgent)
            {
                Priority priority = new Priority();
                priority.value = 2;
                submissionInfo.priority = priority;
            }
            if (submission.metadata != null)
            {
                Metadata[] metadata = new Metadata[submission.metadata.Count];
                int i = 0;
                foreach (String key in submission.metadata.Keys)
                {
                    metadata[i].key = key.Length > 255 ? key.Substring(0, 255) : key;
                    string value = submission.metadata[key];
                    metadata[i].value = value.Length > 1024 ? value.Substring(0, 1024) : value;
                    i++;
                }
                submissionInfo.metadata = metadata;
            }
            
            if (!String.IsNullOrEmpty(submission.pmNotes))
            {
                submissionInfo.internalNotes = submission.pmNotes;
            }
            submissionInfo.projectTicket = submission.project.ticket;


            if (submission.submitter != null)
            {
                submissionInfo.submitters = new String[] { submission.submitter };
            }
            if (submission.workflow != null && !String.IsNullOrEmpty(submission.workflow.ticket))
            {
                submissionInfo.workflowDefinitionTicket = submission.workflow.ticket;
            }

            return submissionInfo;
        }

        private bool _endpointExists()
        {
            String urlStr = pdConfig.url;
            if (!urlStr.EndsWith("/"))
            {
                urlStr = urlStr + "/";
            }
            try
            {
                var client = new WebClient();
                if (pdConfig.proxy != null)
            {
                if (pdConfig.username != null && pdConfig.password != null)
                {
                    binding.BypassProxyOnLocal = false;
                    binding.UseDefaultWebProxy = true;
                    System.Net.NetworkCredential cred = new NetworkCredential(pdConfig.proxy.proxyUser, pdConfig.proxy.proxyPassword);
                    WebProxy wproxy = new WebProxy("http://" + pdConfig.proxy.proxyHost + ":" + pdConfig.proxy.proxyPort, false, null, cred);
                    client.Proxy = wproxy;
                }
                else
                {
                    WebProxy wproxy = new WebProxy("http://" + pdConfig.proxy.proxyHost + ":" + pdConfig.proxy.proxyPort);
                    client.Proxy = wproxy;
                }
            }
                    using (var stream = client.OpenRead(urlStr + "services/ProjectService_4130.wsdl"))
                    {
                        return true;
                    }

            }
            catch (Exception)
            {
                throw new Exception("Url '" + pdConfig.url + "' incorrect or unsupported PD version (needs to be "
                    + PDVERSION + " or higher).");
            }
        }

        private String[] _getSubmitters(String projectShortCode)
        {
            US.getSubmittersRequest request = new US.getSubmittersRequest
            {
                projectShortCode = projectShortCode
            };
            US.UserProfile[] users = null;
            HashSet<String> submitters = new HashSet<string>();
            using (WcfServiceClient<US.UserProfileServicePortType> wcfServiceClient =
                new WcfServiceClient<US.UserProfileServicePortType>(binding, USER_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    users = wcfServiceClient.Service.getSubmitters(request).@return;
                });
            }
            if (users != null)
            {
                foreach (US.UserProfile profile in users)
                {
                    UserInfo info = profile.userInfo;
                    if (info.userName != null && !info.userName.Equals("") && info.enabled.Value && !info.accountLocked.Value && info.accountNonExpired.Value)
                    {
                        submitters.Add(info.userName);
                    }
                }
            }
            string[] retVal = new string[submitters.Count];
            submitters.CopyTo(retVal);
            return retVal;
        }

        private Boolean _isSubmitterValid(String projectShortCode, String submitter)
        {
            string[] submitters = _getSubmitters(projectShortCode);

            foreach (String user in submitters)
            {
                if (user.Equals(submitter))
                {
                    return true;
                }
            }

            return false;
        }

        private void _validateDocument(GL.Document document)
        {
            if (document == null || document.data == null || document.data.Length <= 0)
            {
                throw new Exception("Document is empty");
            }
            if (String.IsNullOrEmpty(document.name))
            {
                throw new Exception("Document name not set");
            }

            GL.Project project = submission.project;

            if (!document.fileformat.Equals("Non-Parsable", StringComparison.CurrentCultureIgnoreCase))
            {
                Boolean isFileFormatCorrect = false;

                foreach (String fileFormat in project.fileFormats)
                {
                    if (fileFormat.Equals(document.fileformat))
                    {
                        isFileFormatCorrect = true;
                        break;
                    }
                }
                if (!isFileFormatCorrect)
                {
                    throw new Exception("Specified document file format '" + document.fileformat + "' is not configured in project");
                }
            }


            if (String.IsNullOrEmpty(document.sourceLanguage))
            {
                throw new Exception("Source language not set");
            }
            if (document.targetLanguages == null || document.targetLanguages.Length <= 0)
            {
                throw new Exception("Target languages are not set");
            }

            foreach (String targetLanguage in document.targetLanguages)
            {
                Boolean isLanguageDirectionFound = false;
                foreach (GL.LanguageDirection direction in project.languageDirections)
                {
                    if (direction.sourceLanguage.Equals(document.sourceLanguage) && direction.targetLanguage.Equals(targetLanguage))
                    {
                        isLanguageDirectionFound = true;
                        break;
                    }
                }
                if (!isLanguageDirectionFound)
                {
                    throw new Exception(
                        "Project is not configured for language direction " + document.sourceLanguage + "->" + targetLanguage);
                }
            }
            foreach( String targetLanguage in document.targetLanguages ) {
			    foreach( String key in document.targetWorkflowNames.Keys ) {
				    if( key.Equals( targetLanguage ) ) {
					    Boolean found = false;
					    foreach( Workflow workflow in submission.project.workflows ) {
                            if (workflow.name.Equals(document.targetWorkflowNames[key]))
                            {
							    found = true;
							    break;
						    }
					    }
					    if( !found ) {
                            throw new Exception("Workflow with name '" + document.targetWorkflowNames[key]
							    + "' for target language " + key + " not found in project "
							    + submission.project.shortcode );
					    }
				    }
			    }
		    }
        }

        private Boolean _validateSubmission()
        {
            if (submission == null)
            {
                throw new Exception("Please initialize submission first.");
            }
            if (submission.project == null)
            {
                throw new Exception("Please set submission project");
            }
            if (String.IsNullOrEmpty(submission.name))
            {
                throw new Exception("Please set submission name");
            }
            if (submission.workflow != null)
            {
                GL.Workflow[] workflows = submission.project.workflows;
                Boolean isSet = false;
                foreach (GL.Workflow wf in workflows)
                {
                    if (wf.ticket.Equals(submission.workflow.ticket))
                    {
                        isSet = true;
                        break;
                    }
                }
                if (!isSet)
                {
                    throw new Exception("Invalid submission workflow '" + submission.workflow.name);
                }
            }
            if (submission.project.customAttributes != null)
            {
                foreach (GL.CustomAttribute customAttribute in submission.project.customAttributes)
                {
                    if (customAttribute == null)
                    {
                        continue;
                    }
                    if (customAttribute.mandatory)
                    {
                        Boolean isSet = false;
                        foreach (String key in submission.customAttributes.Keys)
                        {
                            if (key.Equals(customAttribute.name) && !String.IsNullOrEmpty(submission.customAttributes[key]))
                            {
                                isSet = true;
                                break;
                            }
                        }
                        if (!isSet)
                        {
                            throw new Exception("Mandatory custom field '" + customAttribute.name + "' is not set");
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(submission.submitter))
            {
                if (!_isSubmitterValid(submission.project.shortcode, submission.submitter))
                {
                    throw new Exception("Specified submitter '" + submission.submitter + "' doesn`t exist in project " + submission.project.shortcode);
                }
            }

            if (!String.IsNullOrEmpty(submission.owner) && !submission.owner.Equals(submission.submitter))
            {
                if (!_isSubmitterValid(submission.project.shortcode, submission.owner))
                {
                    throw new Exception("Specified submitter '" + submission.submitter + "' doesn`t exist in project " + submission.project.shortcode);
                }
            }

            /* if (submission.dueDate!=null && submission.dueDate < DateTime.Now.Date) {
                throw new Exception("Submission due date should be greater than current date");
            } */

            return true;
        }

        /**
         * Cancel document for all languages
         * 
         * @param documentTicket
         *            Document ticket to cancel
         */
        public String cancelDocument(String documentTicket)
        {
            TS.DocumentTicket dticket = new TS.DocumentTicket();
            dticket.ticketId = documentTicket;

            TS.cancelTargetRequest request = new TS.cancelTargetRequest();
            request.targetId = documentTicket;

            string retVal = null;
            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    retVal = wcfServiceClient.Service.cancelTarget(request).@return;
                });
            }
            return retVal;
        }

        /**
         * Cancel document for specified target language
         * 
         * @param documentTicket
         *            Document ticket to cancel
         * @param locale
         *            Target locale to cancel
         */
        public String cancelDocument(String documentTicket, String locale)
        {
            TS.DocumentTicket dticket = new TS.DocumentTicket();
            dticket.ticketId = documentTicket;

            TS.cancelTargetByDocumentIdRequest request = new TS.cancelTargetByDocumentIdRequest();
            request.documentId = dticket;
            request.targetLocale = locale;

            string retVal = null;
            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    retVal = wcfServiceClient.Service.cancelTargetByDocumentId(request).@return;
                });
            }
            return retVal;
        }

        /**
        * Cancel Submission for all languages
        * 
        * @param submissionTicket
        *            Submission ticket to cancel
        */
        public String cancelSubmission(String submissionTicket)
        {
            SS.cancelSubmissionRequest request = new SS.cancelSubmissionRequest();
            request.submissionId = submissionTicket;

            string retVal = null;
            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    retVal = wcfServiceClient.Service.cancelSubmission(request).@return;
                });
            }
            return retVal;
        }

        /**
         * Cancel Submission for all languages with comment
         * 
         * @param submissionTicket
         *            Submission ticket to cancel
         * @param comment
         *            Option Cancellation comment
         */
        public String cancelSubmission(String submissionTicket, String comment)
        {
            SS.cancelSubmissionWithCommentRequest request = new SS.cancelSubmissionWithCommentRequest();
            request.submissionId = submissionTicket;
            request.comment = comment;

            string retVal = null;
            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    retVal = wcfServiceClient.Service.cancelSubmissionWithComment(request).@return;
                });
            }
            return retVal;
        }

        /**
         * Downloads translated document from PD
         * 
         * @param targetTicket
         *            Document ticket to download
         */
        public MemoryStream downloadCompletedTarget(String targetTicket)
        {
            TS.downloadTargetResourceRequest request = new TS.downloadTargetResourceRequest();
            request.targetId = targetTicket;

            TS.RepositoryItem repositoryItem = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    repositoryItem = wcfServiceClient.Service.downloadTargetResource(request).@return;
                });
            }

            if (repositoryItem != null && repositoryItem.data != null && repositoryItem.data.Value.Length > 0)
            {
                return new MemoryStream(repositoryItem.data.Value);
            }
            else
            {
                return null;
            }
        }

        /**
         * Get cancelled targets for a submission
         * 
         * @param submissionTicket
         *            Submission ticket
         * @param maxResults
         *            Maximum number of cancelled targets to return. This
         *            configuration is to avoid time-outs in case the number of
         *            targets is very large.
         * @return Array of cancelled targets
         */
        public GL.Target[] getCancelledTargets(String submissionTicket, int maxResults)
        {
            string[] submissions = new string[1];
            submissions[0] = submissionTicket;

            TS.getCanceledTargetsBySubmissionsRequest requestCancelledTargets = new TS.getCanceledTargetsBySubmissionsRequest
            {
                submissionTickets = submissions,
                maxResults = maxResults
            };

            TS.Target[] result = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.getCanceledTargetsBySubmissions(requestCancelledTargets).@return;
                });
            }

            return _convertTargetsToInternal(result);
        }

        /**
         * Get cancelled targets for all projects
         * 
         * @param maxResults
         *            Maximum number of cancelled targets to return. This
         *            configuration is to avoid time-outs in case the number of
         *            targets is very large.
         * @return Array of cancelled targets
         */
        public GL.Target[] getCancelledTargets(int maxResults)
        {
            PS.getUserProjectsRequest request = new PS.getUserProjectsRequest();
            request.isSubProjectIncluded = true;

            GL.Project[] projects = getProjects();

            string[] prjs = new string[projects.Length];
            for (int i = 0; i < projects.Length; i++)
            {
                prjs[i] = projects[i].ticket;
            }

            TS.getCanceledTargetsByProjectsRequest requestCancelledTargets = new TS.getCanceledTargetsByProjectsRequest
            {
                projectTickets = prjs,
                maxResults = maxResults
            };

            TS.Target[] targets = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    targets = wcfServiceClient.Service.getCanceledTargetsByProjects(requestCancelledTargets).@return;
                });
            }

            return _convertTargetsToInternal(targets);
        }
        /**
        * Get completed targets for all projects
        * 
        * @param maxResults
        *            Maximum number of completed targets to return in this call.
        * @return Array of completed targets
        */

        public GL.Target[] getCompletedTargets(int maxResults)
        {
            PS.getUserProjectsRequest request = new PS.getUserProjectsRequest();
            request.isSubProjectIncluded = true;

            GL.Project[] projects = getProjects();

            string[] projectTickets = new string[projects.Length];

            for (int i = 0; i < projects.Length; i++)
            {
                projectTickets[i] = projects[i].ticket;
            }

            TS.getCompletedTargetsByProjectsRequest requestCompletedTargets = new TS.getCompletedTargetsByProjectsRequest
            {
                projectTickets = projectTickets,
                maxResults = maxResults
            };

            TS.Target[] result = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.getCompletedTargetsByProjects(requestCompletedTargets).@return;
                });
            }

            return _convertTargetsToInternal(result);
        }


        /**
        * Get completed targets for specified project
        * 
        * @param project
        *            Project for which completed targets are requested
        * @param maxResults
        *            Maximum number of completed targets to return in this call.
        * @return Array of completed targets
        */
        public GL.Target[] getCompletedTargets(GL.Project project, int maxResults)
        {
            string[] prjs = new string[1];
            prjs[0] = project.ticket;
            TS.getCompletedTargetsByProjectsRequest requestCompletedTargets = new TS.getCompletedTargetsByProjectsRequest
            {
                projectTickets = prjs,
                maxResults = maxResults
            };

            TS.Target[] result = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.getCompletedTargetsByProjects(requestCompletedTargets).@return;
                });
            }
            return _convertTargetsToInternal(result);
        }

        /**
        * Get completed targets for specified submission ticket
        * 
        * @param submissionTicket
        *            Submission for which completed targets are requested
        * @param maxResults
        *            Maximum number of completed targets to return in this call.
        * @return Array of completed targets
        */
        public GL.Target[] getCompletedTargets(String submissionTicket, int maxResults)
        {
            string[] tickets = new string[1];
            tickets[0] = submissionTicket;
            TS.getCompletedTargetsBySubmissionsRequest requestCompletedTargets = new TS.getCompletedTargetsBySubmissionsRequest
            {
                submissionTickets = tickets,
                maxResults = maxResults
            };

            TS.Target[] result = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.getCompletedTargetsBySubmissions(requestCompletedTargets).@return;
                });
            }
            return _convertTargetsToInternal(result);
        }

        /**
         * Get project by shortcode
         * 
         * @param projectShortcode
         *            Project shortcode
         *            
         * @return Project
         */
        public GL.Project getProject(String projectShortcode)
        {
            try
            {
                PS.findProjectByShortCodeRequest request = new PS.findProjectByShortCodeRequest();
                request.projectShortCode = projectShortcode;

                PS.Project project = null;

                using (WcfServiceClient<PS.ProjectServicePortType> wcfServiceClient =
                    new WcfServiceClient<PS.ProjectServicePortType>(binding, PROJECT_ENDPOINT_ADDRESS, USERNAMETOKEN))
                {
                    wcfServiceClient.CallServiceMethod(delegate
                    {
                        project = wcfServiceClient.Service.findProjectByShortCode(request).@return;
                    });
                }
                GL.Project result = new GL.Project(project);
                result.submitters = _getSubmitters(result.shortcode);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Invalid Project Code: " + e.Message);
            }
        }

        /**
         * Get all user projects
         * 
         *            
         * @return Array of Projects to which the logged in user has access to
         */
        public GL.Project[] getProjects()
        {
            PS.getUserProjectsRequest request = new PS.getUserProjectsRequest();
            request.isSubProjectIncluded = true;

            PS.Project[] projects = null;

            using (WcfServiceClient<PS.ProjectServicePortType> wcfServiceClient =
                new WcfServiceClient<PS.ProjectServicePortType>(binding, PROJECT_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    projects = wcfServiceClient.Service.getUserProjects(request).@return;
                });
            }
            if (projects == null)
            {
                return null;
            }
            return _convertProjectsToInternal(projects);
        }

        public string getSubmissionName(String submissionTicket)
        {
            SS.findByTicketRequest request = new SS.findByTicketRequest();
            request.ticket = submissionTicket;

            SS.Submission submission = null;
            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    submission = wcfServiceClient.Service.findByTicket(request).@return;
                });
            }
            if (submission != null)
            {
                return submission.submissionInfo.name;
            }
            else
            {
                throw new Exception("Invalid submission ticket");
            }
        }

        /**
	     * Get Submission status for specified ticket.
	     * 
	     * @return True if submission is complete.
	     * @throws Exception
	     */
	    public String getSubmissionStatus( String submissionTicket ) {
            SS.findByTicketRequest request = new SS.findByTicketRequest();
            request.ticket = submissionTicket;

            SS.Submission submission = null;
            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    submission = wcfServiceClient.Service.findByTicket(request).@return;
                });
            }
            if (submission != null)
            {
                return submission.status.name;
            }
            else
            {
                return null;
            }

	    }

        public String[] getSubmissionTickets()
        {
            if (submission != null)
            {
                return submission.tickets.ToArray();
            }
            else
            {
                throw new Exception("Submission not initialized");
            }
        }

        /**
	     * Get Unstarted Submissions.
	     * 
	     * @return Array of Submissions that have not been started.
	     */
        public GL.Submission[] getUnstartedSubmissions(GL.Project project)
        {
            SS.findCreatingSubmissionsByProjectShortCodeRequest request = new SS.findCreatingSubmissionsByProjectShortCodeRequest();
            request.projectShortCode = project.shortcode;

            SS.Submission[] submissions = null;

            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    submissions = wcfServiceClient.Service.findCreatingSubmissionsByProjectShortCode(request).@return;
                });
            }
            if (submissions == null)
            {
                return null;
            }
            return _convertSubmissionsToInternal(submissions);
        }

        /**
         * Initialize a new Submission
         * 
         * @param submission
         *            Submission configuration to initialize a new submission
         */
        public void initSubmission(GL.Submission submission)
        {
            this.submission = submission;
            _validateSubmission();
            submission.tickets = new List<String>();

        }

        public bool isSubmissionComplete(String submissionTicket)
        {
            SS.findByTicketRequest request = new SS.findByTicketRequest();
            request.ticket = submissionTicket;

            SS.Submission submission = null;
            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    submission = wcfServiceClient.Service.findByTicket(request).@return;
                });
            }
            bool status = false;
            if (submission != null && submission.status.value == 4)
            {
                status = true;
            }

            return status;
        }

        /**
        * Sends confirmation that the target resources was downloaded successfully
        * by the customer.
        * 
        * @param targetTicket
        *            Downloaded target`s ticket
        */
        public String sendDownloadConfirmation(String targetTicket)
        {
            TS.sendDownloadConfirmationRequest request = new TS.sendDownloadConfirmationRequest();
            request.targetId = targetTicket;

            string result = null;

            using (WcfServiceClient<TS.TargetServicePortType> wcfServiceClient =
                new WcfServiceClient<TS.TargetServicePortType>(binding, TARGET_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.sendDownloadConfirmation(request).@return;

                });
            }
            return result;
        }

        /**
         * 
         * Set/update Project Director connection configuration
         * 
         * @param connectionConfig
         *            Project Director Connection Configuration
         */
        public void setConnectionConfig(ProjectDirectorConfig connectionConfig)
        {
            if (String.IsNullOrEmpty(connectionConfig.url))
            {
                throw new Exception("PD Url not set");
            }
            if (String.IsNullOrEmpty(connectionConfig.username))
            {
                throw new Exception("Username not set");
            }
            if (String.IsNullOrEmpty(connectionConfig.password))
            {
                throw new Exception("Password not set");
            }
            if (String.IsNullOrEmpty(connectionConfig.userAgent))
            {
                throw new Exception("UserAgent not set");
            }

            this.pdConfig = connectionConfig;
            if (this.pdConfig.url.EndsWith("/"))
            {
                this.pdConfig.url = this.pdConfig.url.Substring(0, this.pdConfig.url.Length - 1);
            }
            if (this.pdConfig.url.StartsWith("https"))
            {
                binding = SecureConstants.BINDING;
            }
            else
            {
                binding = Constants.BINDING;
            }
            System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

            if (pdConfig.proxy != null)
            {
                if (pdConfig.username != null && pdConfig.password != null)
                {
                    binding.BypassProxyOnLocal = false;
                    binding.UseDefaultWebProxy = true;
                    System.Net.NetworkCredential cred = new NetworkCredential(pdConfig.proxy.proxyUser, pdConfig.proxy.proxyPassword);
                    WebProxy wproxy = new WebProxy("http://" + pdConfig.proxy.proxyHost + ":" + pdConfig.proxy.proxyPort, false, null, cred);
                    WebRequest.DefaultWebProxy = wproxy;
                }
                else
                {
                    binding.ProxyAddress = new Uri("http://" + pdConfig.proxy.proxyHost + ":" + pdConfig.proxy.proxyPort);
                    binding.BypassProxyOnLocal = false;
                    binding.UseDefaultWebProxy = false;
                }
            }

            _endpointExists();

            USERNAMETOKEN = new UsernameToken
            {
                Id = "1",
                Username = this.pdConfig.username,
                Password = this.pdConfig.password,
                UserAgent = this.pdConfig.userAgent
            };

            PROJECT_ENDPOINT_ADDRESS = new EndpointAddress(new Uri(this.pdConfig.url) + "/services/ProjectService_4130");
            TARGET_ENDPOINT_ADDRESS = new EndpointAddress(new Uri(this.pdConfig.url) + "/services/TargetService_4130");
            SUBMISSION_ENDPOINT_ADDRESS = new EndpointAddress(new Uri(this.pdConfig.url) + "/services/SubmissionService_4130");
            DOCUMENT_ENDPOINT_ADDRESS = new EndpointAddress(new Uri(this.pdConfig.url) + "/services/DocumentService_4130");
            USER_ENDPOINT_ADDRESS = new EndpointAddress(new Uri(this.pdConfig.url) + "/services/UserProfileService_4130");

            

            if (pdConfig.timeOut != 0)
            {
                binding.SendTimeout = new TimeSpan(0, 0, pdConfig.timeOut, 0, 0);
                binding.ReceiveTimeout = new TimeSpan(0, 0, pdConfig.timeOut, 0, 0);
            }
            try
            {
                getProjects();
            }
            catch (Exception e)
            {
                throw new Exception("Invalid config. " + e.Message);
            }
        }

        /**
         * 
         * Start Submission
         * 
         * @return Submission Ticket
         */
        public String[] startSubmission()
        {
            if (submission == null || submission.project == null || String.IsNullOrEmpty(submission.name))
            {
                throw new Exception("Please initialize submission first.");
            }

            if (submission.tickets.Count == 0)
            {
                throw new Exception("Please upload a translatable document before attempting to start a submission.");
            }

            // Start each submission
		    String[] tickets = submission.tickets.ToArray();
		    Dictionary<String,SubmissionInfo> submissionInfos = _createSubmissionInfos();
		    foreach( String submissionTicket in tickets ) {
                SS.startSubmissionRequest request = new SS.startSubmissionRequest
                {
                    submissionId = submissionTicket,
                    submissionInfo = submissionInfos[submissionTicket]
                };
                string result = null;
                using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
                {
                    wcfServiceClient.CallServiceMethod(delegate
                    {
                        result = wcfServiceClient.Service.startSubmission(request).@return;
                    });
                }
                if (!String.IsNullOrEmpty(submission.owner))
                {
                    SS.addOwnerRequest addOwnerRequest = new SS.addOwnerRequest
                    {
                        submissionTicket = submissionTicket,
                        username = submission.owner
                    };

                    using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                        new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
                    {
                        wcfServiceClient.CallServiceMethod(delegate
                        {
                            result = wcfServiceClient.Service.addOwner(addOwnerRequest).@return;
                        });
                    }
                }
		    }

		    submission = null;
		    return tickets;
        }

        /**
         * 
         * Upload reference file for submission
         * 
         * @param referenceDocument
         *            Reference document
         */
        public String uploadReference(GL.ReferenceDocument referenceDocument)
        {
            if (referenceDocument == null || referenceDocument.data == null || referenceDocument.data.Length == 0)
            {
                throw new Exception("Empty data stream");
            }
            if (String.IsNullOrEmpty(referenceDocument.name))
            {
                throw new Exception("Invalid name.");
            }
            if (submission == null)
            {
                throw new Exception("Submission is not initialized.");
            }
            String[] tickets = submission.tickets.ToArray();
            if (submission.tickets.Count == 0)
            {
                throw new Exception("Invalid submission ticket. Please upload a translatable document before attempting to upload reference documents.");
            }
            SS.base64Binary base64data = new SS.base64Binary();
            base64data.contentType = "application/octet-stream";
            base64data.Value = referenceDocument.data;
            SS.uploadReferenceRequest request = new uploadReferenceRequest
            {
                data = base64data,
                resourceInfo = referenceDocument.getResourceInfo(),
                submissionId = tickets[tickets.Length - 1]
            };

            string result = null;

            using (WcfServiceClient<SS.SubmissionServicePortType> wcfServiceClient =
                new WcfServiceClient<SS.SubmissionServicePortType>(binding, SUBMISSION_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    result = wcfServiceClient.Service.uploadReference(request).@return;
                });
            }
            return result;
        }

        /**
         * Uploads the document to project director for translation
         * 
         * @param document
         *            Document that requires translation
         * @return Document Ticket
         */
        public String uploadTranslatable(GL.Document document)
        {
            if (submission == null)
            {
                throw new Exception("Submission not initialized.");
            }
            _validateDocument(document);

            DS.DocumentInfo documentInfo = document.getDocumentInfo(submission);
            DS.ResourceInfo resourceInfo = document.getResourceInfo();
            DS.base64Binary base64data = new DS.base64Binary();
            base64data.contentType = "application/octet-stream";
            base64data.Value = document.data;

            DS.submitDocumentWithBinaryResourceRequest request = new DS.submitDocumentWithBinaryResourceRequest
                    {
                        documentInfo = documentInfo,
                        resourceInfo = resourceInfo,
                        data = base64data
                    };

            DS.DocumentTicket documentTicket = null;

            using (WcfServiceClient<DS.DocumentServicePortType> wcfServiceClient =
                new WcfServiceClient<DS.DocumentServicePortType>(binding, DOCUMENT_ENDPOINT_ADDRESS, USERNAMETOKEN))
            {
                wcfServiceClient.CallServiceMethod(delegate
                {
                    documentTicket = wcfServiceClient.Service.submitDocumentWithBinaryResource(request).@return;
                });
            }

            if (documentTicket != null)
            {
                submission.addTicket(documentTicket.submissionTicket);
            }

            return documentTicket.ticketId;

        }

    }
}
