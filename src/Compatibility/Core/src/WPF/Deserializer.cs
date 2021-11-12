using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Compatibility.Internals;

namespace Microsoft.Maui.Controls.Compatibility.Platform.WPF
{
	internal sealed class Deserializer : IDeserializer
	{
		const string PropertyStoreFile = "PropertyStore.forms";

		[RequiresUnreferencedCode(TrimmerConstants.SerializerTrimmerWarning)]
		public Task<IDictionary<string, object>> DeserializePropertiesAsync()
		{
			IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

			if (!isoStore.FileExists(PropertyStoreFile))
				return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>(4));

			using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(PropertyStoreFile, FileMode.Open, isoStore))
			{
				if (stream.Length == 0)
					return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>(4));

				try
				{
					var serializer = new DataContractSerializer(typeof(IDictionary<string, object>));
					return Task.FromResult((IDictionary<string, object>)serializer.ReadObject(stream));
				}
				catch (Exception e)
				{
					Debug.WriteLine("Could not deserialize properties: " + e.Message);
					Log.Warning("Microsoft.Maui.Controls.Compatibility PropertyStore", $"Exception while reading Application properties: {e}");
				}

				return Task.FromResult<IDictionary<string, object>>(null);
			}
		}

		[RequiresUnreferencedCode(TrimmerConstants.SerializerTrimmerWarning)]
		public async Task SerializePropertiesAsync(IDictionary<string, object> properties)
		{
			try
			{
				IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

				using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(PropertyStoreFile, FileMode.Create, isoStore))
				{
					var serializer = new DataContractSerializer(typeof(IDictionary<string, object>));
					serializer.WriteObject(stream, properties);
					await stream.FlushAsync();
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine("Could not move new serialized property file over old: " + e.Message);
			}
		}
	}
}
