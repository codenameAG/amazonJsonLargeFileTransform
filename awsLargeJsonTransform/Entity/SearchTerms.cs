using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ca.awsLargeJsonTransform.Entity
{

    public class SearchTerms
    {
        public Reportspecification reportSpecification { get; set; }
        public List<Databydepartmentandsearchterm> dataByDepartmentAndSearchTerm { get; set; }
    }

    public class Reportspecification
    {
        public string dataStartTime { get; set; }
        public string dataEndTime { get; set; }
        public string[] marketplaceIds { get; set; }
        public Reportoptions reportOptions { get; set; }
        public string reportType { get; set; }
    }

    public class Reportoptions
    {
        public string reportPeriod { get; set; }
    }

    public class Databydepartmentandsearchterm
    {
        public string departmentName { get; set; }
        public string searchTerm { get; set; }
        public int? searchFrequencyRank { get; set; }
        public string clickedAsin { get; set; }
        public string clickedItemName { get; set; }
        public int? clickShareRank { get; set; }
        public float? clickShare { get; set; }
        public float? conversionShare { get; set; }
    }

}
