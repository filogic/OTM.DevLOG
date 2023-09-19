using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace OTM.DevLOG.ApplicationServices
{
	public interface INdwIngestorAppService
        : IApplicationService
    {
        Task ActualTrafficInfo( );
    }
}

