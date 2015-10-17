using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using sparkiy.Connectors.IoT.Windows.Models;

namespace sparkiy.Connectors.IoT.Windows
{
	/// <summary>
	/// Windows IoT device API client.
	/// </summary>
	public class DeviceApi : IDeviceApi
	{
		private const string GetInstalledAppXPackagesApiPath = "/api/appx/packagemanager/packages";
		private const string GetIpConfigApiPath = "/api/networking/ipconfig";
		private const string GetComputerNameApiPath = "/api/os/machinename";
		private const string GetSoftwareInfoApiPath = "/api/os/info";

		private Connection currentConnection;
		private Credentials currentCredentials;
		private RestClient client;


		/// <summary>
		/// Gets or sets the connection information.
		/// </summary>
		/// <value>
		/// The connection information.
		/// </value>
		public Connection Connection
		{
			get { return this.currentConnection; }
			set { this.SetConnection(value); }
		}

		/// <summary>
		/// Gets or sets the credentials.
		/// </summary>
		/// <value>
		/// The credentials.
		/// </value>
		public Credentials Credentials
		{
			get { return this.currentCredentials; }
			set { this.SetCredentials(value);}
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceApi"/> class.
		/// </summary>
		/// <remarks>
		/// If you use empty constructor, you should call <see cref="Initialize"/> right after constructor 
		/// in order to set connection information and credentials data.
		/// </remarks>
		public DeviceApi()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceApi"/> class.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="credentials">The credentials.</param>
		/// <exception cref="System.ArgumentNullException">
		/// connection
		/// or
		/// credentials
		/// </exception>
		public DeviceApi(Connection connection, Credentials credentials)
		{
			if (connection == null) throw new ArgumentNullException(nameof(connection));
			if (credentials == null) throw new ArgumentNullException(nameof(credentials));

			this.Initialize(connection, credentials);
		}


		/// <summary>
		/// Initializes the client with proper connection information and credentials.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="credentials">The credentials.</param>
		/// <exception cref="System.ArgumentNullException">
		/// connection
		/// or
		/// credentials
		/// </exception>
		public void Initialize(Connection connection, Credentials credentials)
		{
			if (connection == null) throw new ArgumentNullException(nameof(connection));
			if (credentials == null) throw new ArgumentNullException(nameof(credentials));

			this.SetConnection(connection, true);
			this.SetCredentials(credentials, true);
			this.ReinitializeClient();
		}

		/// <summary>
		/// Sets the connection information.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="suppressReinitialization">
		/// If set to <c>True</c> client reinitialization will be suppressed.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// connection
		/// </exception>
		protected void SetConnection(Connection connection, bool suppressReinitialization = false)
		{
			if (connection == null) throw new ArgumentNullException(nameof(connection));

			this.currentConnection = connection;

			// Reinitialize client if not suppressed
			if (!suppressReinitialization)
				this.ReinitializeClient();
		}

		/// <summary>
		/// Sets the credentials.
		/// </summary>
		/// <param name="credentials">The credentials.</param>
		/// <param name="suppressReinitialization">
		/// If set to <c>True</c> client reinitialization will be suppressed.
		/// </param>
		/// <exception cref="System.ArgumentNullException">
		/// credentials
		/// </exception>
		protected void SetCredentials(Credentials credentials, bool suppressReinitialization = false)
		{
			if (credentials == null) throw new ArgumentNullException(nameof(credentials));

			this.currentCredentials = credentials;

			// Reinitialize client if not suppressed
			if (!suppressReinitialization)
				this.ReinitializeClient();
		}

		/// <summary>
		/// Reinitializes the REST client.
		/// </summary>
		/// <exception cref="System.NullReferenceException">
		/// Set Connection information before initializing client.
		/// or
		/// Set Credentials before initializing client.
		/// </exception>
		protected void ReinitializeClient()
		{
			if (this.currentConnection == null) throw new NullReferenceException("Set Connection information before initializing client.");
			if (this.currentCredentials == null) throw new NullReferenceException("Set Credentials before initializing client.");

			this.client = new RestClient(this.currentConnection, this.currentCredentials);
		}

		/// <summary>
		/// Gets the machine name.
		/// </summary>
		/// <returns>Returns <see cref="MachineName"/> that is populated with data from device.</returns>
		public async Task<MachineName> GetMachineNameAsync()
		{
			return await GetDeserializedAsync<MachineName>(GetComputerNameApiPath);
		}

		/// <summary>
		/// Gets the software information.
		/// </summary>
		/// <returns>Returns <see cref="SoftwareInfo"/> that is populated with data from device.</returns>
		public async Task<SoftwareInfo> GetSoftwareInfo()
		{
			return await GetDeserializedAsync<SoftwareInfo>(GetSoftwareInfoApiPath);
		}

		/// <summary>
		/// Gets the ip configuration.
		/// </summary>
		/// <returns>Returns <see cref="IpConfig"/> that is populated with data from device.</returns>
		public async Task<IpConfig> GetIpConfig()
		{
			return await GetDeserializedAsync<IpConfig>(GetIpConfigApiPath);
		}

		/// <summary>
		/// Gets the installed AppX packages.
		/// </summary>
		/// <returns>Returns <see cref="AppXPackages"/> that is populated with data from device.</returns>
		public async Task<AppXPackages> GetInstalledAppXPackages()
		{
			return await GetDeserializedAsync<AppXPackages>(GetInstalledAppXPackagesApiPath);
		}

		/// <summary>
		/// Gets and deserialized data from given path.
		/// </summary>
		/// <typeparam name="T">Type into which data needs to be deserialized to.</typeparam>
		/// <param name="path">The path.</param>
		/// <returns>
		/// Returns new instance of <see cref="T"/> populated with value from data that was 
		/// retrieved using REST API client GET request. If retrieving or deserialization 
		/// fails - returns <c>default(<see cref="T"/>)</c>.
		/// </returns>
		protected async Task<T> GetDeserializedAsync<T>(string path)
		{
			// Retrieve JSON data
			var dataJson = await client.GetAsync(path);
			if (dataJson == null)
				return default(T);

			// Deserialize JSON data
			var data = TryDeserialize<T>(dataJson);

			return data;
		}

		/// <summary>
		/// Tries the deserialize given data.
		/// </summary>
		/// <typeparam name="T">Type into which data needs to be deserialized to.</typeparam>
		/// <param name="data">The data.</param>
		/// <returns>
		/// Returns new instance of <see cref="T"/> populated with values from given data. 
		/// If deserialization fails - returns <c>default(<see cref="T"/>)</c>.
		/// </returns>
		protected static T TryDeserialize<T>(string data)
		{
			try
			{
				return JsonConvert.DeserializeObject<T>(data);
			}
			catch (Exception)
			{
				return default(T);
			}
		}
	}
}