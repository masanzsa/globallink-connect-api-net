using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobalLink.Connect.Model
{
    public class WordCount
    {
        public int golden { get; set; }
        public int exact_100 { get; set; }
        public int fuzzy { get; set; }
        public int repetitions { get; set; }
        public int nomatch { get; set; }
        public int total { get; set; }
        public WordCount(int _golden, int _exact_100, int _repetitions, int _nomatch, int _total)
        {
            this.golden = _golden;
            this.exact_100 = _exact_100;
            this.fuzzy = _total - _golden - _exact_100 - _repetitions - _nomatch;
            this.repetitions = _repetitions;
            this.nomatch = _nomatch;
            this.total = _total;
        }
    }
}
