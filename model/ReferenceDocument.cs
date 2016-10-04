using GlobalLink.Connect.SubmissionServiceRef;
using System;
using System.IO;

namespace GlobalLink.Connect.Model
{
    public class ReferenceDocument
    {
        public byte[] data { get; set; }
        public String name { get; set; }

        /**
         * Set data from string
         * 
         * @param data
         *            String containing data that needs to be translated
         */
        public void setDataFromString(String data)
        {
            this.data = System.Text.Encoding.UTF8.GetBytes(data);
             
        }

        /**
         * Set data from string
         * 
         * @param data
         *            String containing data that needs to be translated
         * @throws UnsupportedEncodingException
         */
        public void setDataFromMemoryStream(MemoryStream data)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = data.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                this.data = ms.ToArray();
            }
        }

        public ResourceInfo getResourceInfo()
        {
            ResourceInfo resourceInfo = new ResourceInfo();
            resourceInfo.name = this.name;
            resourceInfo.size = data.Length;
            return resourceInfo;
        }

        /**
         * Gets data as InputStream
         * @return InputStream
         */
        public String getDataAsString()
        {
            char[] chars = new char[this.data.Length / sizeof(char)];
            System.Buffer.BlockCopy(this.data, 0, chars, 0, this.data.Length);
            return new string(chars);
        }
    }
}
