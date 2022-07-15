```csharp

public class IotHubServiceClient : IDisposable
{
	public DevicesClient Devices { get; private set; }
	public ModulesClient Modules { get; private set; }

	protected IotHubServiceClient();
	
	public IotHubServiceClient(string connectionString, IotHubServiceClientOptions options = default);
	public IotHubServiceClient(string hostName, TokenCredential credential, IotHubServiceClientOptions options = default);
	public IotHubServiceClient(string hostName, AzureSasCredential credential, IotHubServiceClientOptions options = default);

	public void Dispose();
}

public class IotHubServiceClientOptions
{
	public IotHubServiceClientOptions(ServiceVersion version = LatestVersion)

	public IWebProxy Proxy { get; set; }
	public HttpClient HttpClient { get; set; }
	public ServiceVersion Version { get; set; } = LatestVersion;

	public enum ServiceVersion
	{
		V2021_04_12 = 1,
		V2020_03_13 = 2,
		V2019_10_01 = 3,
		V2019_09_30 = 4,
		V2019_03_30 = 5,
		V2018_06_30 = 6,
		V2018_04_01 = 7
	}
}


public class DevicesClient
{
	protected DevicesClient();

	public virtual async Task<Device> AddAsync(Device device, CancellationToken cancellationToken = default);
	public virtual async Task<Device> GetAsync(string deviceId, CancellationToken cancellationToken = default);
	public virtual async Task<Device> SetAsync(Device device, CancellationToken cancellationToken = default);
	public virtual async Task<Device> SetAsync(Device device, bool forceUpdate, CancellationToken cancellationToken = default);
	public virtual async Task DeleteAsync(string deviceId, CancellationToken cancellationToken = default);
	public virtual async Task DeleteAsync(Device device, CancellationToken cancellationToken = default);

	public virtual async Task<BulkRegistryOperationResult> AddWithTwinAsync(Device device, Twin twin, CancellationToken cancellationToken = default);

	public virtual async Task<BulkRegistryOperationResult> AddAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default);
	public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default);
	public virtual async Task<BulkRegistryOperationResult> SetAsync(IEnumerable<Device> devices, bool forceUpdate, CancellationToken cancellationToken = default);
	public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, CancellationToken cancellationToken = default);
	public virtual async Task<BulkRegistryOperationResult> DeleteAsync(IEnumerable<Device> devices, bool forceDelete, CancellationToken cancellationToken = default);

	public virtual async Task<IEnumerable<Module>> GetModulesAsync(string deviceId, CancellationToken cancellationToken = default);
			
	public virtual async Task<RegistryStatistics> GetRegistryStatisticsAsync(CancellationToken cancellationToken = default);
	public virtual async Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken = default);

	public virtual async Task ExportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default);
	public virtual async Task ImportAsync(string storageAccountConnectionString, string containerName, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, bool excludeKeys, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ExportAsync(Uri exportBlobContainerUri, string outputBlobName, bool excludeKeys, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ExportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ImportAsync(Uri importBlobContainerUri, Uri outputBlobContainerUri, string inputBlobName, CancellationToken cancellationToken = default);
	public virtual Task<JobProperties> ImportAsync(JobProperties jobParameters, CancellationToken cancellationToken = default);
	public virtual async Task<JobProperties> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
	public virtual async Task<IEnumerable<JobProperties>> GetJobsAsync(CancellationToken cancellationToken = default);
	public virtual async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default);
}

public class ModulesClient
{
	protected ModulesClient();

	public virtual async Task<Module> AddAsync(Module module, CancellationToken cancellationToken = default);
	public virtual async Task<Module> GetAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default);
	public virtual async Task<Module> SetAsync(Module module, CancellationToken cancellationToken = default);
	public virtual async Task<Module> SetAsync(Module module, bool forceUpdate, CancellationToken cancellationToken = default);
	public virtual async Task DeleteAsync(string deviceId, string moduleId, CancellationToken cancellationToken = default);
	public virtual async Task DeleteAsync(Module module, CancellationToken cancellationToken = default);		
}

```