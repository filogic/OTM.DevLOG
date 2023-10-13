using System;
using System.ComponentModel.DataAnnotations;

namespace OTM.DevLOG.Data
{
	public class NdwOpenDataMeasurementSiteReference
		: Volo.Abp.Domain.Entities.Entity<System.Guid>
    {
        [StringLength(256)]
        public required System.String MeasurementSiteId { get; set; }


        public required System.String MeasurementSiteReference { get; set; }
    }
}
